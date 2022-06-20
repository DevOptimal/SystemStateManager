using bradselw.SystemResources.Registry.Proxy;
using LiteDB;
using bradselw.MachineStateManager.Registry;
using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace bradselw.MachineStateManager.Persistence.Registry
{
    internal class PersistentRegistryValueCaretaker : PersistentCaretaker<PersistentRegistryValueOriginator, RegistryValueMemento>
    {
        public PersistentRegistryValueCaretaker(RegistryHive hive, RegistryView view, string subKey, string name, IRegistryProxy registry)
            : this(new PersistentRegistryValueOriginator(hive, view, subKey, name, registry))
        {
        }

        public PersistentRegistryValueCaretaker(PersistentRegistryValueOriginator originator)
            : base(GetID(originator), originator)
        {
        }

        [BsonCtor]
        public PersistentRegistryValueCaretaker(string _id, int processID, DateTime processStartTime, BsonDocument originator, BsonDocument memento)
            : base(_id, processID, processStartTime, BsonMapper.Global.ToObject<PersistentRegistryValueOriginator>(originator), BsonMapper.Global.ToObject<RegistryValueMemento>(memento))
        {
        }

        public static IEnumerable<IDisposable> GetAbandonedCaretakers(Dictionary<int, DateTime?> processes)
            => GetAbandonedCaretakers<PersistentRegistryValueCaretaker>(processes);

        private static string GetID(RegistryValueOriginator originator)
        {
            if (originator == null)
            {
                throw new ArgumentNullException(nameof(originator));
            }

            return string.Join("\\", originator.Hive, originator.View, originator.SubKey, originator.Name ?? "(Default)").ToLower();
        }
    }
}
