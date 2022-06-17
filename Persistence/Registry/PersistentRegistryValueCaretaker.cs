using LiteDB;
using MachineStateManager.Core.Registry;
using Microsoft.Win32;
using Registry;
using System;
using System.Collections.Generic;

namespace MachineStateManager.Persistence.Registry
{
    internal class PersistentRegistryValueCaretaker : PersistentCaretaker<RegistryValueOriginator, RegistryValueMemento>
    {
        static PersistentRegistryValueCaretaker()
        {
            BsonMapper.Global.RegisterType(SerializeOriginator, DeserializeOriginator);
        }

        public PersistentRegistryValueCaretaker(RegistryHive hive, RegistryView view, string subKey, string name, IRegistry registry)
            : this(new RegistryValueOriginator(hive, view, subKey, name, registry))
        {
        }

        public PersistentRegistryValueCaretaker(RegistryValueOriginator originator)
            : base(GetID(originator), originator)
        {
        }

        [BsonCtor]
        public PersistentRegistryValueCaretaker(string _id, int processID, DateTime processStartTime, BsonDocument originator, BsonDocument memento)
            : base(_id, processID, processStartTime, BsonMapper.Global.ToObject<RegistryValueOriginator>(originator), BsonMapper.Global.ToObject<RegistryValueMemento>(memento))
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

        private static BsonValue SerializeOriginator(RegistryValueOriginator originator)
        {
            return new BsonDocument
            {
                [nameof(RegistryValueOriginator.Hive)] = originator.Hive.ToString(),
                [nameof(RegistryValueOriginator.View)] = originator.View.ToString(),
                [nameof(RegistryValueOriginator.SubKey)] = originator.SubKey,
                [nameof(RegistryValueOriginator.Name)] = originator.Name,
                [nameof(RegistryValueOriginator.Registry)] = BsonMapper.Global.ToDocument(originator.Registry),
            };
        }

        private static RegistryValueOriginator DeserializeOriginator(BsonValue bson)
        {
            return new RegistryValueOriginator(
                hive: (RegistryHive)Enum.Parse(typeof(RegistryHive), bson[nameof(RegistryValueOriginator.Hive)]),
                view: (RegistryView)Enum.Parse(typeof(RegistryView), bson[nameof(RegistryValueOriginator.View)]),
                subKey: bson[nameof(RegistryValueOriginator.SubKey)],
                name: bson[nameof(RegistryValueOriginator.Name)],
                registry: BsonMapper.Global.ToObject<IRegistry>(bson[nameof(RegistryValueOriginator.Registry)].AsDocument));
        }
    }
}
