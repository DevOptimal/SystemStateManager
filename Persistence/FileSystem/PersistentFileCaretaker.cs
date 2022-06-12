using LiteDB;
using MachineStateManager.Core.FileSystem;
using MachineStateManager.Persistence.FileSystem.Caching;
using System.Diagnostics;

namespace MachineStateManager.Persistence.FileSystem
{
    internal class PersistentFileCaretaker : PersistentCaretaker<FileOriginator, FileMemento>
    {
        static PersistentFileCaretaker()
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
                    var originator = new FileOriginator(
                        bson[nameof(Originator)][nameof(FileOriginator.Path)].AsString, new LiteDBBlobStore());
                    var memento = new FileMemento(
                        bson[nameof(Memento)][nameof(FileMemento.Hash)].AsString);
                    return new PersistentFileCaretaker(bson["_id"].AsString, bson[nameof(ProcessID)].AsInt32, bson[nameof(ProcessStartTime)].AsDateTime, originator, memento);
                }
            );
        }

        public PersistentFileCaretaker(string path, IBlobStore fileCache)
            : this(new FileOriginator(path, fileCache))
        {
        }

        public PersistentFileCaretaker(FileOriginator originator)
            : base(GetID(originator), originator)
        {
        }

        public PersistentFileCaretaker(string id, int processID, DateTime processStartTime, FileOriginator originator, FileMemento memento)
            : base(id, processID, processStartTime, originator, memento)
        {
        }

        public static IEnumerable<IDisposable> GetAbandonedCaretakers() => GetAbandonedCaretakers<PersistentFileCaretaker>();

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
