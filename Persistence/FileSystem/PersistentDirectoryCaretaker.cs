using LiteDB;
using MachineStateManager.Core.FileSystem;

namespace MachineStateManager.Persistence.FileSystem
{
    internal class PersistentDirectoryCaretaker : PersistentCaretaker<DirectoryOriginator, DirectoryMemento>
    {
        public override string ID => Originator.Path.ToLower();

        public PersistentDirectoryCaretaker(string path, LiteDatabase database)
            : this(new DirectoryOriginator(path), database)
        {
        }

        public PersistentDirectoryCaretaker(DirectoryOriginator originator, LiteDatabase database)
            : base(originator, database)
        {
        }

        public PersistentDirectoryCaretaker(DirectoryOriginator originator, DirectoryMemento memento, LiteDatabase database)
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
                    var originator = new DirectoryOriginator(
                        bson[nameof(Originator)][nameof(DirectoryOriginator.Path)].AsString);
                    var memento = new DirectoryMemento(
                        bson[nameof(Memento)][nameof(DirectoryMemento.Exists)].AsBoolean);
                    return new PersistentDirectoryCaretaker(originator, memento, database);
                }
            );
        }
    }
}
