using FileSystem;
using LiteDB;
using MachineStateManager.Core.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace MachineStateManager.Persistence.FileSystem
{
    internal class PersistentFileCaretaker : PersistentCaretaker<FileOriginator, FileMemento>
    {
        private bool disposedValue;

        static PersistentFileCaretaker()
        {
            BsonMapper.Global.RegisterType(SerializeOriginator, DeserializeOriginator);
        }

        public PersistentFileCaretaker(string path, IBlobStore fileCache, IFileSystem fileSystem)
            : this(new FileOriginator(path, fileCache, fileSystem))
        {
        }

        public PersistentFileCaretaker(FileOriginator originator)
            : base(GetID(originator), originator)
        {
        }

        [BsonCtor]
        public PersistentFileCaretaker(string _id, int processID, DateTime processStartTime, BsonDocument originator, BsonDocument memento)
            : base(_id, processID, processStartTime, BsonMapper.Global.ToObject<FileOriginator>(originator), BsonMapper.Global.ToObject<FileMemento>(memento))
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

        private static BsonValue SerializeOriginator(FileOriginator originator)
        {
            return new BsonDocument
            {
                [nameof(FileOriginator.Path)] = originator.Path,
                [nameof(FileOriginator.FileCache)] = BsonMapper.Global.ToDocument(originator.FileCache),
                [nameof(FileOriginator.FileSystem)] = BsonMapper.Global.ToDocument(originator.FileSystem),
            };
        }

        private static FileOriginator DeserializeOriginator(BsonValue bson)
        {
            return new FileOriginator(
                path: bson[nameof(FileOriginator.Path)],
                fileCache: BsonMapper.Global.ToObject<IBlobStore>(bson[nameof(FileOriginator.FileCache)].AsDocument),
                fileSystem: BsonMapper.Global.ToObject<IFileSystem>(bson[nameof(FileOriginator.FileSystem)].AsDocument));
        }
    }
}
