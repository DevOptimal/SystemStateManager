using bradselw.MachineStateManager.FileSystem;
using LiteDB;
using System;

namespace bradselw.MachineStateManager.Persistence.FileSystem
{
    internal class PersistentDirectoryCaretaker : PersistentCaretaker<PersistentDirectoryOriginator, DirectoryMemento>
    {
        public PersistentDirectoryCaretaker(string id, PersistentDirectoryOriginator originator)
            : base(id, originator)
        {
        }

        [BsonCtor]
        public PersistentDirectoryCaretaker(string _id, int processID, DateTime processStartTime, BsonDocument originator, BsonDocument memento)
            : base(_id, processID, processStartTime, BsonMapper.Global.ToObject<PersistentDirectoryOriginator>(originator), BsonMapper.Global.ToObject<DirectoryMemento>(memento))
        {
        }
    }
}
