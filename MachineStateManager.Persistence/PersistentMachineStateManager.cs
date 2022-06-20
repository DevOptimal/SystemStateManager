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
using System.Runtime.InteropServices;

namespace bradselw.MachineStateManager.Persistence
{
    public class PersistentMachineStateManager : MachineStateManager
    {
        public PersistentMachineStateManager()
            : this(new DefaultEnvironmentProxy(), new DefaultFileSystemProxy(), new DefaultRegistryProxy())
        {
        }

        internal PersistentMachineStateManager(IEnvironmentProxy environment, IFileSystemProxy fileSystem, IRegistryProxy registry)
            : base(new LiteDBBlobStore(fileSystem), environment, fileSystem, registry)
        {
        }

        internal PersistentMachineStateManager(List<IDisposable> caretakers)
            : base(caretakers, new LiteDBBlobStore(new DefaultFileSystemProxy()))
        {
        }

        protected override IDisposable GetEnvironmentVariableCaretaker(string name, EnvironmentVariableTarget target, IEnvironmentProxy environment)
        {
            return new PersistentEnvironmentVariableCaretaker(name, environment);
        }

        protected override IDisposable GetDirectoryCaretaker(string path, IFileSystemProxy fileSystem)
        {
            return new PersistentDirectoryCaretaker(path, fileSystem);
        }

        protected override IDisposable GetFileCaretaker(string path, IFileSystemProxy fileSystem)
        {
            return new PersistentFileCaretaker(path, new LiteDBBlobStore(fileSystem), fileSystem);
        }

        protected override IDisposable GetRegistryKeyCaretaker(RegistryHive hive, RegistryView view, string subKey, IRegistryProxy registry)
        {
            return new PersistentRegistryKeyCaretaker(hive, view, subKey, registry);
        }

        protected override IDisposable GetRegistryValueCaretaker(RegistryHive hive, RegistryView view, string subKey, string name, IRegistryProxy registry)
        {
            return new PersistentRegistryValueCaretaker(hive, view, subKey, name, registry);
        }

        public static void RestoreAbandonedCaretakers()
        {
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

            var abandonedCaretakers = new List<IDisposable>();

            abandonedCaretakers.AddRange(PersistentEnvironmentVariableCaretaker.GetAbandonedCaretakers(processes));
            abandonedCaretakers.AddRange(PersistentDirectoryCaretaker.GetAbandonedCaretakers(processes));
            abandonedCaretakers.AddRange(PersistentFileCaretaker.GetAbandonedCaretakers(processes));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                abandonedCaretakers.AddRange(PersistentRegistryKeyCaretaker.GetAbandonedCaretakers(processes));
                abandonedCaretakers.AddRange(PersistentRegistryValueCaretaker.GetAbandonedCaretakers(processes));
            }

            var machineStateManager = new PersistentMachineStateManager(abandonedCaretakers);

            machineStateManager.Dispose();
        }
    }
}