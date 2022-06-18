using FileSystem;
using LiteDB;
using MachineStateManager.Core.FileSystem;

namespace MachineStateManager.Persistence.FileSystem
{
    internal class PersistentDirectoryOriginator : DirectoryOriginator
    {
        [BsonCtor]
        public PersistentDirectoryOriginator(string path, BsonDocument fileSystem)
            : this(path, BsonMapper.Global.ToObject<IFileSystem>(fileSystem))
        {
        }

        public PersistentDirectoryOriginator(string path, IFileSystem fileSystem)
            : base(path, fileSystem)
        {
        }
    }
}
