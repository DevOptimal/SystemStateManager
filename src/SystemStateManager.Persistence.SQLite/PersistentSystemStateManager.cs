using Dapper;
using DevOptimal.SystemStateManager.Environment;
using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemStateManager.Persistence.SQLite.Environment;
using DevOptimal.SystemStateManager.Persistence.SQLite.FileSystem;
using DevOptimal.SystemStateManager.Persistence.SQLite.FileSystem.Caching;
using DevOptimal.SystemStateManager.Persistence.SQLite.Registry;
using DevOptimal.SystemStateManager.Registry;
using DevOptimal.SystemUtilities.Environment;
using DevOptimal.SystemUtilities.FileSystem;
using DevOptimal.SystemUtilities.Registry;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace DevOptimal.SystemStateManager.Persistence.SQLite
{
    public class PersistentSystemStateManager : SystemStateManager
    {
        public static Uri PersistenceURI { get; set; } = new Uri(
            Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData),
                nameof(SystemStateManager),
                $"{nameof(Persistence)}.db"));

        internal static readonly SqliteConnection Connection;

        static PersistentSystemStateManager()
        {
            if (!PersistenceURI.IsFile)
            {
                throw new NotSupportedException($"{nameof(PersistenceURI)} is invalid. Only local file paths are supported.");
            }

            var databaseFile = new FileInfo(PersistenceURI.LocalPath);

            var databaseDirectory = databaseFile.Directory;

            if (!databaseDirectory.Exists)
            {
                databaseDirectory.Create();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var directorySecurity = databaseDirectory.GetAccessControl();
                    directorySecurity.AddAccessRule(new FileSystemAccessRule(
                        identity: new SecurityIdentifier(WellKnownSidType.WorldSid, domainSid: null),
                        fileSystemRights: FileSystemRights.FullControl,
                        inheritanceFlags: InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        propagationFlags: PropagationFlags.NoPropagateInherit,
                        type: AccessControlType.Allow));
                    databaseDirectory.SetAccessControl(directorySecurity);
                }
            }

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databaseFile.FullName
            }.ToString();
            Connection = new SqliteConnection(connectionString);
            Connection.Open();
        }

        public PersistentSystemStateManager()
            : this(new DefaultEnvironment(), new DefaultFileSystem(), new DefaultRegistry())
        {
        }

        public PersistentSystemStateManager(IEnvironment environment, IFileSystem fileSystem, IRegistry registry)
            : base(new SQLiteFileCache(fileSystem), environment, fileSystem, registry)
        {
        }

        private PersistentSystemStateManager(List<ISnapshot> snapshots)
            : base(snapshots, new SQLiteFileCache(new DefaultFileSystem()))
        {
        }

        protected override ISnapshot CreateEnvironmentVariableSnapshot(string id, string name, EnvironmentVariableTarget target, IEnvironment environment)
        {
            var originator = new EnvironmentVariableOriginator(name, target, environment);
            return new PersistentEnvironmentVariableCaretaker(id, originator);
        }

        protected override ISnapshot CreateDirectorySnapshot(string id, string path, IFileSystem fileSystem)
        {
            var originator = new DirectoryOriginator(path, fileSystem);
            return new PersistentDirectoryCaretaker(id, originator);
        }

        protected override ISnapshot CreateFileSnapshot(string id, string path, IFileCache fileCache, IFileSystem fileSystem)
        {
            var originator = new FileOriginator(path, fileCache, fileSystem);
            return new PersistentFileCaretaker(id, originator);
        }

        protected override ISnapshot CreateRegistryKeySnapshot(string id, RegistryHive hive, RegistryView view, string subKey, IRegistry registry)
        {
            var originator = new RegistryKeyOriginator(hive, view, subKey, registry);
            return new PersistentRegistryKeyCaretaker(id, originator);
        }

        protected override ISnapshot CreateRegistryValueSnapshot(string id, RegistryHive hive, RegistryView view, string subKey, string name, IRegistry registry)
        {
            var originator = new RegistryValueOriginator(hive, view, subKey, name, registry);
            return new PersistentRegistryValueCaretaker(id, originator);
        }

        /// <summary>
        /// Restores all abandoned snapshots on the current machine. An "abandoned snapshot" is a snapshot that was created by a process that no longer exists.
        /// </summary>
        public static void RestoreAbandonedSnapshots()
        {
            // Create a dictionary that maps process IDs to process start times, which will be used to uniquely identify a currently running process.
            // A null value indicates that the current process does not have permission to the corresponding process - try rerunning in an elevated process.
            var processes = new Dictionary<int, DateTime?>();
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    processes[process.Id] = process.StartTime;
                }
                catch (Win32Exception)
                {
                    processes[process.Id] = null;
                }
                catch (InvalidOperationException) { } // The process has already exited, so don't add it.
            }

            var abandonedSnapshots = new List<ISnapshot>();

            Connection.Query<PersistentEnvironmentVariableCaretaker>($@"SELECT * FROM {nameof(PersistentEnvironmentVariableCaretaker)}").ToList();

            using (var database = LiteDatabaseFactory.GetDatabase())
            {
                abandonedSnapshots.AddRange(database.GetCollection<IPersistentSnapshot>().FindAll()
                    .Where(c => !(processes.ContainsKey(c.ProcessID) &&
                        (
                            processes[c.ProcessID] == c.ProcessStartTime ||
                            processes[c.ProcessID] == null
                        )))
                    .Cast<ISnapshot>());
            }

            if (abandonedSnapshots.Any())
            {
                var systemStateManager = new PersistentSystemStateManager(abandonedSnapshots);
                systemStateManager.Dispose();
            }
        }
    }
}
