using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemUtilities.FileSystem;
using System;

namespace DevOptimal.SystemStateManager.Persistence.FileSystem.Caching
{
    internal class ZipFileCache : IFileCache
    {
        public IFileSystem FileSystem { get; }

        public ZipFileCache(IFileSystem fileSystem)
        {
            FileSystem = fileSystem;
        }

        public void DownloadFile(string id, string destinationPath)
        {
            throw new NotImplementedException();
        }

        public string UploadFile(string sourcePath)
        {
            throw new NotImplementedException();
        }
    }
}
