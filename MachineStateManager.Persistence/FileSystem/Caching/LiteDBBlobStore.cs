using bradselw.MachineStateManager.FileSystem;
using System;
using System.IO;
using System.Security.Cryptography;

namespace bradselw.MachineStateManager.Persistence.FileSystem.Caching
{
    internal class LiteDBBlobStore : IBlobStore
    {
        public void DownloadFile(string id, string destinationPath)
        {
            using (var database = PersistentFileCaretaker.GetDatabase())
            {
                var fileStorage = database.FileStorage;
                var blobFile = fileStorage.FindById(id);
                if (blobFile == null)
                {
                    throw new FileNotFoundException();
                }

                var destinationFile = new FileInfo(destinationPath);
                using (var destinationStream = destinationFile.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    destinationStream.SetLength(0); // Delete existing file.

                    blobFile.CopyTo(destinationStream);
                }
            }
        }

        public string UploadFile(string sourcePath)
        {
            using (var database = PersistentFileCaretaker.GetDatabase())
            {
                var fileStorage = database.FileStorage;
                var sourceFile = new FileInfo(sourcePath);
                if (!sourceFile.Exists)
                {
                    throw new FileNotFoundException();
                }
                using (var sourceStream = sourceFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                {

                    var id = ComputeFileHash(sourceStream);
                    var blobFile = fileStorage.FindById(id);
                    if (blobFile == null)
                    {
                        fileStorage.Upload(id, id, sourceStream);
                    }

                    return id;
                }
            }
        }

        private static string ComputeFileHash(FileStream fileStream)
        {
            var previousPosition = fileStream.Position;

            fileStream.Position = 0;

            using (var md5 = MD5.Create())
            {

                var hash = md5.ComputeHash(fileStream);

                fileStream.Position = previousPosition;

                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }
    }
}
