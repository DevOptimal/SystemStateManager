using LiteDB;
using MachineStateManager.Core.FileSystem;
using System.Diagnostics;

namespace MachineStateManager.Persistence.FileSystem
{
    internal class PersistentDirectoryCaretaker : PersistentCaretaker<DirectoryOriginator, DirectoryMemento>
    {
        public PersistentDirectoryCaretaker(string path, LiteDatabase database)
            : this(new DirectoryOriginator(path), database)
        {
        }

        public PersistentDirectoryCaretaker(DirectoryOriginator originator, LiteDatabase database)
            : base(GetID(originator), originator, database)
        {
        }

        public PersistentDirectoryCaretaker(string id, int processID, DateTime processStartTime, DirectoryOriginator originator, DirectoryMemento memento, LiteDatabase database)
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
                        [nameof(ProcessID)] = caretaker.ProcessID,
                        [nameof(ProcessStartTime)] = caretaker.ProcessStartTime,
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
                    return new PersistentDirectoryCaretaker(bson["_id"].AsString, bson[nameof(ProcessID)].AsInt32, bson[nameof(ProcessStartTime)].AsDateTime, originator, memento, database);
                }
            );
        }

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
