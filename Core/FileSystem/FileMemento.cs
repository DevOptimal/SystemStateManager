namespace MachineStateManager.Core.FileSystem
{
    internal class FileMemento : IMemento
    {
        public string Hash { get; }

        public FileMemento(string hash)
        {
            Hash = hash;
        }
    }
}
