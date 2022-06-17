using Environment;
using FileSystem;
using MachineStateManager.Core.FileSystem;
using MachineStateManager.Persistence.Environment;
using MachineStateManager.Persistence.FileSystem;
using MachineStateManager.Persistence.FileSystem.Caching;
using MachineStateManager.Persistence.Registry;
using Microsoft.Win32;
using Registry;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MachineStateManager.Persistence
{
    public class PersistentMachineStateManager : MachineStateManager
    {
        public PersistentMachineStateManager()
            : base()
        {
        }

        internal PersistentMachineStateManager(IEnvironment environment, IFileSystem fileSystem, IRegistry registry)
            : base(new LiteDBBlobStore(), environment, fileSystem, registry)
        {
        }

        internal PersistentMachineStateManager(List<IDisposable> caretakers)
            : base(caretakers, new LiteDBBlobStore())
        {
        }

        protected override IDisposable GetEnvironmentVariableCaretaker(string name, EnvironmentVariableTarget target, IEnvironment environment)
        {
            return new PersistentEnvironmentVariableCaretaker(name, environment);
        }

        protected override IDisposable GetDirectoryCaretaker(string path, IFileSystem fileSystem)
        {
            return new PersistentDirectoryCaretaker(path, fileSystem);
        }

        protected override IDisposable GetFileCaretaker(string path, IFileSystem fileSystem)
        {
            return new PersistentFileCaretaker(path, new LiteDBBlobStore(), fileSystem);
        }

        protected override IDisposable GetRegistryKeyCaretaker(RegistryHive hive, RegistryView view, string subKey, IRegistry registry)
        {
            return new PersistentRegistryKeyCaretaker(hive, view, subKey, registry);
        }

        protected override IDisposable GetRegistryValueCaretaker(RegistryHive hive, RegistryView view, string subKey, string name, IRegistry registry)
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