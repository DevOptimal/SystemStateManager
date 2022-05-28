namespace MachineStateManager.FileSystem
{
    internal class FileMemento : IMemento
    {
        public string? Hash { get; set; }

        public FileMemento(string? hash)
        {
            Hash = hash;
        }
    }
}
