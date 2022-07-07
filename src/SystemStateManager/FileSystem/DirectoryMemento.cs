namespace DevOptimal.SystemStateManager.FileSystem
{
    internal class DirectoryMemento : IMemento
    {
        public bool Exists { get; }

        public DirectoryMemento(bool exists)
        {
            Exists = exists;
        }
    }
}
