using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemStateManager.Persistence.Environment;
using DevOptimal.SystemStateManager.Persistence.FileSystem;
using DevOptimal.SystemStateManager.Persistence.FileSystem.Caching;
using DevOptimal.SystemStateManager.Persistence.Registry;
using DevOptimal.SystemUtilities.Environment;
using DevOptimal.SystemUtilities.FileSystem;
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
        public static Uri PersistenceURI { get; set; } = new Uri(
            Path.Combine(
                global::System.Environment.GetFolderPath(global::System.Environment.SpecialFolder.CommonApplicationData),
                nameof(SystemStateManager),
                $"{nameof(Persistence)}.litedb"));

        public PersistentSystemStateManager()
            : this(new DefaultEnvironmentProxy(), new DefaultFileSystemProxy(), new DefaultRegistryProxy())
        {
        }

        protected PersistentSystemStateManager(IEnvironmentProxy environment, IFileSystemProxy fileSystem, IRegistryProxy registry)
            : base(new LiteDBBlobStore(fileSystem), environment, fileSystem, registry)
        {
        }

        internal PersistentSystemStateManager(List<ISnapshot> snapshots)
            : base(snapshots, new LiteDBBlobStore(new DefaultFileSystemProxy()))
        {
        }

        protected override ISnapshot CreateEnvironmentVariableSnapshot(string id, string name, EnvironmentVariableTarget target, IEnvironmentProxy environment)
        {
            var originator = new PersistentEnvironmentVariableOriginator(name, target, environment);
            return new PersistentEnvironmentVariableCaretaker(id, originator);
        }

        protected override ISnapshot CreateDirectorySnapshot(string id, string path, IFileSystemProxy fileSystem)
        {
            var originator = new PersistentDirectoryOriginator(path, fileSystem);
            return new PersistentDirectoryCaretaker(id, originator);
        }

        protected override ISnapshot CreateFileSnapshot(string id, string path, IBlobStore fileCache, IFileSystemProxy fileSystem)
        {
            var originator = new PersistentFileOriginator(path, fileCache, fileSystem);
            return new PersistentFileCaretaker(id, originator);
        }

        protected override ISnapshot CreateRegistryKeySnapshot(string id, RegistryHive hive, RegistryView view, string subKey, IRegistryProxy registry)
        {
            var originator = new PersistentRegistryKeyOriginator(hive, view, subKey, registry);
            return new PersistentRegistryKeyCaretaker(id, originator);
        }

        protected override ISnapshot CreateRegistryValueSnapshot(string id, RegistryHive hive, RegistryView view, string subKey, string name, IRegistryProxy registry)
        {
            var originator = new PersistentRegistryValueOriginator(hive, view, subKey, name, registry);
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