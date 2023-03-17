using Dapper;
using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemUtilities.FileSystem;
using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace DevOptimal.SystemStateManager.Persistence.SQLite.FileSystem.Caching
{
    internal class SQLiteFileCache : IFileCache
    {
        private const int maxChunkSize = 10000000;

        private class FileRecord
        {
            public int ID { get; set; }

            public string FileID { get; set; }

            public string ChunkHash { get; set; }

            public int ChunkIndex { get; set; }
        }

        private class FileChunk
        {
            public string Hash { get; set; }

            public byte[] Data { get; set; }
        }

        public IFileSystem FileSystem { get; }

        public SQLiteFileCache(IFileSystem fileSystem)
        {
            FileSystem = fileSystem;

            PersistentSystemStateManager.Connection.Execute($@"CREATE TABLE IF NOT EXISTS {nameof(FileRecord)} (
                {nameof(FileRecord.ID)} INTEGER PRIMARY KEY,
                {nameof(FileRecord.FileID)} TEXT,
                {nameof(FileRecord.ChunkHash)} TEXT,
                {nameof(FileRecord.ChunkIndex)} INTEGER
            );");
            PersistentSystemStateManager.Connection.Execute($@"CREATE INDEX IF NOT EXISTS {nameof(FileRecord.FileID)} ON {nameof(FileRecord)} (
                {nameof(FileRecord.FileID)}
            );");
            PersistentSystemStateManager.Connection.Execute($@"CREATE TABLE IF NOT EXISTS {nameof(FileChunk)} (
                {nameof(FileChunk.Hash)} TEXT PRIMARY KEY,
                {nameof(FileChunk.Data)} BLOB
            );");
        }

        public void DownloadFile(string id, string destinationPath)
        {
            var destinationFile = new FileInfo(destinationPath);
            using (var fileStream = FileSystem.OpenFile(destinationFile.FullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                fileStream.SetLength(0); // Delete existing file.
                var records = PersistentSystemStateManager.Connection.Query<FileRecord>($"SELECT * FROM {nameof(FileRecord)} WHERE {nameof(FileRecord.FileID)} = '{id}'").OrderBy(r => r.ChunkIndex).ToList();
                foreach (var record in records)
                {
                    var chunk = PersistentSystemStateManager.Connection.QuerySingle<FileChunk>($"SELECT * FROM {nameof(FileChunk)} WHERE {nameof(FileChunk.Hash)} = '{record.ChunkHash}'");
                    fileStream.Write(chunk.Data, 0, chunk.Data.Length);
                }
            }
        }

        public string UploadFile(string sourcePath)
        {
            var fileID = Guid.NewGuid().ToString();
            var file = new FileInfo(sourcePath);
            using (var fileStream = FileSystem.OpenFile(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var chunkHashAlgorithm = SHA1.Create())
            {
                var index = 0;
                var remainingBytes = file.Length;
                while (remainingBytes > 0)
                {
                    var chunkSize = Math.Min(maxChunkSize, remainingBytes);
                    var buffer = new byte[chunkSize];
                    remainingBytes -= fileStream.Read(buffer, 0, (int)chunkSize);

                    var hash = BitConverter.ToString(chunkHashAlgorithm.ComputeHash(buffer)).Replace("-", string.Empty);
                    try
                    {
                        PersistentSystemStateManager.Connection.Execute(
                            $@"INSERT INTO {nameof(FileChunk)} ({nameof(FileChunk.Hash)}, {nameof(FileChunk.Data)}) VALUES (@{nameof(FileChunk.Hash)}, @{nameof(FileChunk.Data)})",
                            new FileChunk {
                                Hash = hash,
                                Data = buffer
                            });
                    }
                    catch (SqliteException ex) when (ex.SqliteErrorCode == 19 && ex.SqliteExtendedErrorCode == 1555)
                    {
                        // Ignorable - means the chunk has already been uploaded.
                    }
                    finally
                    {
                        buffer = null;
                    }
                    PersistentSystemStateManager.Connection.Execute(
                        $@"INSERT INTO {nameof(FileRecord)} ({nameof(FileRecord.FileID)}, {nameof(FileRecord.ChunkHash)}, {nameof(FileRecord.ChunkIndex)}) VALUES (@{nameof(FileRecord.FileID)}, @{nameof(FileRecord.ChunkHash)}, @{nameof(FileRecord.ChunkIndex)})",
                        new FileRecord
                        {
                            FileID = fileID,
                            ChunkHash = hash,
                            ChunkIndex = index++
                        });
                }
            }

            return fileID;
        }
    }
}
