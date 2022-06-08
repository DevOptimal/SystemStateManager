using LiteDB;
using MachineStateManager.Core.FileSystem;
using System;
using System.IO;
using System.Security.Cryptography;

namespace MachineStateManager.Persistence.FileSystem.Caching
{
    internal class LiteDBBlobStore : IBlobStore
    {
        private readonly ILiteStorage<string> fileStorage;

        public LiteDBBlobStore(LiteDatabase database)
        {
            fileStorage = database.FileStorage;
        }

        public void DownloadFile(string hash, string destinationPath)
        {
            var blobFile = fileStorage.FindById(hash);
            if (blobFile == null)
            {
                throw new FileNotFoundException();
            }

            var destinationFile = new FileInfo(destinationPath);
            using var destinationStream = destinationFile.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            destinationStream.SetLength(0); // Delete existing file.

            blobFile.CopyTo(destinationStream);
        }

        public string UploadFile(string sourcePath)
        {
            var sourceFile = new FileInfo(sourcePath);
            if (!sourceFile.Exists)
            {
                throw new FileNotFoundException();
            }
            using var sourceStream = sourceFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);

            var hash = ComputeFileHash(sourceStream);
            var blobFile = fileStorage.FindById(hash);
            if (blobFile == null)
            {
                fileStorage.Upload(hash, hash, sourceStream);
            }

            return hash;
        }

        private static string ComputeFileHash(FileStream fileStream)
        {
            var previousPosition = fileStream.Position;

            fileStream.Position = 0;

            using var md5 = MD5.Create();

            var hash = md5.ComputeHash(fileStream);

            fileStream.Position = previousPosition;

            return Convert.ToHexString(hash);
        }
    }
}
