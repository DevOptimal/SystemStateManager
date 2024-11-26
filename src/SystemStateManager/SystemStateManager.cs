using DevOptimal.SystemStateManager.Environment;
using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemStateManager.FileSystem.Caching;
using DevOptimal.SystemStateManager.Registry;
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
using System.Runtime.InteropServices;

namespace DevOptimal.SystemStateManager
{
    public class SystemStateManager : IDisposable
    {
        private readonly IDatabase database;

        private readonly IFileCache fileCache;

        protected readonly IEnvironment defaultEnvironment;

        protected readonly IFileSystem defaultFileSystem;

        protected readonly IRegistry defaultRegistry;

        private bool disposedValue;

        public SystemStateManager()
            : this(new DefaultEnvironment(), new DefaultFileSystem(), new DefaultRegistry())
        {
        }

        public SystemStateManager(IEnvironment environment, IFileSystem fileSystem, IRegistry registry)
            : this(new LocalFileCache(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData), nameof(SystemStateManager), "FileCache"), fileSystem), environment, fileSystem, registry)
        {
        }

        public SystemStateManager(IFileCache fileCache, IEnvironment environment, IFileSystem fileSystem, IRegistry registry)
            : this(new MemoryDatabase(), fileCache, environment, fileSystem, registry)
        {
        }

        public SystemStateManager(IDatabase database)
            : this(database, new DefaultEnvironment(), new DefaultFileSystem(), new DefaultRegistry())
        {
        }

        public SystemStateManager(IDatabase database, IEnvironment environment, IFileSystem fileSystem, IRegistry registry)
            : this(database, new LocalFileCache(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData), nameof(SystemStateManager), "FileCache"), fileSystem), environment, fileSystem, registry)
        {
        }

        public SystemStateManager(IDatabase database, IFileCache fileCache)
            : this(database, fileCache, new DefaultEnvironment(), new DefaultFileSystem(), new DefaultRegistry())
        {
        }

        public SystemStateManager(IDatabase database, IFileCache fileCache, IEnvironment environment, IFileSystem fileSystem, IRegistry registry)
        {
            this.database = database;
            this.fileCache = fileCache;

            defaultEnvironment = environment;
            defaultFileSystem = fileSystem;
            defaultRegistry = registry;
        }

        public ISnapshot SnapshotEnvironmentVariable(string name) => SnapshotEnvironmentVariable(name, EnvironmentVariableTarget.Process);

        public virtual ISnapshot SnapshotEnvironmentVariable(string name, EnvironmentVariableTarget target)
            => SnapshotEnvironmentVariable(name, target, defaultEnvironment);

        public ISnapshot SnapshotEnvironmentVariable(string name, EnvironmentVariableTarget target, IEnvironment environment)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var id = $"[EnvironmentVariable]{target}\\{name}";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                id = id.ToLower();
            }

            if (!TryGetSnapshot(id, out var snapshot))
            {
                snapshot = CreateEnvironmentVariableSnapshot(id, name, target, environment);
            }

            return snapshot;
        }

        public ISnapshot SnapshotDirectory(string path)
            => SnapshotDirectory(path, defaultFileSystem);

        public ISnapshot SnapshotDirectory(string path, IFileSystem fileSystem)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            path = Path.GetFullPath(path);

            var id = $"[FileSystem]{path}";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                id = id.ToLower();
            }

            if (!TryGetSnapshot(id, out var snapshot))
            {
                snapshot = CreateDirectorySnapshot(id, path, fileSystem);
            }

            return snapshot;
        }

        public ISnapshot SnapshotFile(string path)
            => SnapshotFile(path, defaultFileSystem);

        public ISnapshot SnapshotFile(string path, IFileSystem fileSystem)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            path = Path.GetFullPath(path);

            var id = $"[FileSystem]{path}";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                id = id.ToLower();
            }

            if (!TryGetSnapshot(id, out var snapshot))
            {
                snapshot = CreateFileSnapshot(id, path, fileCache, fileSystem);
            }

            return snapshot;
        }

        public ISnapshot SnapshotRegistryKey(RegistryHive hive, RegistryView view, string subKey)
            => SnapshotRegistryKey(hive, view, subKey, defaultRegistry);

        public ISnapshot SnapshotRegistryKey(RegistryHive hive, RegistryView view, string subKey, IRegistry registry)
        {
            subKey = RegistryPath.GetFullPath(subKey);

            var id = $"[Registry]{hive}\\{view}\\{subKey}".ToLower();

            if (!TryGetSnapshot(id, out var snapshot))
            {
                snapshot = CreateRegistryKeySnapshot(id, hive, view, subKey, registry);
            }

            return snapshot;
        }

        public ISnapshot SnapshotRegistryValue(RegistryHive hive, RegistryView view, string subKey, string name)
            => SnapshotRegistryValue(hive, view, subKey, name, defaultRegistry);

        public ISnapshot SnapshotRegistryValue(RegistryHive hive, RegistryView view, string subKey, string name, IRegistry registry)
        {
            subKey = RegistryPath.GetFullPath(subKey);

            var id = $"[Registry]{hive}\\{view}\\{subKey}\\\\{name ?? "(Default)"}".ToLower();

            if (!TryGetSnapshot(id, out var snapshot))
            {
                snapshot = CreateRegistryValueSnapshot(id, hive, view, subKey, name, registry);
            }

            return snapshot;
        }

        private bool TryGetSnapshot(string id, out ISnapshot snapshot)
        {
            snapshot = database.GetSnapshot(id);
            return snapshot != null;
        }

        protected virtual ISnapshot CreateEnvironmentVariableSnapshot(string id, string name, EnvironmentVariableTarget target, IEnvironment environment)
        {
            var originator = new EnvironmentVariableOriginator(name, target, environment);
            return new Caretaker<EnvironmentVariableOriginator, EnvironmentVariableMemento>(id, database, originator);
        }

        protected virtual ISnapshot CreateDirectorySnapshot(string id, string path, IFileSystem fileSystem)
        {
            var originator = new DirectoryOriginator(path, fileSystem);
            return new Caretaker<DirectoryOriginator, DirectoryMemento>(id, database, originator);
        }

        protected virtual ISnapshot CreateFileSnapshot(string id, string path, IFileCache fileCache, IFileSystem fileSystem)
        {
            var originator = new FileOriginator(path, fileCache, fileSystem);
            return new Caretaker<FileOriginator, FileMemento>(id, database, originator);
        }

        protected virtual ISnapshot CreateRegistryKeySnapshot(string id, RegistryHive hive, RegistryView view, string subKey, IRegistry registry)
        {
            var originator = new RegistryKeyOriginator(hive, view, subKey, registry);
            return new Caretaker<RegistryKeyOriginator, RegistryKeyMemento>(id, database, originator);
        }

        protected virtual ISnapshot CreateRegistryValueSnapshot(string id, RegistryHive hive, RegistryView view, string subKey, string name, IRegistry registry)
        {
            var originator = new RegistryValueOriginator(hive, view, subKey, name, registry);
            return new Caretaker<RegistryValueOriginator, RegistryValueMemento>(id, database, originator);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    var exceptions = new List<Exception>();
                    foreach (var snapshot in database.GetSnapshots())
                    {
                        try
                        {
                            snapshot.Dispose();
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    }
                    if (exceptions.Any())
                    {
                        throw new AggregateException(exceptions);
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~SystemStateManager()
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

        public static void RestorAbandonedSnapshots(IDatabase database)
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

            database.GetSnapshots().Where(c => !(processes.ContainsKey(c.ProcessID) && (processes[c.ProcessID] == c.ProcessStartTime || processes[c.ProcessID] == null)));
        }
    }
}
