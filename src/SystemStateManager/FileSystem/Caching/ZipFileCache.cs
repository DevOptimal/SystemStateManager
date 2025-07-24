using DevOptimal.SystemUtilities.FileSystem.Abstractions;
using System;
using System.IO;
using System.IO.Compression;

namespace DevOptimal.SystemStateManager.FileSystem.Caching
{
    public class ZipFileCache : IFileCache
    {
        public IFileSystem FileSystem { get; set; }

        private readonly FileInfo zipFile;

        public ZipFileCache(IFileSystem fileSystem, FileInfo zipFile)
        {
            FileSystem = fileSystem;
            this.zipFile = zipFile;
        }

        public void DownloadFile(string id, string destinationPath)
        {
            using (var zipStream = zipFile.Open(FileMode.OpenOrCreate, FileAccess.Read, FileShare.None))
            using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                var zipEntry = zipArchive.GetEntry(id);
                using (var entryStream = zipEntry.Open())
                using (var fileStream = File.Open(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    entryStream.CopyTo(fileStream);
                }
            }
        }

        public string UploadFile(string sourcePath)
        {
            var id = Guid.NewGuid().ToString();
            using (var zipStream = zipFile.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Update))
            {
                var zipEntry = zipArchive.CreateEntry(id);
                using (var fileStream = File.Open(sourcePath, FileMode.Open, FileAccess.Read, FileShare.None))
                using (var entryStream = zipEntry.Open())
                {
                    fileStream.CopyTo(entryStream);
                }
            }
            return id;
        }
    }
}