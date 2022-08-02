﻿using DevOptimal.SystemStateManager.FileSystem;
using LiteDB;
using System;

namespace DevOptimal.SystemStateManager.Persistence.FileSystem
{
    internal class PersistentDirectoryCaretaker : PersistentCaretaker<PersistentDirectoryOriginator, DirectoryMemento>
    {
        public PersistentDirectoryCaretaker(string id, PersistentDirectoryOriginator originator)
            : base(id, originator)
        {
        }

        [BsonCtor]
        public PersistentDirectoryCaretaker(string _id, int processID, DateTime processStartTime, BsonDocument originator, BsonDocument memento)
            : base(_id, processID, processStartTime, LiteDatabaseFactory.Mapper.ToObject<PersistentDirectoryOriginator>(originator), LiteDatabaseFactory.Mapper.ToObject<DirectoryMemento>(memento))
        {
        }
    }
}
