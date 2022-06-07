using LiteDB;
using MachineStateManager.FileSystem;

namespace MachineStateManager.Persistence.FileSystem
{
    internal class PersistedFileCaretaker : PersistedCaretaker<FileOriginator, FileMemento>
    {
        public override string ID => Originator.Path.ToLower();

        public PersistedFileCaretaker(string path, IBlobStore fileCache, LiteDatabase database)
            : this(new FileOriginator(path, fileCache), database)
        {
        }

        public PersistedFileCaretaker(FileOriginator originator, LiteDatabase database) : base(originator, database)
        {
        }

        public PersistedFileCaretaker(FileOriginator originator, FileMemento memento, LiteDatabase database) : base(originator, memento, database)
        {
        }
        public static void RegisterType(LiteDatabase database, IBlobStore fileCache)
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
                    var originator = new FileOriginator(
                        bson[nameof(Originator)][nameof(FileOriginator.Path)].AsString, fileCache);
                    var memento = new FileMemento(
                        bson[nameof(Memento)][nameof(FileMemento.Hash)].AsString);
                    return new PersistedFileCaretaker(originator, memento, database);
                }
            );
        }
    }
}
