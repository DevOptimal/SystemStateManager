using LiteDB;
using MachineStateManager.Core.Registry;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace MachineStateManager.Persistence.Registry
{
    [SupportedOSPlatform("windows")]
    internal class PersistentRegistryKeyCaretaker : PersistentCaretaker<RegistryKeyOriginator, RegistryKeyMemento>
    {
        public override string ID => string.Join('\\', Originator.Hive, Originator.View, Originator.SubKey.ToLower());

        public PersistentRegistryKeyCaretaker(RegistryHive hive, RegistryView view, string subKey, LiteDatabase database)
            : this(new RegistryKeyOriginator(hive, view, subKey), database)
        {
        }

        public PersistentRegistryKeyCaretaker(RegistryKeyOriginator originator, LiteDatabase database)
            : base(originator, database)
        {
        }

        public PersistentRegistryKeyCaretaker(RegistryKeyOriginator originator, RegistryKeyMemento memento, LiteDatabase database)
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
                    var originator = new RegistryKeyOriginator(
                        Enum.Parse<RegistryHive>(bson[nameof(Originator)][nameof(RegistryKeyOriginator.Hive)].AsString),
                        Enum.Parse<RegistryView>(bson[nameof(Originator)][nameof(RegistryKeyOriginator.View)].AsString),
                        bson[nameof(Originator)][nameof(RegistryKeyOriginator.SubKey)].AsString);
                    var memento = new RegistryKeyMemento(
                        bson[nameof(Memento)][nameof(RegistryKeyMemento.Exists)].AsBoolean);
                    return new PersistentRegistryKeyCaretaker(originator, memento, database);
                }
            );
        }
    }
}
