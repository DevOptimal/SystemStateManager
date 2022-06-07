using LiteDB;
using MachineStateManager.Environment;
using MachineStateManager.FileSystem;
using MachineStateManager.Persistence.Environment;
using MachineStateManager.Persistence.FileSystem;
using MachineStateManager.Persistence.FileSystem.Caching;

namespace MachineStateManager.Persistence
{
    public class PersistedMachineStateManager : IDisposable
    {
        private bool disposedValue;

        private readonly List<IDisposable> caretakers;

        private readonly LiteDatabase database;

        private readonly IBlobStore blobStore;

        private readonly string ID;

        public PersistedMachineStateManager()
        {
            ID = Guid.NewGuid().ToString();

            caretakers = new List<IDisposable>();

            var mapper = new BsonMapper();
            database = new LiteDatabase(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData), nameof(MachineStateManager), "persistence.db"));
            mapper.RegisterType(
                serialize: (caretaker) =>
                {
                    var dictionary = new Dictionary<string, BsonValue>
                    {
                        ["_id"] = caretaker.ID,
                        [nameof(PersistedCaretaker<PersistedEnvironmentVariableOriginator, EnvironmentVariableMemento>.Originator)] = mapper.ToDocument(caretaker.Originator),
                        [nameof(PersistedCaretaker<PersistedEnvironmentVariableOriginator, EnvironmentVariableMemento>.Memento)] = mapper.ToDocument(caretaker.Memento),
                    };
                    return new BsonDocument(dictionary);
                },
                deserialize: (bson) =>
                {
                    var originator = new PersistedEnvironmentVariableOriginator(
                        bson[nameof(PersistedCaretaker<PersistedEnvironmentVariableOriginator, EnvironmentVariableMemento>.Originator)][nameof(EnvironmentVariableOriginator.Name)].AsString,
                        Enum.Parse<EnvironmentVariableTarget>(bson[nameof(PersistedCaretaker<PersistedEnvironmentVariableOriginator, EnvironmentVariableMemento>.Originator)][nameof(EnvironmentVariableOriginator.Target)].AsString));
                    var memento = new EnvironmentVariableMemento(
                        bson[nameof(PersistedCaretaker<PersistedEnvironmentVariableOriginator, EnvironmentVariableMemento>.Memento)][nameof(EnvironmentVariableMemento.Value)].AsString);
                    return new PersistedCaretaker<PersistedEnvironmentVariableOriginator, EnvironmentVariableMemento>(originator, memento, database);
                }
            );

            blobStore = new LiteDBBlobStore(database);

            mapper.RegisterType(
                serialize: (caretaker) =>
                {
                    var dictionary = new Dictionary<string, BsonValue>
                    {
                        ["_id"] = caretaker.ID,
                        [nameof(PersistedCaretaker<PersistedFileOriginator, FileMemento>.Originator)] = mapper.ToDocument(caretaker.Originator),
                        [nameof(PersistedCaretaker<PersistedFileOriginator, FileMemento>.Memento)] = mapper.ToDocument(caretaker.Memento),
                    };
                    return new BsonDocument(dictionary);
                },
                deserialize: (bson) =>
                {
                    var originator = new PersistedFileOriginator(
                        bson[nameof(PersistedCaretaker<PersistedFileOriginator, FileMemento>.Originator)][nameof(PersistedFileOriginator.Path)].AsString, blobStore);
                    var memento = new FileMemento(
                        bson[nameof(PersistedCaretaker<PersistedFileOriginator, FileMemento>.Memento)][nameof(FileMemento.Hash)].AsString);
                    return new PersistedCaretaker<PersistedFileOriginator, FileMemento>(originator, memento, database);
                }
            );
        }

        public IDisposable SnapshotEnvironmentVariable(string name)
        {
            var originator = new PersistedEnvironmentVariableOriginator(name);
            var caretaker = new PersistedCaretaker<PersistedEnvironmentVariableOriginator, EnvironmentVariableMemento>(originator, database);
            caretakers.Add(caretaker);
            return caretaker;
        }
        public IDisposable SnapshotEnvironmentVariable(string name, EnvironmentVariableTarget target)
        {
            var originator = new PersistedEnvironmentVariableOriginator(name, target);
            var caretaker = new PersistedCaretaker<PersistedEnvironmentVariableOriginator, EnvironmentVariableMemento>(originator, database);
            caretakers.Add(caretaker);
            return caretaker;
        }

        public IDisposable SnapshotFile(string path)
        {
            var originator = new PersistedFileOriginator(path, blobStore);
            var caretaker = new PersistedCaretaker<PersistedFileOriginator, FileMemento>(originator, database);
            caretakers.Add(caretaker);
            return caretaker;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                foreach (var caretaker in caretakers)
                {
                    caretaker.Dispose();
                }

                database.Dispose();

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MachineStateManager()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}