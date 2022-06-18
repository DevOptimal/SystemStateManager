﻿using FileSystem;
using LiteDB;
using MachineStateManager.Core.FileSystem;

namespace MachineStateManager.Persistence.FileSystem
{
    internal class PersistentFileOriginator : FileOriginator
    {
        [BsonCtor]
        public PersistentFileOriginator(string path, BsonDocument fileCache, BsonDocument fileSystem)
            : this(path, BsonMapper.Global.ToObject<IBlobStore>(fileCache), BsonMapper.Global.ToObject<IFileSystem>(fileSystem))
        {
        }

        public PersistentFileOriginator(string path, IBlobStore fileCache, IFileSystem fileSystem)
            : base(path, fileCache, fileSystem)
        {
        }
    }
}
