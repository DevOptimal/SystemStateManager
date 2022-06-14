using FileSystem;
using System;
using System.IO;

namespace MachineStateManager.Core.FileSystem.Caching
{
    internal class LocalBlobStore : IBlobStore
    {
        public string RootPath { get; }

        public IFileSystem FileSystem { get; }

        public LocalBlobStore(string rootDirectoryPath, IFileSystem fileSystem)
        {
            FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

            RootPath = Path.GetFullPath(rootDirectoryPath);
            if (!fileSystem.DirectoryExists(RootPath))
            {
                fileSystem.CreateDirectory(RootPath);
            }
        }

        public void DownloadFile(string id, string destinationPath)
        {
            var blobPath = Path.Combine(RootPath, id);
            if (!FileSystem.FileExists(blobPath))
            {
                throw new FileNotFoundException();
            }

            FileSystem.CopyFile(blobPath, destinationPath, overwrite: true);
        }

        public string UploadFile(string sourcePath)
        {
            if (!FileSystem.FileExists(sourcePath))
            {
                throw new FileNotFoundException();
            }

            var id = Guid.NewGuid().ToString();

            var blobPath = Path.Combine(RootPath, id);

            FileSystem.CopyFile(sourcePath, blobPath, overwrite: false);

            return id;
        }
    }
}
