using LiteDB;
using MachineStateManager.Core.FileSystem;
using MachineStateManager.Persistence.FileSystem.Caching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace MachineStateManager.Persistence.FileSystem
{
    internal class PersistentFileCaretaker : PersistentCaretaker<FileOriginator, FileMemento>
    {
        private bool disposedValue;

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

        public static IEnumerable<IDisposable> GetAbandonedCaretakers(Dictionary<int, DateTime?> processes)
            => GetAbandonedCaretakers<PersistentFileCaretaker>(processes);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposedValue)
            {
                if (disposing)
                {
                    using (var database = GetDatabase())
                    {
                        var collection = database.GetCollection<PersistentFileCaretaker>();

                        if (!(collection.Find(c => c.Memento.Hash == Memento.Hash).Any()))
                        {
                            var fileStorage = database.FileStorage;
                            fileStorage.Delete(Memento.Hash);
                        }
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        private static string GetID(FileOriginator originator)
        {
            if (originator == null)
            {
                throw new ArgumentNullException(nameof(originator));
            }

            var id = originator.Path;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                id = id.ToLower();
            }

            return id;
        }
    }
}
