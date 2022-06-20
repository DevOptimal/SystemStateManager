namespace bradselw.MachineStateManager.FileSystem
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
