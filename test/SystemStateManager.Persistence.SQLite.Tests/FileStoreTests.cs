using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace DevOptimal.SystemStateManager.Persistence.SQLite.Tests
{
    [TestClass]
    public class FileStoreTests : TestBase
    {
        private const string filePath = @"";
        private const string fileID = "";

        public class FileRecord
        {
            public int ID { get; set; }

            public string FileID { get; set; }

            public string ChunkHash { get; set; }

            public int ChunkIndex { get; set; }
        }

        public class FileChunk
        {
            public string Hash { get; set; }

            public byte[] Data { get; set; }
        }

        [TestMethod]
        public void UploadFile()
        {
            const int maxChunkSize = 10000000;//999999953;

            connection.Execute($@"CREATE TABLE IF NOT EXISTS {nameof(FileRecord)}s (
                {nameof(FileRecord.ID)} INTEGER PRIMARY KEY,
                {nameof(FileRecord.FileID)} TEXT,
                {nameof(FileRecord.ChunkHash)} TEXT,
                {nameof(FileRecord.ChunkIndex)} INTEGER
            );");
            connection.Execute($@"CREATE INDEX IF NOT EXISTS {nameof(FileRecord.FileID)} ON {nameof(FileRecord)}s (
                {nameof(FileRecord.FileID)}
            );");
            connection.Execute($@"CREATE TABLE IF NOT EXISTS {nameof(FileChunk)}s (
                {nameof(FileChunk.Hash)} TEXT PRIMARY KEY,
                {nameof(FileChunk.Data)} BLOB
            );");

            var file = new FileInfo(filePath);
            using (var fileStream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var index = 0;
                var remainingBytes = file.Length;
                while (remainingBytes > 0)
                {
                    var chunkSize = Math.Min(maxChunkSize, remainingBytes);
                    var buffer = new byte[chunkSize];
                    remainingBytes -= fileStream.Read(buffer, 0, (int)chunkSize);
                    var hash = BitConverter.ToString(SHA1.HashData(buffer)).Replace("-", string.Empty);
                    try
                    {
                        connection.Execute(
                        sql: $@"INSERT INTO {nameof(FileChunk)}s ({nameof(FileChunk.Hash)}, {nameof(FileChunk.Data)}) VALUES (@{nameof(FileChunk.Hash)}, @{nameof(FileChunk.Data)})",
                        param: new FileChunk
                        {
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
                    connection.Execute(
                        sql: $@"INSERT INTO {nameof(FileRecord)}s ({nameof(FileRecord.FileID)}, {nameof(FileRecord.ChunkHash)}, {nameof(FileRecord.ChunkIndex)}) VALUES (@{nameof(FileRecord.FileID)}, @{nameof(FileRecord.ChunkHash)}, @{nameof(FileRecord.ChunkIndex)})",
                        param: new FileRecord
                        {
                            FileID = fileID,
                            ChunkHash = hash,
                            ChunkIndex = index++
                        });
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
                var records = connection.Query<FileRecord>($"SELECT * FROM {nameof(FileRecord)} WHERE {nameof(FileRecord.FileID)} = '{fileID}'").OrderBy(r => r.ChunkIndex).ToList();
                foreach (var record in records)
                {
                    var chunk = connection.QuerySingle<FileChunk>($"SELECT * FROM {nameof(FileChunk)} WHERE {nameof(FileChunk.Hash)} = '{record.ChunkHash}'");
                    fileStream.Write(chunk.Data, 0, chunk.Data.Length);
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