using LiteDB;
using MachineStateManager.Core.Registry;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace MachineStateManager.Persistence.Registry
{
    [SupportedOSPlatform("windows")]
    internal class PersistentRegistryKeyCaretaker : PersistentCaretaker<RegistryKeyOriginator, RegistryKeyMemento>
    {
        public PersistentRegistryKeyCaretaker(RegistryHive hive, RegistryView view, string subKey, LiteDatabase database)
            : this(new RegistryKeyOriginator(hive, view, subKey), database)
        {
        }

        public PersistentRegistryKeyCaretaker(RegistryKeyOriginator originator, LiteDatabase database)
            : base(GetID(originator), originator, database)
        {
        }

        public PersistentRegistryKeyCaretaker(string id, int processID, DateTime processStartTime, RegistryKeyOriginator originator, RegistryKeyMemento memento, LiteDatabase database)
            : base(id, processID, processStartTime, originator, memento, database)
        {
        }

        public static void RegisterType(LiteDatabase database)
        {
            var currentProcess = Process.GetCurrentProcess();

            database.Mapper.RegisterType(
                serialize: (caretaker) =>
                {
                    var dictionary = new Dictionary<string, BsonValue>
                    {
                        ["_id"] = caretaker.ID,
                        [nameof(Process) + nameof(Process.Id)] = currentProcess.Id,
                        [nameof(Process) + nameof(Process.StartTime)] = currentProcess.StartTime,
                        [nameof(Originator)] = database.Mapper.ToDocument(caretaker.Originator),
                        [nameof(Memento)] = database.Mapper.ToDocument(caretaker.Memento),
                    };
                    return new BsonDocument(dictionary);
                },
                deserialize: (bson) =>
                {
                    var originator = new RegistryKeyOriginator(
                        Enum.Parse<RegistryHive>(bson[nameof(Originator)][nameof(RegistryKeyOriginator.Hive)].AsString),
                        Enum.Parse<RegistryView>(bson[nameof(Originator)][nameof(RegistryKeyOriginator.View)].AsString),
                        bson[nameof(Originator)][nameof(RegistryKeyOriginator.SubKey)].AsString);
                    var memento = new RegistryKeyMemento(
                        bson[nameof(Memento)][nameof(RegistryKeyMemento.Exists)].AsBoolean);
                    return new PersistentRegistryKeyCaretaker(bson["_id"].AsString, bson[nameof(ProcessID)].AsInt32, bson[nameof(ProcessStartTime)].AsDateTime, originator, memento, database);
                }
            );
        }

        private static string GetID(RegistryKeyOriginator originator)
        {
            if (originator == null)
            {
                throw new ArgumentNullException(nameof(originator));
            }

            return string.Join('\\', originator.Hive, originator.View, originator.SubKey).ToLower();
        }
    }
}
