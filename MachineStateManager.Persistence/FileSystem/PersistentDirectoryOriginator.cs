using DevOptimal.MachineStateManager.FileSystem;
using DevOptimal.System.Resources.FileSystem;
using LiteDB;

namespace DevOptimal.MachineStateManager.Persistence.FileSystem
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
