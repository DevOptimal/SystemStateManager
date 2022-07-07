using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemUtilities.FileSystem;
using LiteDB;
using System;
using System.IO;
using System.Security.Cryptography;

namespace DevOptimal.SystemStateManager.Persistence.FileSystem.Caching
{
    internal class LiteDBBlobStore : IBlobStore
    {
        public IFileSystem FileSystem { get; }

        public LiteDBBlobStore(IFileSystem fileSystem)
        {
            FileSystem = fileSystem;
        }

        [BsonCtor]
        public LiteDBBlobStore(BsonDocument fileSystem)
        {
            FileSystem = BsonMapper.Global.ToObject<IFileSystem>(fileSystem);
        }

        public void DownloadFile(string id, string destinationPath)
        {
            using (var database = LiteDatabaseFactory.GetDatabase())
            {
                var fileStorage = database.FileStorage;
                var blobFile = fileStorage.FindById(id);
                if (blobFile == null)
                {
                    throw new FileNotFoundException();
                }

                var destinationFile = new FileInfo(destinationPath);
                using (var destinationStream = FileSystem.OpenFile(destinationFile.FullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    destinationStream.SetLength(0); // Delete existing file.

                    blobFile.CopyTo(destinationStream);
                }
            }
        }

        public string UploadFile(string sourcePath)
        {
            using (var database = LiteDatabaseFactory.GetDatabase())
            {
                var fileStorage = database.FileStorage;
                sourcePath = Path.GetFullPath(sourcePath);
                if (!FileSystem.FileExists(sourcePath))
                {
                    throw new FileNotFoundException();
                }
                using (var sourceStream = FileSystem.OpenFile(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
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
