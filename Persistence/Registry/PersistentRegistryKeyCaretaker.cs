using LiteDB;
using MachineStateManager.Core.Registry;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MachineStateManager.Persistence.Registry
{
    internal class PersistentRegistryKeyCaretaker : PersistentCaretaker<RegistryKeyOriginator, RegistryKeyMemento>
    {
        static PersistentRegistryKeyCaretaker()
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
                    var originator = new RegistryKeyOriginator(
                        (RegistryHive)Enum.Parse(typeof(RegistryHive), bson[nameof(Originator)][nameof(RegistryKeyOriginator.Hive)].AsString),
                        (RegistryView)Enum.Parse(typeof(RegistryView), bson[nameof(Originator)][nameof(RegistryKeyOriginator.View)].AsString),
                        bson[nameof(Originator)][nameof(RegistryKeyOriginator.SubKey)].AsString);
                    var memento = new RegistryKeyMemento(
                        bson[nameof(Memento)][nameof(RegistryKeyMemento.Exists)].AsBoolean);
                    return new PersistentRegistryKeyCaretaker(bson["_id"].AsString, bson[nameof(ProcessID)].AsInt32, bson[nameof(ProcessStartTime)].AsDateTime, originator, memento);
                }
            );
        }

        public PersistentRegistryKeyCaretaker(RegistryHive hive, RegistryView view, string subKey)
            : this(new RegistryKeyOriginator(hive, view, subKey))
        {
        }

        public PersistentRegistryKeyCaretaker(RegistryKeyOriginator originator)
            : base(GetID(originator), originator)
        {
        }

        public PersistentRegistryKeyCaretaker(string id, int processID, DateTime processStartTime, RegistryKeyOriginator originator, RegistryKeyMemento memento)
            : base(id, processID, processStartTime, originator, memento)
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
