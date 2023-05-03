using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemUtilities.FileSystem;
using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace DevOptimal.SystemStateManager.Persistence.FileSystem.Caching
{
    internal class SQLiteFileCache : IFileCache
    {
        private const int maxChunkSize = 999999953; // Maximum BLOB size for SQLite database

        private class FileChunk
        {
            public int FileID { get; set; }

            public int ChunkIndex { get; set; }

            public byte[] Data { get; set; }
        }

        public IFileSystem FileSystem { get; }

        public SQLiteFileCache(IFileSystem fileSystem)
        {
            FileSystem = fileSystem;

            using (var connection = SqliteConnectionFactory.Create())
            {
                var createCommand = connection.CreateCommand();
                createCommand.CommandText =
                $@"CREATE TABLE IF NOT EXISTS {nameof(FileChunk)} (
                    {nameof(FileChunk.FileID)} INTEGER NOT NULL,
                    {nameof(FileChunk.ChunkIndex)} INTEGER NOT NULL,
                    {nameof(FileChunk.Data)} BLOB,
                    PRIMARY KEY ({nameof(FileChunk.FileID)}, {nameof(FileChunk.ChunkIndex)})
                );";
                createCommand.ExecuteNonQuery();
            }
        }

        public void DownloadFile(string id, string destinationPath)
        {
            var destinationFile = new FileInfo(destinationPath);
            using (var fileStream = FileSystem.OpenFile(destinationFile.FullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            using (var connection = SqliteConnectionFactory.Create())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    fileStream.SetLength(0); // Delete existing file.

                    var selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = $@"SELECT {nameof(FileChunk.Data)} FROM {nameof(FileChunk)} WHERE {nameof(FileChunk.FileID)} = @{nameof(FileChunk.FileID)} ORDER BY {nameof(FileChunk.ChunkIndex)} ASC";
                    selectCommand.Parameters.AddWithValue($"@{nameof(FileChunk.FileID)}", id);
                    using (var reader = selectCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            using (var blobStream = reader.GetStream(reader.GetOrdinal(nameof(FileChunk.Data))))
                            {
                                blobStream.CopyTo(fileStream);
                            }
                        }
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public string UploadFile(string sourcePath)
        {
            var fileID = Guid.NewGuid().ToString();
            var file = new FileInfo(sourcePath);

            using (var fileStream = FileSystem.OpenFile(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var connection = SqliteConnectionFactory.Create())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var index = 0;
                    var remainingBytes = fileStream.Length;
                    while (remainingBytes > 0)
                    {
                        var chunkSize = Math.Min(maxChunkSize, remainingBytes);

                        var insertCommand = connection.CreateCommand();
                        insertCommand.CommandText =
                        $@"INSERT INTO {nameof(FileChunk)} (
                            {nameof(FileChunk.FileID)},
                            {nameof(FileChunk.ChunkIndex)},
                            {nameof(FileChunk.Data)}
                        ) VALUES (
                            @{nameof(FileChunk.FileID)},
                            @{nameof(FileChunk.ChunkIndex)},
                            zeroblob(@{nameof(chunkSize)})
                        );
                        SELECT last_insert_rowid();";
                        insertCommand.Parameters.AddWithValue($"@{nameof(FileChunk.FileID)}", fileID);
                        insertCommand.Parameters.AddWithValue($"@{nameof(FileChunk.ChunkIndex)}", index++);
                        insertCommand.Parameters.AddWithValue($"@{nameof(chunkSize)}", chunkSize);
                        var rowid = (long)insertCommand.ExecuteScalar();

                        var bufferSize = 81920;
                        using (var blobStream = new SqliteBlob(connection, nameof(FileChunk), nameof(FileChunk.Data), rowid))
                        {
                            for (var i = 0; i < chunkSize; i += bufferSize)
                            {
                                bufferSize = (int)Math.Min(bufferSize, chunkSize - i);
                                var buffer = new byte[bufferSize];
                                remainingBytes -= fileStream.Read(buffer, 0, bufferSize);
                                blobStream.Write(buffer, 0, bufferSize);
                            }
                        }
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }

            return fileID;
        }
    }
}
