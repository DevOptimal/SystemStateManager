using DevOptimal.SystemUtilities.FileSystem;
using System;

namespace DevOptimal.SystemStateManager.FileSystem
{
    internal class DirectoryOriginator : IOriginator<DirectoryMemento>
    {
        public string Path { get; }

        public IFileSystem FileSystem { get; }

        public DirectoryOriginator(string path, IFileSystem fileSystem)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Path = System.IO.Path.GetFullPath(path);
            FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public DirectoryMemento GetState()
        {
            return new DirectoryMemento
            {
                Exists = FileSystem.DirectoryExists(Path)
            };
        }

        public void SetState(DirectoryMemento memento)
        {
            if (FileSystem.DirectoryExists(Path))
            {
                if (!memento.Exists)
                {
                    FileSystem.DeleteDirectory(Path, recursive: true);
                }
            }
            else // Path does not exist
            {
                if (memento.Exists)
                {
                    FileSystem.CreateDirectory(Path);
                }
            }
        }
    }
}
