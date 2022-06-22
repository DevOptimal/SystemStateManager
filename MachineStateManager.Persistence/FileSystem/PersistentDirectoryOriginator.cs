using bradselw.MachineStateManager.FileSystem;
using bradselw.SystemResources.FileSystem.Proxy;
using LiteDB;

namespace bradselw.MachineStateManager.Persistence.FileSystem
{
    internal class PersistentDirectoryOriginator : DirectoryOriginator
    {
        [BsonCtor]
        public PersistentDirectoryOriginator(string path, BsonDocument fileSystem)
            : this(path, BsonMapper.Global.ToObject<IFileSystemProxy>(fileSystem))
        {
        }

        public PersistentDirectoryOriginator(string path, IFileSystemProxy fileSystem)
            : base(path, fileSystem)
        {
        }
    }
}
