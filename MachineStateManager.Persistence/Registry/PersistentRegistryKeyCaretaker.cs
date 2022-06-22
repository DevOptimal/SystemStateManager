using bradselw.MachineStateManager.Registry;
using LiteDB;
using System;

namespace bradselw.MachineStateManager.Persistence.Registry
{
    internal class PersistentRegistryKeyCaretaker : PersistentCaretaker<PersistentRegistryKeyOriginator, RegistryKeyMemento>
    {
        public PersistentRegistryKeyCaretaker(string id, PersistentRegistryKeyOriginator originator)
            : base(id, originator)
        {
        }

        [BsonCtor]
        public PersistentRegistryKeyCaretaker(string _id, int processID, DateTime processStartTime, BsonDocument originator, BsonDocument memento)
            : base(_id, processID, processStartTime, BsonMapper.Global.ToObject<PersistentRegistryKeyOriginator>(originator), BsonMapper.Global.ToObject<RegistryKeyMemento>(memento))
        {
        }
    }
}
