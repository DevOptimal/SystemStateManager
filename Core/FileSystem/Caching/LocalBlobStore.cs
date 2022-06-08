using System.Security.Cryptography;

namespace MachineStateManager.Core.FileSystem.Caching
{
    internal class LocalBlobStore : IBlobStore
    {
        private readonly DirectoryInfo rootDirectory;

        public LocalBlobStore(string rootDirectoryPath)
        {
            rootDirectory = new DirectoryInfo(rootDirectoryPath);
            if (!rootDirectory.Exists)
            {
                rootDirectory.Create();
            }
        }

        public void DownloadFile(string hash, string destinationPath)
        {
            var blobFile = new FileInfo(Path.Combine(rootDirectory.FullName, hash));
            if (!blobFile.Exists)
            {
                throw new FileNotFoundException();
            }
            using var blobStream = blobFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            
            var destinationFile = new FileInfo(destinationPath);
            using var destinationStream = destinationFile.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            destinationStream.SetLength(0); // Delete existing file.

            blobStream.CopyTo(destinationStream);
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
            var blobFile = new FileInfo(Path.Combine(rootDirectory.FullName, hash));
            if (!blobFile.Exists)
            {
                using var blobStream = blobFile.Open(FileMode.CreateNew, FileAccess.Write, FileShare.None);

                sourceStream.CopyTo(blobStream);
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
