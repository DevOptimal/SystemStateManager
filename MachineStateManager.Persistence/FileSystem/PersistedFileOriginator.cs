using MachineStateManager.FileSystem;

namespace MachineStateManager.Persistence.FileSystem
{
    internal class PersistedFileOriginator : FileOriginator, IPersistedOriginator<FileMemento>
    {
        public string ID => Path.ToLower();

        public PersistedFileOriginator(string path, IBlobStore fileCache) : base(path, fileCache)
        {
        }
    }
}
