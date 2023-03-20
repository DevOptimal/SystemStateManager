using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Security.Cryptography;

namespace DevOptimal.SystemStateManager.Persistence.SQLite.Tests
{
    [TestClass]
    public class FileStoreTests : TestBase
    {
        private const string filePath = @"";
        private const string fileID = "A6BA9119-75F6-4630-B27B-C00238CCF751";

        public class FileChunk
        {
            public int FileID { get; set; }
             
            public int ChunkIndex { get; set; }

            public byte[] Data { get; set; }
        }

        [TestMethod]
        public void UploadFile()
        {
            const int maxChunkSize = 999999953;

            var createCommand = connection.CreateCommand();
            createCommand.CommandText =
            $@"CREATE TABLE IF NOT EXISTS {nameof(FileChunk)} (
                {nameof(FileChunk.FileID)} INTEGER NOT NULL,
                {nameof(FileChunk.ChunkIndex)} INTEGER NOT NULL,
                {nameof(FileChunk.Data)} BLOB,
                PRIMARY KEY ({nameof(FileChunk.FileID)}, {nameof(FileChunk.ChunkIndex)})
            );";
            createCommand.ExecuteNonQuery();

            var file = new FileInfo(filePath);
            using (var fileStream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var index = 0;
                var remainingBytes = file.Length;
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

                Console.WriteLine("DONE");
            }
        }

        [TestMethod]
        public void DownloadFile()
        {
            var destinationPath = Path.Combine(TestContext.ResultsDirectory, Path.GetFileName(filePath));
            var destinationFile = new FileInfo(destinationPath);
            using (var fileStream = File.Open(destinationFile.FullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                fileStream.SetLength(0); // Delete existing file.

                var selectCommand = connection.CreateCommand();
                selectCommand.CommandText = $@"SELECT {nameof(FileChunk.Data)} FROM {nameof(FileChunk)} WHERE {nameof(FileChunk.FileID)} = @{nameof(FileChunk.FileID)} ORDER BY {nameof(FileChunk.ChunkIndex)} ASC";
                selectCommand.Parameters.AddWithValue($"@{nameof(FileChunk.FileID)}", fileID);
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
            }
        }

        [TestMethod]
        public void ComputeHash()
        {
            const int maxChunkSize = 100000;

            var file = new FileInfo(filePath);
            var expectedHash = BitConverter.ToString(SHA1.HashData(File.ReadAllBytes(file.FullName))).Replace("-", string.Empty);
            using var fs = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var sha1 = SHA1.Create();
            var remainingBytes = file.Length;
            while (remainingBytes > 0)
            {
                var chunkSize = Math.Min(maxChunkSize, remainingBytes);
                var buffer = new byte[chunkSize];
                remainingBytes -= fs.Read(buffer, 0, (int)chunkSize);
                sha1.TransformBlock(buffer, 0, (int)chunkSize, buffer, 0);
            }
            sha1.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            var actualHash = BitConverter.ToString(sha1.Hash).Replace("-", string.Empty);

            Assert.AreEqual(expectedHash, actualHash);
        }
    }
}