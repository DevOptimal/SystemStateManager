using LiteDB;
using MachineStateManager.Core.Registry;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace MachineStateManager.Persistence.Registry
{
    internal class PersistentRegistryValueCaretaker : PersistentCaretaker<RegistryValueOriginator, RegistryValueMemento>
    {
        static PersistentRegistryValueCaretaker()
        {
            var currentProcess = Process.GetCurrentProcess();

            BsonMapper.Global.RegisterType(
                serialize: (caretaker) =>
                {
                    var dictionary = new Dictionary<string, BsonValue>
                    {
                        ["_id"] = caretaker.ID,
                        [nameof(Process) + nameof(Process.Id)] = currentProcess.Id,
                        [nameof(Process) + nameof(Process.StartTime)] = currentProcess.StartTime,
                        [nameof(Originator)] = BsonMapper.Global.ToDocument(caretaker.Originator),
                        [nameof(Memento)] = BsonMapper.Global.ToDocument(caretaker.Memento),
                    };
                    return new BsonDocument(dictionary);
                },
                deserialize: (bson) =>
                {
                    var originator = new RegistryValueOriginator(
                        (RegistryHive)Enum.Parse(typeof(RegistryHive), bson[nameof(Originator)][nameof(RegistryValueOriginator.Hive)].AsString),
                        (RegistryView)Enum.Parse(typeof(RegistryView), bson[nameof(Originator)][nameof(RegistryValueOriginator.View)].AsString),
                        bson[nameof(Originator)][nameof(RegistryValueOriginator.SubKey)].AsString,
                        bson[nameof(Originator)][nameof(RegistryValueOriginator.Name)].AsString);
                    var memento = new RegistryValueMemento(
                        bson[nameof(Memento)][nameof(RegistryValueMemento.Value)],
                        (RegistryValueKind)Enum.Parse(typeof(RegistryValueKind), bson[nameof(Memento)][nameof(RegistryValueMemento.Kind)].AsString));
                    return new PersistentRegistryValueCaretaker(bson["_id"].AsString, bson[nameof(ProcessID)].AsInt32, bson[nameof(ProcessStartTime)].AsDateTime, originator, memento);
                }
            );
        }

        public PersistentRegistryValueCaretaker(RegistryHive hive, RegistryView view, string subKey, string name)
            : this(new RegistryValueOriginator(hive, view, subKey, name))
        {
        }

        public PersistentRegistryValueCaretaker(RegistryValueOriginator originator)
            : base(GetID(originator), originator)
        {
        }

        public PersistentRegistryValueCaretaker(string id, int processID, DateTime processStartTime, RegistryValueOriginator originator, RegistryValueMemento memento)
            : base(id, processID, processStartTime, originator, memento)
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
