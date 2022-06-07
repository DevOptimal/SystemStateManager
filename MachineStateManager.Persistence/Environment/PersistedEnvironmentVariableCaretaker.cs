using LiteDB;
using MachineStateManager.Environment;

namespace MachineStateManager.Persistence.Environment
{
    internal class PersistedEnvironmentVariableCaretaker : PersistedCaretaker<EnvironmentVariableOriginator, EnvironmentVariableMemento>
    {
        public override string ID => $"{Originator.Target}={Originator.Name}";

        public PersistedEnvironmentVariableCaretaker(string name, LiteDatabase database)
            : this(new EnvironmentVariableOriginator(name), database)
        {
        }

        public PersistedEnvironmentVariableCaretaker(string name, EnvironmentVariableTarget target, LiteDatabase database)
            : this(new EnvironmentVariableOriginator(name, target), database)
        {
        }

        public PersistedEnvironmentVariableCaretaker(EnvironmentVariableOriginator originator, LiteDatabase database) : base(originator, database)
        {
        }

        public PersistedEnvironmentVariableCaretaker(EnvironmentVariableOriginator originator, EnvironmentVariableMemento memento, LiteDatabase database) : base(originator, memento, database)
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
                    var originator = new EnvironmentVariableOriginator(
                        bson[nameof(Originator)][nameof(EnvironmentVariableOriginator.Name)].AsString,
                        Enum.Parse<EnvironmentVariableTarget>(bson[nameof(Originator)][nameof(EnvironmentVariableOriginator.Target)].AsString));
                    var memento = new EnvironmentVariableMemento(
                        bson[nameof(Memento)][nameof(EnvironmentVariableMemento.Value)].AsString);
                    return new PersistedEnvironmentVariableCaretaker(originator, memento, database);
                }
            );
        }
    }
}
