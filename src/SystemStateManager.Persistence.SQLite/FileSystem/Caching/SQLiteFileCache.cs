using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemUtilities.FileSystem;
using SQLite;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace DevOptimal.SystemStateManager.Persistence.SQLite.FileSystem.Caching
{
    internal class SQLiteFileCache : IFileCache
    {
        // This is the maximum number of bytes that sqlite-net-pcl can add to a BLOB field.
        private const int maxChunkSize = 999999953;

        private class FileRecord
        {
            [PrimaryKey, AutoIncrement]
            public int ID { get; set; }

            [Indexed]
            public string FileID { get; set; }

            public string ChunkHash { get; set; }

            public int ChunkIndex { get; set; }
        }

        private class FileChunk
        {
            [PrimaryKey]
            public string Hash { get; set; }

            public byte[] Data { get; set; }
        }

        public IFileSystem FileSystem { get; }

        public SQLiteFileCache(IFileSystem fileSystem)
        {
            FileSystem = fileSystem;
        }

        public void DownloadFile(string id, string destinationPath)
        {
            var destinationFile = new FileInfo(destinationPath);
            using (var db = SQLiteDatabaseFactory.GetDatabase())
            using (var fileStream = FileSystem.OpenFile(destinationFile.FullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                fileStream.SetLength(0); // Delete existing file.
                var records = db.Query<FileRecord>($"SELECT * FROM {nameof(FileRecord)} WHERE {nameof(FileRecord.FileID)} = '{id}'").OrderBy(r => r.ChunkIndex).ToList();
                foreach (var record in records)
                {
                    var chunk = db.Find<FileChunk>(record.ChunkHash);
                    fileStream.Write(chunk.Data, 0, chunk.Data.Length);
                }
            }
        }

        public string UploadFile(string sourcePath)
        {
            var fileID = Guid.NewGuid().ToString();
            var file = new FileInfo(sourcePath);
            using (var db = SQLiteDatabaseFactory.GetDatabase())
            using (var fileStream = FileSystem.OpenFile(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                db.CreateTable<FileRecord>();
                db.CreateTable<FileChunk>();

                var index = 0;
                var remainingBytes = file.Length;
                while (remainingBytes > 0)
                {
                    var chunkSize = Math.Min(maxChunkSize, remainingBytes);
                    var buffer = new byte[chunkSize];
                    remainingBytes -= fileStream.Read(buffer, 0, (int)chunkSize);
                    using (var chunkHashAlgorithm = SHA1.Create())
                    {
                        var hash = BitConverter.ToString(chunkHashAlgorithm.ComputeHash(buffer)).Replace("-", string.Empty);
                        try
                        {
                            db.Insert(new FileChunk { Data = buffer, Hash = hash });
                        }
                        catch (SQLiteException ex) when (ex.Message.Equals($"UNIQUE constraint failed: {nameof(FileChunk)}.{nameof(FileChunk.Hash)}"))
                        {
                            // Means that the chunk has already been added, no need to add it again.
                        }
                        db.Insert(new FileRecord
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
}
