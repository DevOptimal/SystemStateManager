using LiteDB;
using MachineStateManager.Core.Registry;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace MachineStateManager.Persistence.Registry
{
    [SupportedOSPlatform("windows")]
    internal class PersistentRegistryValueCaretaker : PersistentCaretaker<RegistryValueOriginator, RegistryValueMemento>
    {
        public override string ID => string.Join('\\', Originator.Hive, Originator.View, Originator.SubKey.ToLower());

        public PersistentRegistryValueCaretaker(RegistryHive hive, RegistryView view, string subKey, string name, LiteDatabase database)
            : this(new RegistryValueOriginator(hive, view, subKey, name), database)
        {
        }

        public PersistentRegistryValueCaretaker(RegistryValueOriginator originator, LiteDatabase database)
            : base(originator, database)
        {
        }

        public PersistentRegistryValueCaretaker(RegistryValueOriginator originator, RegistryValueMemento memento, LiteDatabase database)
            : base(originator, memento, database)
        {
        }

        public static void RegisterType(LiteDatabase database)
        {
            database.Mapper.RegisterType(
                serialize: (caretaker) =>
                {
                    var dictionary = new Dictionary<string, BsonValue>
                    {
                        ["_id"] = caretaker.ID,
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
                    return new PersistentRegistryValueCaretaker(originator, memento, database);
                }
            );
        }
    }
}
