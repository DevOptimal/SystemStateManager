using LiteDB;
using MachineStateManager.Core.Registry;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace MachineStateManager.Persistence.Registry
{
    [SupportedOSPlatform("windows")]
    internal class PersistentRegistryValueCaretaker : PersistentCaretaker<RegistryValueOriginator, RegistryValueMemento>
    {
        public PersistentRegistryValueCaretaker(RegistryHive hive, RegistryView view, string subKey, string name, LiteDatabase database)
            : this(new RegistryValueOriginator(hive, view, subKey, name), database)
        {
        }

        public PersistentRegistryValueCaretaker(RegistryValueOriginator originator, LiteDatabase database)
            : base(GetID(originator), originator, database)
        {
        }

        public PersistentRegistryValueCaretaker(string id, int processID, DateTime processStartTime, RegistryValueOriginator originator, RegistryValueMemento memento, LiteDatabase database)
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
                    var originator = new RegistryValueOriginator(
                        Enum.Parse<RegistryHive>(bson[nameof(Originator)][nameof(RegistryValueOriginator.Hive)].AsString),
                        Enum.Parse<RegistryView>(bson[nameof(Originator)][nameof(RegistryValueOriginator.View)].AsString),
                        bson[nameof(Originator)][nameof(RegistryValueOriginator.SubKey)].AsString,
                        bson[nameof(Originator)][nameof(RegistryValueOriginator.Name)].AsString);
                    var memento = new RegistryValueMemento(
                        bson[nameof(Memento)][nameof(RegistryValueMemento.Value)],
                        Enum.Parse<RegistryValueKind>(bson[nameof(Memento)][nameof(RegistryValueMemento.Kind)].AsString));
                    return new PersistentRegistryValueCaretaker(bson["_id"].AsString, bson[nameof(ProcessID)].AsInt32, bson[nameof(ProcessStartTime)].AsDateTime, originator, memento, database);
                }
            );
        }

        private static string GetID(RegistryValueOriginator originator)
        {
            if (originator == null)
            {
                throw new ArgumentNullException(nameof(originator));
            }

            return string.Join('\\', originator.Hive, originator.View, originator.SubKey, originator.Name ?? "(Default)").ToLower();
        }
    }
}
