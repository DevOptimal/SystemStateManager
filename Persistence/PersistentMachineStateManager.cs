using LiteDB;
using MachineStateManager.Persistence.Environment;
using MachineStateManager.Persistence.FileSystem;
using MachineStateManager.Persistence.FileSystem.Caching;
using MachineStateManager.Persistence.Registry;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace MachineStateManager.Persistence
{
    public class PersistentMachineStateManager : MachineStateManager
    {
        private static readonly LiteDatabase database;

        private static readonly LiteDBBlobStore fileCache;

        private static readonly string databaseFilePath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData),
            nameof(MachineStateManager),
            $"{nameof(Persistence)}.db");

        static PersistentMachineStateManager()
        {
            var databaseFilePath = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData),
                nameof(MachineStateManager),
                $"{nameof(Persistence)}.db");

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

        public PersistentMachineStateManager()
            : base(fileCache)
        {
        }

        private PersistentMachineStateManager(List<IDisposable> caretakers)
            : base(fileCache, caretakers)
        {
        }

        public override IDisposable SnapshotEnvironmentVariable(string name)
        {
            var caretaker = new PersistentEnvironmentVariableCaretaker(name, database);
            caretakers.Add(caretaker);
            return caretaker;
        }

        public override IDisposable SnapshotEnvironmentVariable(string name, EnvironmentVariableTarget target)
        {
            var caretaker = new PersistentEnvironmentVariableCaretaker(name, target, database);
            caretakers.Add(caretaker);
            return caretaker;
        }

        public override IDisposable SnapshotDirectory(string path)
        {
            var caretaker = new PersistentDirectoryCaretaker(path, database);
            caretakers.Add(caretaker);
            return caretaker;
        }

        public override IDisposable SnapshotFile(string path)
        {
            var caretaker = new PersistentFileCaretaker(path, fileCache, database);
            caretakers.Add(caretaker);
            return caretaker;
        }

        [SupportedOSPlatform("windows")]
        public override IDisposable SnapshotRegistryKey(RegistryHive hive, RegistryView view, string subKey)
        {
            var caretaker = new PersistentRegistryKeyCaretaker(hive, view, subKey, database);
            caretakers.Add(caretaker);
            return caretaker;
        }

        [SupportedOSPlatform("windows")]
        public override IDisposable SnapshotRegistryValue(RegistryHive hive, RegistryView view, string subKey, string name)
        {
            var caretaker = new PersistentRegistryValueCaretaker(hive, view, subKey, name, database);
            caretakers.Add(caretaker);
            return caretaker;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            database.Dispose();
        }

        public static void RestoreAbandonedCaretakers()
        {
            var accessibleProcessInfos = new Dictionary<int, DateTime>();
            var inaccessibleProcessIDs = new HashSet<int>();
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    accessibleProcessInfos[process.Id] = process.StartTime;
                }
                catch (Win32Exception)
                {
                    inaccessibleProcessIDs.Add(process.Id);
                }
            }

            var abandonedCaretakers = new List<IDisposable>();

            abandonedCaretakers.AddRange(GetAbandonedCaretakers<PersistentEnvironmentVariableCaretaker>(database, accessibleProcessInfos, inaccessibleProcessIDs));
            abandonedCaretakers.AddRange(GetAbandonedCaretakers<PersistentDirectoryCaretaker>(database, accessibleProcessInfos, inaccessibleProcessIDs));
            abandonedCaretakers.AddRange(GetAbandonedCaretakers<PersistentFileCaretaker>(database, accessibleProcessInfos, inaccessibleProcessIDs));
            if (OperatingSystem.IsWindows())
            {
                abandonedCaretakers.AddRange(GetAbandonedCaretakers<PersistentRegistryKeyCaretaker>(database, accessibleProcessInfos, inaccessibleProcessIDs));
                abandonedCaretakers.AddRange(GetAbandonedCaretakers<PersistentRegistryValueCaretaker>(database, accessibleProcessInfos, inaccessibleProcessIDs));
            }

            var machineStateManager = new PersistentMachineStateManager(abandonedCaretakers);

            machineStateManager.Dispose();
        }

        private static IEnumerable<IDisposable> GetAbandonedCaretakers<TCaretaker>(LiteDatabase database, Dictionary<int, DateTime> accessibleProcessInfos, HashSet<int> inaccessibleProcessIDs)
            where TCaretaker : IPersistentCaretaker
        {
            return database.GetCollection<TCaretaker>().FindAll()
                .Where(c => !(inaccessibleProcessIDs.Contains(c.ProcessID) ||
                    (accessibleProcessInfos.ContainsKey(c.ProcessID) &&
                    accessibleProcessInfos[c.ProcessID] == c.ProcessStartTime)))
                .Cast<IDisposable>();
        }
    }
}