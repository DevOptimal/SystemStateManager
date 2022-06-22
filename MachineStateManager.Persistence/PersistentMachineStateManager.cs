using bradselw.MachineStateManager.FileSystem;
using bradselw.MachineStateManager.Persistence.Environment;
using bradselw.MachineStateManager.Persistence.FileSystem;
using bradselw.MachineStateManager.Persistence.FileSystem.Caching;
using bradselw.MachineStateManager.Persistence.Registry;
using bradselw.SystemResources.Environment.Proxy;
using bradselw.SystemResources.FileSystem.Proxy;
using bradselw.SystemResources.Registry.Proxy;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace bradselw.MachineStateManager.Persistence
{
    public class PersistentMachineStateManager : MachineStateManager
    {
        public PersistentMachineStateManager()
            : this(new DefaultEnvironmentProxy(), new DefaultFileSystemProxy(), new DefaultRegistryProxy())
        {
        }

        protected PersistentMachineStateManager(IEnvironmentProxy environment, IFileSystemProxy fileSystem, IRegistryProxy registry)
            : base(new LiteDBBlobStore(fileSystem), environment, fileSystem, registry)
        {
        }

        internal PersistentMachineStateManager(List<ICaretaker> caretakers)
            : base(caretakers, new LiteDBBlobStore(new DefaultFileSystemProxy()))
        {
        }

        protected override ICaretaker GetEnvironmentVariableCaretaker(string id, string name, EnvironmentVariableTarget target, IEnvironmentProxy environment)
        {
            var originator = new PersistentEnvironmentVariableOriginator(name, target, environment);
            return new PersistentEnvironmentVariableCaretaker(id, originator);
        }

        protected override ICaretaker GetDirectoryCaretaker(string id, string path, IFileSystemProxy fileSystem)
        {
            var originator = new PersistentDirectoryOriginator(path, fileSystem);
            return new PersistentDirectoryCaretaker(id, originator);
        }

        protected override ICaretaker GetFileCaretaker(string id, string path, IBlobStore fileCache, IFileSystemProxy fileSystem)
        {
            var originator = new PersistentFileOriginator(path, fileCache, fileSystem);
            return new PersistentFileCaretaker(id, originator);
        }

        protected override ICaretaker GetRegistryKeyCaretaker(string id, RegistryHive hive, RegistryView view, string subKey, IRegistryProxy registry)
        {
            var originator = new PersistentRegistryKeyOriginator(hive, view, subKey, registry);
            return new PersistentRegistryKeyCaretaker(id, originator);
        }

        protected override ICaretaker GetRegistryValueCaretaker(string id, RegistryHive hive, RegistryView view, string subKey, string name, IRegistryProxy registry)
        {
            var originator = new PersistentRegistryValueOriginator(hive, view, subKey, name, registry);
            return new PersistentRegistryValueCaretaker(id, originator);
        }

        /// <summary>
        /// Gets abandoned caretakers on the current machine. An "abandoned caretaker" is a caretaker that was created by a process that no longer exists.
        /// </summary>
        /// <returns>An enumeration of all caretakers on this machine that have been abandoned.</returns>
        public static void RestoreAbandonedCaretakers()
        {
            // Create a dictionary that maps process IDs to process start times, which will be used to uniquely identify a currently running process.
            // A null value indicates that this process does not have permission to the other process - try rerunning in an elevated process.
            // Pass this data in instead of calling Process.GetProcesses() because it is an expensive call.
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

            var abandonedCaretakers = new List<ICaretaker>();

            using (var database = LiteDatabaseFactory.GetDatabase())
            {
                abandonedCaretakers.AddRange(database.GetCollection<IPersistentCaretaker>().FindAll()
                    .Where(c => !(processes.ContainsKey(c.ProcessID) &&
                        (
                            processes[c.ProcessID] == c.ProcessStartTime ||
                            processes[c.ProcessID] == null
                        )))
                    .Cast<ICaretaker>());
            }

            if (abandonedCaretakers.Any())
            {
                var machineStateManager = new PersistentMachineStateManager(abandonedCaretakers);
                machineStateManager.Dispose();
            }
        }
    }
}