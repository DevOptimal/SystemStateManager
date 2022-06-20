using bradselw.SystemResources.FileSystem.Proxy;
using LiteDB;
using bradselw.MachineStateManager.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace bradselw.MachineStateManager.Persistence.FileSystem
{
    internal class PersistentFileCaretaker : PersistentCaretaker<PersistentFileOriginator, FileMemento>
    {
        private bool disposedValue;

        public PersistentFileCaretaker(string path, IBlobStore fileCache, IFileSystemProxy fileSystem)
            : this(new PersistentFileOriginator(path, fileCache, fileSystem))
        {
        }

        public PersistentFileCaretaker(PersistentFileOriginator originator)
            : base(GetID(originator), originator)
        {
        }

        [BsonCtor]
        public PersistentFileCaretaker(string _id, int processID, DateTime processStartTime, BsonDocument originator, BsonDocument memento)
            : base(_id, processID, processStartTime, BsonMapper.Global.ToObject<PersistentFileOriginator>(originator), BsonMapper.Global.ToObject<FileMemento>(memento))
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
