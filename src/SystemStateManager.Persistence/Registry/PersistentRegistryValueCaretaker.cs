using DevOptimal.SystemStateManager.Registry;
using LiteDB;
using System;

namespace DevOptimal.SystemStateManager.Persistence.Registry
{
    internal class PersistentRegistryValueCaretaker : PersistentCaretaker<PersistentRegistryValueOriginator, RegistryValueMemento>
    {
        public PersistentRegistryValueCaretaker(string id, PersistentRegistryValueOriginator originator)
            : base(id, originator)
        {
        }

        [BsonCtor]
        public PersistentRegistryValueCaretaker(string _id, int processID, DateTime processStartTime, BsonDocument originator, BsonDocument memento)
            : base(_id, processID, processStartTime, LiteDatabaseFactory.Mapper.ToObject<PersistentRegistryValueOriginator>(originator), LiteDatabaseFactory.Mapper.ToObject<RegistryValueMemento>(memento))
        {
        }
    }
}
