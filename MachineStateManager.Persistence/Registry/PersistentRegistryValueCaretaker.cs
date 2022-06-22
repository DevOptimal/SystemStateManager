using bradselw.MachineStateManager.Registry;
using LiteDB;
using System;

namespace bradselw.MachineStateManager.Persistence.Registry
{
    internal class PersistentRegistryValueCaretaker : PersistentCaretaker<PersistentRegistryValueOriginator, RegistryValueMemento>
    {
        public PersistentRegistryValueCaretaker(string id, PersistentRegistryValueOriginator originator)
            : base(id, originator)
        {
        }

        [BsonCtor]
        public PersistentRegistryValueCaretaker(string _id, int processID, DateTime processStartTime, BsonDocument originator, BsonDocument memento)
            : base(_id, processID, processStartTime, BsonMapper.Global.ToObject<PersistentRegistryValueOriginator>(originator), BsonMapper.Global.ToObject<RegistryValueMemento>(memento))
        {
        }
    }
}
