using LiteDB;
using MachineStateManager.Core.Registry;
using Microsoft.Win32;
using Registry;
using System;
using System.Collections.Generic;

namespace MachineStateManager.Persistence.Registry
{
    internal class PersistentRegistryKeyCaretaker : PersistentCaretaker<RegistryKeyOriginator, RegistryKeyMemento>
    {
        static PersistentRegistryKeyCaretaker()
        {
            BsonMapper.Global.RegisterType(SerializeOriginator, DeserializeOriginator);
        }

        public PersistentRegistryKeyCaretaker(RegistryHive hive, RegistryView view, string subKey, IRegistry registry)
            : this(new RegistryKeyOriginator(hive, view, subKey, registry))
        {
        }

        public PersistentRegistryKeyCaretaker(RegistryKeyOriginator originator)
            : base(GetID(originator), originator)
        {
        }

        [BsonCtor]
        public PersistentRegistryKeyCaretaker(string _id, int processID, DateTime processStartTime, BsonDocument originator, BsonDocument memento)
            : base(_id, processID, processStartTime, BsonMapper.Global.ToObject<RegistryKeyOriginator>(originator), BsonMapper.Global.ToObject<RegistryKeyMemento>(memento))
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

        private static BsonValue SerializeOriginator(RegistryKeyOriginator originator)
        {
            return new BsonDocument
            {
                [nameof(RegistryKeyOriginator.Hive)] = originator.Hive.ToString(),
                [nameof(RegistryKeyOriginator.View)] = originator.View.ToString(),
                [nameof(RegistryKeyOriginator.SubKey)] = originator.SubKey,
                [nameof(RegistryKeyOriginator.Registry)] = BsonMapper.Global.ToDocument(originator.Registry),
            };
        }

        private static RegistryKeyOriginator DeserializeOriginator(BsonValue bson)
        {
            return new RegistryKeyOriginator(
                hive: (RegistryHive)Enum.Parse(typeof(RegistryHive), bson[nameof(RegistryKeyOriginator.Hive)]),
                view: (RegistryView)Enum.Parse(typeof(RegistryView), bson[nameof(RegistryKeyOriginator.View)]),
                subKey: bson[nameof(RegistryKeyOriginator.SubKey)],
                registry: BsonMapper.Global.ToObject<IRegistry>(bson[nameof(RegistryKeyOriginator.Registry)].AsDocument));
        }
    }
}
