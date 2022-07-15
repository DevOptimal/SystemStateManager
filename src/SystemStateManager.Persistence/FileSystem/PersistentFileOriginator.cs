using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemUtilities.FileSystem;
using LiteDB;

namespace DevOptimal.SystemStateManager.Persistence.FileSystem
{
    internal class PersistentFileOriginator : FileOriginator
    {
        public PersistentFileOriginator(string path, IFileCache fileCache, IFileSystem fileSystem)
            : base(path, fileCache, fileSystem)
        {
        }

        [BsonCtor]
        public PersistentFileOriginator(string path, BsonDocument fileCache, BsonDocument fileSystem)
            : this(path, BsonMapper.Global.ToObject<IFileCache>(fileCache), BsonMapper.Global.ToObject<IFileSystem>(fileSystem))
        {
        }
    }
}
