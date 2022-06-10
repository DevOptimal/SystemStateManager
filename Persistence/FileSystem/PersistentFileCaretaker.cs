using LiteDB;
using MachineStateManager.Core;
using MachineStateManager.Core.FileSystem;
using System.Diagnostics;

namespace MachineStateManager.Persistence.FileSystem
{
    internal class PersistentFileCaretaker : PersistentCaretaker<FileOriginator, FileMemento>
    {
        public PersistentFileCaretaker(string path, IBlobStore fileCache, LiteDatabase database)
            : this(new FileOriginator(path, fileCache), database)
        {
        }

        public PersistentFileCaretaker(FileOriginator originator, LiteDatabase database)
            : base(GetID(originator), originator, database)
        {
        }

        public PersistentFileCaretaker(string id, int processID, DateTime processStartTime, FileOriginator originator, FileMemento memento, LiteDatabase database)
            : base(id, processID, processStartTime, originator, memento, database)
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            var col = database.GetCollection<Caretaker<FileOriginator, FileMemento>>(CollectionName);

        }

        public static void RegisterType(LiteDatabase database, IBlobStore fileCache)
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
                    var originator = new FileOriginator(
                        bson[nameof(Originator)][nameof(FileOriginator.Path)].AsString, fileCache);
                    var memento = new FileMemento(
                        bson[nameof(Memento)][nameof(FileMemento.Hash)].AsString);
                    return new PersistentFileCaretaker(bson["_id"].AsString, bson[nameof(ProcessID)].AsInt32, bson[nameof(ProcessStartTime)].AsDateTime, originator, memento, database);
                }
            );
        }

        private static string GetID(FileOriginator originator)
        {
            if (originator == null)
            {
                throw new ArgumentNullException(nameof(originator));
            }

            var id = originator.Path;

            if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
            {
                id = id.ToLower();
            }

            return id;
        }
    }
}
