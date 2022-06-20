﻿using bradselw.SystemResources.FileSystem.Proxy;
using System;

namespace bradselw.MachineStateManager.FileSystem
{
    internal class DirectoryOriginator : IOriginator<DirectoryMemento>
    {
        public string Path { get; }

        public IFileSystemProxy FileSystem { get; }

        public DirectoryOriginator(string path, IFileSystemProxy fileSystem)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public DirectoryMemento GetState()
        {
            return new DirectoryMemento(FileSystem.DirectoryExists(Path));
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