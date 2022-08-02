using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemUtilities.FileSystem;
using LiteDB;

namespace DevOptimal.SystemStateManager.Persistence.FileSystem
{
    internal class PersistentDirectoryOriginator : DirectoryOriginator
    {
        public PersistentDirectoryOriginator(string path, IFileSystem fileSystem)
            : base(path, fileSystem)
        {
        }

        [BsonCtor]
        public PersistentDirectoryOriginator(string path, BsonDocument fileSystem)
            : this(path, LiteDatabaseFactory.Mapper.ToObject<IFileSystem>(fileSystem))
        {
        }
    }
}
