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
using System.Runtime.InteropServices;

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

        protected override ICaretaker GetDirectoryCaretaker(string path, IFileSystemProxy fileSystem)
        {
            var originator = new PersistentDirectoryOriginator(path, fileSystem);

            if (!TryGetCaretaker<PersistentDirectoryCaretaker>(originator.ID, out var caretaker))
            {
                caretaker = new PersistentDirectoryCaretaker(originator);
            }

            return caretaker;
        }

        protected override ICaretaker GetFileCaretaker(string path, IBlobStore fileCache, IFileSystemProxy fileSystem)
        {
            var originator = new PersistentFileOriginator(path, fileCache, fileSystem);

            if (!TryGetCaretaker<PersistentFileCaretaker>(originator.ID, out var caretaker))
            {
                caretaker = new PersistentFileCaretaker(originator);
            }

            return caretaker;
        }

        protected override ICaretaker GetRegistryKeyCaretaker(RegistryHive hive, RegistryView view, string subKey, IRegistryProxy registry)
        {
            var originator = new PersistentRegistryKeyOriginator(hive, view, subKey, registry);

            if (!TryGetCaretaker<PersistentRegistryKeyCaretaker>(originator.ID, out var caretaker))
            {
                caretaker = new PersistentRegistryKeyCaretaker(originator);
            }

            return caretaker;
        }

        protected override ICaretaker GetRegistryValueCaretaker(RegistryHive hive, RegistryView view, string subKey, string name, IRegistryProxy registry)
        {
            var originator = new PersistentRegistryValueOriginator(hive, view, subKey, name, registry);

            if (!TryGetCaretaker<PersistentRegistryValueCaretaker>(originator.ID, out var caretaker))
            {
                caretaker = new PersistentRegistryValueCaretaker(originator);
            }

            return caretaker;
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

            var abandonedCaretakers = new List<ICaretaker>();

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