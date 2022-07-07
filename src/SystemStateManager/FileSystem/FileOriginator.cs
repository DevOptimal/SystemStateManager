using DevOptimal.SystemUtilities.FileSystem;
using System;

namespace DevOptimal.SystemStateManager.FileSystem
{
    internal class FileOriginator : IOriginator<FileMemento>
    {
        public string Path { get; }

        /// <summary>
        /// Files can be big, so their contents cannot be stored in memory. Instead, persist the content to a blob
        /// store, indexed by its hash. The hash will be stored in the FileMemento.
        /// </summary>
        public IBlobStore FileCache { get; }

        public IFileSystem FileSystem { get; }

        public FileOriginator(string path, IBlobStore fileCache, IFileSystem fileSystem)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Path = global::System.IO.Path.GetFullPath(path);
            FileCache = fileCache ?? throw new ArgumentNullException(nameof(fileCache));
            FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public FileMemento GetState()
        {
            if (!FileSystem.FileExists(Path))
            {
                return new FileMemento(null);
            }

            return new FileMemento(FileCache.UploadFile(Path));
        }

        public void SetState(FileMemento memento)
        {
            var directoryPath = global::System.IO.Path.GetDirectoryName(Path);
            if (!FileSystem.DirectoryExists(directoryPath))
            {
                FileSystem.CreateDirectory(directoryPath);
            }

            if (memento.Hash == null)
            {
                if (FileSystem.FileExists(Path))
                {
                    FileSystem.DeleteFile(Path);
                }
            }
            else
            {
                FileCache.DownloadFile(memento.Hash, Path);
            }
        }
    }
}
