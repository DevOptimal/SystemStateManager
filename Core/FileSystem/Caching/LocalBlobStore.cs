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

        public void DownloadFile(string id, string destinationPath)
        {
            var blobFile = new FileInfo(Path.Combine(rootDirectory.FullName, id));
            if (!blobFile.Exists)
            {
                throw new FileNotFoundException();
            }

            blobFile.CopyTo(destinationPath);
        }

        public string UploadFile(string sourcePath)
        {
            var sourceFile = new FileInfo(sourcePath);
            if (!sourceFile.Exists)
            {
                throw new FileNotFoundException();
            }

            var id = Guid.NewGuid().ToString();

            var blobFile = new FileInfo(Path.Combine(rootDirectory.FullName, id));

            sourceFile.CopyTo(blobFile.FullName);

            return id;
        }
    }
}
