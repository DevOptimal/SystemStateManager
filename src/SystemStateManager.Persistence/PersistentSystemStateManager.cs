using DevOptimal.SystemStateManager.Environment;
using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemStateManager.FileSystem.Caching;
using DevOptimal.SystemStateManager.Persistence.Environment;
using DevOptimal.SystemStateManager.Persistence.FileSystem;
using DevOptimal.SystemStateManager.Persistence.FileSystem.Caching;
using DevOptimal.SystemStateManager.Persistence.Registry;
using DevOptimal.SystemStateManager.Registry;
using DevOptimal.SystemUtilities.Environment;
using DevOptimal.SystemUtilities.FileSystem;
using DevOptimal.SystemUtilities.FileSystem.Extensions;
using DevOptimal.SystemUtilities.Registry;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DevOptimal.SystemStateManager.Persistence
{
    public class PersistentSystemStateManager : SystemStateManager
    {
        public static Uri PersistenceURI
        {
            get => new Uri(DatabaseConnection.DefaultDatabaseFile.FullName);
            set
            {
                if (!value.IsFile)
                {
                    throw new NotSupportedException($"{nameof(PersistenceURI)} is invalid. Only local file paths are supported.");
                }

                DatabaseConnection.DefaultDatabaseFile = new FileInfo(value.LocalPath);
            }
        }

        public PersistentSystemStateManager()
            : this(new DefaultEnvironment(), new DefaultFileSystem(), new DefaultRegistry())
        {
        }

        public PersistentSystemStateManager(IEnvironment environment, IFileSystem fileSystem, IRegistry registry)
            : base(new LocalFileCache(DatabaseConnection.DefaultDatabaseFile.Directory.GetDirectory(nameof(LocalFileCache)), fileSystem), environment, fileSystem, registry)
        {
        }

        private PersistentSystemStateManager(List<ISnapshot> snapshots, IFileCache fileCache, IEnvironment environment, IFileSystem fileSystem, IRegistry registry)
            : base(snapshots, fileCache, environment, fileSystem, registry)
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

            var abandonedSnapshots = new List<ISnapshot>(allSnapshots);
            using (var connection = new DatabaseConnection(environment, fileSystem, registry))
            {
                abandonconnection.List().Where(c => !(processes.ContainsKey(c.ProcessID) && (processes[c.ProcessID] == c.ProcessStartTime || processes[c.ProcessID] == null)));
            }

            var fileCache = new SQLiteFileCache(fileSystem);

            var allSnapshots = PersistentEnvironmentVariableCaretaker.GetCaretakers(connection, environment)
                .Concat(PersistentDirectoryCaretaker.GetCaretakers(connection, fileSystem))
                .Concat(PersistentFileCaretaker.GetCaretakers(connection, fileSystem, fileCache))
                .Concat(PersistentRegistryKeyCaretaker.GetCaretakers(connection, registry))
                .Concat(PersistentRegistryValueCaretaker.GetCaretakers(connection, registry))
                .Where(c => !(processes.ContainsKey(c.ProcessID) && (processes[c.ProcessID] == c.ProcessStartTime || processes[c.ProcessID] == null)));

            var abandonedSnapshots = new List<ISnapshot>(allSnapshots);

            if (abandonedSnapshots.Any())
            {
                var systemStateManager = new PersistentSystemStateManager(abandonedSnapshots, fileCache, environment, fileSystem, registry);
                systemStateManager.Dispose();
            }
        }
    }
}
