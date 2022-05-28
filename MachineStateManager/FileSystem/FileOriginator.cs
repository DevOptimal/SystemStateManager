﻿namespace MachineStateManager.FileSystem
{
    internal class FileOriginator : IOriginator<FileMemento>
    {
        public string Path { get; set; }

        /// <summary>
        /// Files can be big, so their contents cannot be stored in memory. Instead, persist the content to a blob
        /// store, indexed by its hash. The hash will be stored in the FileMemento.
        /// </summary>
        public IBlobStore FileCache { get; set; }

        public FileOriginator(string path, IBlobStore fileCache)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            FileCache = fileCache ?? throw new ArgumentNullException(nameof(fileCache));
        }

        public FileMemento GetState()
        {
            if (!File.Exists(Path))
            {
                return new FileMemento(null);
            }

            return new FileMemento(FileCache.PushFile(Path));
        }

        public void SetState(FileMemento memento)
        {
            if (memento.Hash == null)
            {
                if (File.Exists(Path))
                {
                    File.Delete(Path);
                }
            }
            else
            {
                FileCache.PullFile(memento.Hash, Path);
            }
        }
    }
}
