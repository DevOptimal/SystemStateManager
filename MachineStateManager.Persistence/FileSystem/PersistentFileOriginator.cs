using bradselw.SystemResources.FileSystem.Proxy;
using LiteDB;
using bradselw.MachineStateManager.FileSystem;

namespace bradselw.MachineStateManager.Persistence.FileSystem
{
    internal class PersistentFileOriginator : FileOriginator
    {
        [BsonCtor]
        public PersistentFileOriginator(string path, BsonDocument fileCache, BsonDocument fileSystem)
            : this(path, BsonMapper.Global.ToObject<IBlobStore>(fileCache), BsonMapper.Global.ToObject<IFileSystemProxy>(fileSystem))
        {
        }

        public PersistentFileOriginator(string path, IBlobStore fileCache, IFileSystemProxy fileSystem)
            : base(path, fileCache, fileSystem)
        {
        }
    }
}
