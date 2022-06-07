using LiteDB;
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

        private readonly LiteDBBlobStore fileCache;

        private static readonly string databaseFilePath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData),
            nameof(MachineStateManager),
            $"{nameof(Persistence)}.db");

        public PersistedMachineStateManager()
        {
            caretakers = new List<IDisposable>();

            database = new LiteDatabase(
                connectionString: new ConnectionString(databaseFilePath)
                {
                    Connection = ConnectionType.Shared
                },
                mapper: new BsonMapper());
            fileCache = new LiteDBBlobStore(database);
            PersistedEnvironmentVariableCaretaker.RegisterType(database);
            PersistedFileCaretaker.RegisterType(database, fileCache);
        }

        public IDisposable SnapshotEnvironmentVariable(string name)
        {
            var caretaker = new PersistedEnvironmentVariableCaretaker(name, database);
            caretakers.Add(caretaker);
            return caretaker;
        }
        public IDisposable SnapshotEnvironmentVariable(string name, EnvironmentVariableTarget target)
        {
            var caretaker = new PersistedEnvironmentVariableCaretaker(name, target, database);
            caretakers.Add(caretaker);
            return caretaker;
        }

        public IDisposable SnapshotFile(string path)
        {
            var caretaker = new PersistedFileCaretaker(path, fileCache, database);
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

        //private static LiteDatabase GetDatabaseConnection()
        //{

        //}

        //public static void RestoreAbandonedCaretakers()
        //{
        //    var caretakers = data
        //}
    }
}