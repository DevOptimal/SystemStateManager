using DevOptimal.SystemStateManager.FileSystem;
using LiteDB;
using System;
using System.Linq;

namespace DevOptimal.SystemStateManager.Persistence.FileSystem
{
    internal class PersistentFileCaretaker : PersistentCaretaker<PersistentFileOriginator, FileMemento>
    {
        private bool disposedValue;

        public PersistentFileCaretaker(string id, PersistentFileOriginator originator)
            : base(id, originator)
        {
        }

        [BsonCtor]
        public PersistentFileCaretaker(string _id, int processID, DateTime processStartTime, BsonDocument originator, BsonDocument memento)
            : base(_id, processID, processStartTime, BsonMapper.Global.ToObject<PersistentFileOriginator>(originator), BsonMapper.Global.ToObject<FileMemento>(memento))
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposedValue)
            {
                if (disposing)
                {
                    if (Memento.Hash != null)
                    {
                        using (var database = LiteDatabaseFactory.GetDatabase())
                        {
                            var collection = database.GetCollection<IPersistentSnapshot>();

                            if (!collection.FindAll().OfType<PersistentFileCaretaker>().Any(c => c.Memento.Hash == Memento.Hash))
                            {
                                var fileStorage = database.FileStorage;
                                fileStorage.Delete(Memento.Hash);
                            }
                        }
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }
    }
}
