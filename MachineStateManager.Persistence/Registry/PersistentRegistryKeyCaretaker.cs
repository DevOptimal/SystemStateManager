using bradselw.SystemResources.Registry.Proxy;
using LiteDB;
using bradselw.MachineStateManager.Registry;
using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace bradselw.MachineStateManager.Persistence.Registry
{
    internal class PersistentRegistryKeyCaretaker : PersistentCaretaker<PersistentRegistryKeyOriginator, RegistryKeyMemento>
    {
        public PersistentRegistryKeyCaretaker(RegistryHive hive, RegistryView view, string subKey, IRegistryProxy registry)
            : this(new PersistentRegistryKeyOriginator(hive, view, subKey, registry))
        {
        }

        public PersistentRegistryKeyCaretaker(PersistentRegistryKeyOriginator originator)
            : base(originator)
        {
        }

        [BsonCtor]
        public PersistentRegistryKeyCaretaker(string _id, int processID, DateTime processStartTime, BsonDocument originator, BsonDocument memento)
            : base(_id, processID, processStartTime, BsonMapper.Global.ToObject<PersistentRegistryKeyOriginator>(originator), BsonMapper.Global.ToObject<RegistryKeyMemento>(memento))
        {
        }

        public static IEnumerable<ICaretaker> GetAbandonedCaretakers(Dictionary<int, DateTime?> processes)
            => GetAbandonedCaretakers<PersistentRegistryKeyCaretaker>(processes);
    }
}
