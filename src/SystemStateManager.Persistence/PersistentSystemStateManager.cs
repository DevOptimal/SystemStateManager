using DevOptimal.SystemStateManager.Environment;
using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemStateManager.Persistence.Environment;
using DevOptimal.SystemStateManager.Persistence.FileSystem;
using DevOptimal.SystemStateManager.Persistence.FileSystem.Caching;
using DevOptimal.SystemStateManager.Persistence.Registry;
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

namespace DevOptimal.SystemStateManager.Persistence
{
    public class PersistentSystemStateManager : SystemStateManager
    {
        public static Uri PersistenceURI { get; set; } = new Uri(
            Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData),
                nameof(SystemStateManager),
                $"{nameof(Persistence)}.db"));

        private readonly SqliteConnection connection;

        public PersistentSystemStateManager()
            : this(new DefaultEnvironment(), new DefaultFileSystem(), new DefaultRegistry())
        {
        }

        public PersistentSystemStateManager(IEnvironment environment, IFileSystem fileSystem, IRegistry registry)
            : this(environment, fileSystem, registry, CreateConnection())
        {
        }

        private PersistentSystemStateManager(IEnvironment environment, IFileSystem fileSystem, IRegistry registry, SqliteConnection connection)
            : base(new SQLiteFileCache(fileSystem, connection), environment, fileSystem, registry)
        {
            this.connection = connection;
        }

        private PersistentSystemStateManager(List<ISnapshot> snapshots, IFileCache fileCache, IEnvironment environment, IFileSystem fileSystem, IRegistry registry, SqliteConnection connection)
            : base(snapshots, fileCache, environment, fileSystem, registry)
        {
            this.connection = connection;
        }

        protected override ISnapshot CreateEnvironmentVariableSnapshot(string id, string name, EnvironmentVariableTarget target, IEnvironment environment)
        {
            var originator = new EnvironmentVariableOriginator(name, target, environment);
            return new PersistentEnvironmentVariableCaretaker(id, originator, connection);
        }

        protected override ISnapshot CreateDirectorySnapshot(string id, string path, IFileSystem fileSystem)
        {
            var originator = new DirectoryOriginator(path, fileSystem);
            return new PersistentDirectoryCaretaker(id, originator, connection);
        }

        protected override ISnapshot CreateFileSnapshot(string id, string path, IFileCache fileCache, IFileSystem fileSystem)
        {
            var originator = new FileOriginator(path, fileCache, fileSystem);
            return new PersistentFileCaretaker(id, originator, connection);
        }

        protected override ISnapshot CreateRegistryKeySnapshot(string id, RegistryHive hive, RegistryView view, string subKey, IRegistry registry)
        {
            var originator = new RegistryKeyOriginator(hive, view, subKey, registry);
            return new PersistentRegistryKeyCaretaker(id, originator, connection);
        }

        protected override ISnapshot CreateRegistryValueSnapshot(string id, RegistryHive hive, RegistryView view, string subKey, string name, IRegistry registry)
        {
            var originator = new RegistryValueOriginator(hive, view, subKey, name, registry);
            return new PersistentRegistryValueCaretaker(id, originator, connection);
        }

        /// <summary>
        /// Restores all abandoned snapshots on the current machine. An "abandoned snapshot" is a snapshot that was created by a process that no longer exists.
        /// </summary>
        public static void RestoreAbandonedSnapshots() => RestoreAbandonedSnapshots(new DefaultEnvironment(), new DefaultFileSystem(), new DefaultRegistry());

        /// <summary>
        /// Restores all abandoned snapshots on the current machine. An "abandoned snapshot" is a snapshot that was created by a process that no longer exists.
        /// </summary>
        /// <param name="environment">A concrete implementation of the machine's environment.</param>
        /// <param name="fileSystem">A concrete implementation of the machine's file system.</param>
        /// <param name="registry">A concrete implementation of the machine's registry.</param>
        public static void RestoreAbandonedSnapshots(IEnvironment environment, IFileSystem fileSystem, IRegistry registry)
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

            var connection = CreateConnection();
            var fileCache = new SQLiteFileCache(fileSystem, connection);

            var allSnapshots = PersistentEnvironmentVariableCaretaker.GetCaretakers(connection, environment)
                .Concat(PersistentDirectoryCaretaker.GetCaretakers(connection, fileSystem))
                .Concat(PersistentFileCaretaker.GetCaretakers(connection, fileSystem, fileCache))
                .Concat(PersistentRegistryKeyCaretaker.GetCaretakers(connection, registry))
                .Concat(PersistentRegistryValueCaretaker.GetCaretakers(connection, registry))
                .Where(c => !(processes.ContainsKey(c.ProcessID) && (processes[c.ProcessID] == c.ProcessStartTime || processes[c.ProcessID] == null)));

            var abandonedSnapshots = new List<ISnapshot>(allSnapshots);

            if (abandonedSnapshots.Any())
            {
                var systemStateManager = new PersistentSystemStateManager(abandonedSnapshots, fileCache, environment, fileSystem, registry, connection);
                systemStateManager.Dispose();
            }
        }

        private static SqliteConnection CreateConnection()
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
                DataSource = databaseFile.FullName,
                Cache = SqliteCacheMode.Shared,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false
            }.ToString();
            var connection = new SqliteConnection(connectionString);
            connection.Open();

            // Enable write-ahead logging
            var command = connection.CreateCommand();
            command.CommandText =
            @"
                PRAGMA journal_mode = 'wal'
            ";
            command.ExecuteNonQuery();

            return connection;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            connection.Dispose();
        }
    }
}
