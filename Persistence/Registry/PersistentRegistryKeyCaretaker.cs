using LiteDB;
using MachineStateManager.Core.Registry;
using Microsoft.Win32;
using Registry;
using System;
using System.Collections.Generic;

namespace MachineStateManager.Persistence.Registry
{
    internal class PersistentRegistryKeyCaretaker : PersistentCaretaker<PersistentRegistryKeyOriginator, RegistryKeyMemento>
    {
        public PersistentRegistryKeyCaretaker(RegistryHive hive, RegistryView view, string subKey, IRegistry registry)
            : this(new PersistentRegistryKeyOriginator(hive, view, subKey, registry))
        {
        }

        public PersistentRegistryKeyCaretaker(PersistentRegistryKeyOriginator originator)
            : base(GetID(originator), originator)
        {
        }

        [BsonCtor]
        public PersistentRegistryKeyCaretaker(string _id, int processID, DateTime processStartTime, BsonDocument originator, BsonDocument memento)
            : base(_id, processID, processStartTime, BsonMapper.Global.ToObject<PersistentRegistryKeyOriginator>(originator), BsonMapper.Global.ToObject<RegistryKeyMemento>(memento))
        {
        }

        public static IEnumerable<IDisposable> GetAbandonedCaretakers(Dictionary<int, DateTime?> processes)
            => GetAbandonedCaretakers<PersistentRegistryKeyCaretaker>(processes);

        private static string GetID(RegistryKeyOriginator originator)
        {
            if (originator == null)
            {
                throw new ArgumentNullException(nameof(originator));
            }

            return string.Join("\\", originator.Hive, originator.View, originator.SubKey).ToLower();
        }
    }
}
