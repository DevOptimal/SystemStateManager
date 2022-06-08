using LiteDB;
using MachineStateManager.Persistence.Environment;
using MachineStateManager.Persistence.FileSystem;
using MachineStateManager.Persistence.FileSystem.Caching;
using MachineStateManager.Persistence.Registry;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace MachineStateManager.Persistence
{
    public class PersistentMachineStateManager : IDisposable
    {
        private bool disposedValue;

        private readonly List<IDisposable> caretakers;

        private readonly LiteDatabase database;

        private readonly LiteDBBlobStore fileCache;

        private static readonly string databaseFilePath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData),
            nameof(MachineStateManager),
            $"{nameof(Persistence)}.db");

        public PersistentMachineStateManager()
        {
            caretakers = new List<IDisposable>();

            database = new LiteDatabase(
                connectionString: new ConnectionString(databaseFilePath)
                {
                    Connection = ConnectionType.Shared
                },
                mapper: new BsonMapper());
            fileCache = new LiteDBBlobStore(database);
            PersistentEnvironmentVariableCaretaker.RegisterType(database);
            PersistentDirectoryCaretaker.RegisterType(database);
            PersistentFileCaretaker.RegisterType(database, fileCache);
            if (OperatingSystem.IsWindows())
            {
                PersistentRegistryKeyCaretaker.RegisterType(database);
                PersistentRegistryValueCaretaker.RegisterType(database);
            }
        }

        public IDisposable SnapshotEnvironmentVariable(string name)
        {
            var caretaker = new PersistentEnvironmentVariableCaretaker(name, database);
            caretakers.Add(caretaker);
            return caretaker;
        }

        public IDisposable SnapshotEnvironmentVariable(string name, EnvironmentVariableTarget target)
        {
            var caretaker = new PersistentEnvironmentVariableCaretaker(name, target, database);
            caretakers.Add(caretaker);
            return caretaker;
        }

        public IDisposable SnapshotDirectory(string path)
        {
            var caretaker = new PersistentDirectoryCaretaker(path, database);
            caretakers.Add(caretaker);
            return caretaker;
        }

        public IDisposable SnapshotFile(string path)
        {
            var caretaker = new PersistentFileCaretaker(path, fileCache, database);
            caretakers.Add(caretaker);
            return caretaker;
        }

        [SupportedOSPlatform("windows")]
        public IDisposable SnapshotRegistryKey(RegistryHive hive, RegistryView view, string subKey)
        {
            var caretaker = new PersistentRegistryKeyCaretaker(hive, view, subKey, database);
            caretakers.Add(caretaker);
            return caretaker;
        }

        [SupportedOSPlatform("windows")]
        public IDisposable SnapshotRegistryValue(RegistryHive hive, RegistryView view, string subKey, string name)
        {
            var caretaker = new PersistentRegistryValueCaretaker(hive, view, subKey, name, database);
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