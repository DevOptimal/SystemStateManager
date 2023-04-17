using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemUtilities.FileSystem;
using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace DevOptimal.SystemStateManager.Persistence.FileSystem.Caching
{
    internal class SQLiteFileCache : IFileCache
    {
        private const int maxChunkSize = 10000;

        private class FileChunk
        {
            public int FileID { get; set; }

            public int ChunkIndex { get; set; }

            public byte[] Data { get; set; }
        }

        public IFileSystem FileSystem { get; }

        private readonly SqliteConnection connection;

        public SQLiteFileCache(IFileSystem fileSystem, SqliteConnection connection)
        {
            FileSystem = fileSystem;
            this.connection = connection;

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

        public void DownloadFile(string id, string destinationPath)
        {
            var destinationFile = new FileInfo(destinationPath);
            using (var fileStream = FileSystem.OpenFile(destinationFile.FullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                fileStream.SetLength(0); // Delete existing file.

                var selectCommand = connection.CreateCommand();
                selectCommand.CommandText = $@"SELECT {nameof(FileChunk.Data)} FROM {nameof(FileChunk)} WHERE {nameof(FileChunk.FileID)} = @{nameof(FileChunk.FileID)} ORDER BY {nameof(FileChunk.ChunkIndex)} ASC";
                selectCommand.Parameters.AddWithValue($"@{nameof(FileChunk.FileID)}", id);
                using (var reader = selectCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        using (var blobStream = reader.GetStream(nameof(FileChunk.Data)))
                        {
                            blobStream.CopyTo(fileStream);
                        }
                    }
                }
            }
        }

        public string UploadFile(string sourcePath)
        {
            var fileID = Guid.NewGuid().ToString();
            var file = new FileInfo(sourcePath);

            using (var fileStream = FileSystem.OpenFile(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var index = 0;
                var remainingBytes = fileStream.Length;
                while (remainingBytes > 0)
                {
                    var bufferSize = (int)Math.Min(maxChunkSize, remainingBytes);
                    var buffer = new byte[bufferSize];
                    remainingBytes -= fileStream.Read(buffer, 0, bufferSize);

                    var command = connection.CreateCommand();
                    command.CommandText =
                    $@"INSERT INTO {nameof(FileChunk)} (
                        {nameof(FileChunk.FileID)},
                        {nameof(FileChunk.ChunkIndex)},
                        {nameof(FileChunk.Data)}
                    ) VALUES (
                        @{nameof(FileChunk.FileID)},
                        @{nameof(FileChunk.ChunkIndex)},
                        @{nameof(FileChunk.Data)}
                    );";
                    command.Parameters.AddWithValue($"@{nameof(FileChunk.FileID)}", fileID);
                    command.Parameters.AddWithValue($"@{nameof(FileChunk.ChunkIndex)}", index++);
                    command.Parameters.AddWithValue($"@{nameof(FileChunk.Data)}", buffer);
                    command.ExecuteNonQuery();
                }
            }

            return fileID;
        }
    }
}
