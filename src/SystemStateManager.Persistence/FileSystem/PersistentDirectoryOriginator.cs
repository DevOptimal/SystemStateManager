using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemUtilities.FileSystem;
using LiteDB;

namespace DevOptimal.SystemStateManager.Persistence.FileSystem
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
