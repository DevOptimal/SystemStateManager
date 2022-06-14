using System;
using System.IO;

namespace MachineStateManager.Core.FileSystem
{
    internal class DirectoryOriginator : IOriginator<DirectoryMemento>
    {
        public string Path { get; }

        public DirectoryOriginator(string path)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public DirectoryMemento GetState()
        {
            return new DirectoryMemento(Directory.Exists(Path));
        }

        public void SetState(DirectoryMemento memento)
        {
            if (Directory.Exists(Path))
            {
                if (!memento.Exists)
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            else // Path does not exist
            {
                if (memento.Exists)
                {
                    Directory.CreateDirectory(Path);
                }
            }
        }
    }
}
