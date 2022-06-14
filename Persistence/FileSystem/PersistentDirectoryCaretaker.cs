using LiteDB;
using MachineStateManager.Core.FileSystem;
using System.Diagnostics;

namespace MachineStateManager.Persistence.FileSystem
{
    internal class PersistentDirectoryCaretaker : PersistentCaretaker<DirectoryOriginator, DirectoryMemento>
    {
        static PersistentDirectoryCaretaker()
        {
            var currentProcess = Process.GetCurrentProcess();

            BsonMapper.Global.RegisterType(
                serialize: (caretaker) =>
                {
                    var dictionary = new Dictionary<string, BsonValue>
                    {
                        ["_id"] = caretaker.ID,
                        [nameof(ProcessID)] = caretaker.ProcessID,
                        [nameof(ProcessStartTime)] = caretaker.ProcessStartTime,
                        [nameof(Originator)] = BsonMapper.Global.ToDocument(caretaker.Originator),
                        [nameof(Memento)] = BsonMapper.Global.ToDocument(caretaker.Memento),
                    };
                    return new BsonDocument(dictionary);
                },
                deserialize: (bson) =>
                {
                    var originator = new DirectoryOriginator(
                        bson[nameof(Originator)][nameof(DirectoryOriginator.Path)].AsString);
                    var memento = new DirectoryMemento(
                        bson[nameof(Memento)][nameof(DirectoryMemento.Exists)].AsBoolean);
                    return new PersistentDirectoryCaretaker(bson["_id"].AsString, bson[nameof(ProcessID)].AsInt32, bson[nameof(ProcessStartTime)].AsDateTime, originator, memento);
                }
            );
        }

        public PersistentDirectoryCaretaker(string path)
            : this(new DirectoryOriginator(path))
        {
        }

        public PersistentDirectoryCaretaker(DirectoryOriginator originator)
            : base(GetID(originator), originator)
        {
        }

        public PersistentDirectoryCaretaker(string id, int processID, DateTime processStartTime, DirectoryOriginator originator, DirectoryMemento memento)
            : base(id, processID, processStartTime, originator, memento)
        {
        }

        public static IEnumerable<IDisposable> GetAbandonedCaretakers(Dictionary<int, DateTime?> processes)
            => GetAbandonedCaretakers<PersistentDirectoryCaretaker>(processes);

        private static string GetID(DirectoryOriginator originator)
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
