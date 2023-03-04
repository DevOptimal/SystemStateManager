using Microsoft.VisualStudio.TestTools.UnitTesting;
using SQLite;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace SystemStateManager.Persistence.SQLite.Tests
{
    [TestClass]
    public class FileStoreTests
    {
        private const string filePath = @"";
        private const string fileID = "";

        public TestContext TestContext { get; set; }

        public class FileRecord
        {
            [PrimaryKey, AutoIncrement]
            public int ID { get; set; }

            [Indexed]
            public string FileID { get; set; }

            public string ChunkHash { get; set; }

            public int ChunkIndex { get; set; }
        }

        public class FileChunk
        {
            [PrimaryKey]
            public string Hash { get; set; }

            public byte[] Data { get; set; }
        }

        [TestMethod]
        public void UploadFile()
        {
            const int maxChunkSize = 999999953;

            using var db = GetConnection();
            db.CreateTable<FileRecord>();
            db.CreateTable<FileChunk>();

            var file = new FileInfo(filePath);
            using var fileStream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);

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
                    db.Insert(new FileChunk { Data = buffer, Hash = hash });
                }
                catch (SQLiteException ex) when (ex.Message.Equals($"UNIQUE constraint failed: {nameof(FileChunk)}.{nameof(FileChunk.Hash)}"))
                {
                    // Ignorable
                }
                finally
                {
                    buffer = null;
                }
                db.Insert(new FileRecord
                {
                    FileID = fileID,
                    ChunkHash = hash,
                    ChunkIndex = index++
                });
            }

            Console.WriteLine("DONE");
        }

        [TestMethod]
        public void DownloadFile()
        {
            var destinationPath = Path.Combine(TestContext.ResultsDirectory, Path.GetFileName(filePath));
            var destinationFile = new FileInfo(destinationPath);
            using (var db = GetConnection())
            using (var fileStream = File.Open(destinationFile.FullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                fileStream.SetLength(0); // Delete existing file.
                var records = db.Query<FileRecord>($"SELECT * FROM {nameof(FileRecord)} WHERE FileID = '{fileID}'").OrderBy(r => r.ChunkIndex).ToList();
                foreach (var record in records)
                {
                    var chunk = db.Find<FileChunk>(record.ChunkHash);
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

        private SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(Path.Combine(TestContext.ResultsDirectory, "persistence.sqlite"));
        }
    }
}