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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace DevOptimal.SystemStateManager
{
    public class SystemStateManager : IDisposable
    {
        private readonly List<ISnapshot> snapshots;

        private readonly IBlobStore fileCache;

        protected readonly IEnvironmentProxy defaultEnvironment;

        protected readonly IFileSystemProxy defaultFileSystem;

        protected readonly IRegistryProxy defaultRegistry;

        private bool disposedValue;

        public SystemStateManager()
            : this(new DefaultEnvironmentProxy(), new DefaultFileSystemProxy(), new DefaultRegistryProxy())
        {
        }

        public SystemStateManager(IEnvironmentProxy environment, IFileSystemProxy fileSystem, IRegistryProxy registry)
            : this(new LocalBlobStore(Path.Combine(global::System.Environment.GetFolderPath(global::System.Environment.SpecialFolder.CommonApplicationData), nameof(SystemStateManager), "FileCache"), fileSystem), environment, fileSystem, registry)
        {
        }

        internal SystemStateManager(IBlobStore fileCache, IEnvironmentProxy environment, IFileSystemProxy fileSystem, IRegistryProxy registry)
            : this(new List<ISnapshot>(), fileCache, environment, fileSystem, registry)
        {
        }

        internal SystemStateManager(List<ISnapshot> snapshots, IBlobStore fileCache)
            : this(snapshots, fileCache, new DefaultEnvironmentProxy(), new DefaultFileSystemProxy(), new DefaultRegistryProxy())
        {
        }

        internal SystemStateManager(List<ISnapshot> snapshots, IBlobStore fileCache, IEnvironmentProxy environment, IFileSystemProxy fileSystem, IRegistryProxy registry)
        {
            this.snapshots = snapshots;
            this.fileCache = fileCache;

            defaultEnvironment = environment;
            defaultFileSystem = fileSystem;
            defaultRegistry = registry;
        }

        public ISnapshot SnapshotEnvironmentVariable(string name) => SnapshotEnvironmentVariable(name, EnvironmentVariableTarget.Process);

        public virtual ISnapshot SnapshotEnvironmentVariable(string name, EnvironmentVariableTarget target)
            => SnapshotEnvironmentVariable(name, target, defaultEnvironment);

        public ISnapshot SnapshotEnvironmentVariable(string name, EnvironmentVariableTarget target, IEnvironmentProxy environment)
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
                snapshots.Add(snapshot);
            }

            return snapshot;
        }

        public ISnapshot SnapshotDirectory(string path)
            => SnapshotDirectory(path, defaultFileSystem);

        public ISnapshot SnapshotDirectory(string path, IFileSystemProxy fileSystem)
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
                snapshots.Add(snapshot);
            }

            return snapshot;
        }

        public ISnapshot SnapshotFile(string path)
            => SnapshotFile(path, defaultFileSystem);

        public ISnapshot SnapshotFile(string path, IFileSystemProxy fileSystem)
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
                snapshots.Add(snapshot);
            }

            return snapshot;
        }

        public ISnapshot SnapshotRegistryKey(RegistryHive hive, RegistryView view, string subKey)
            => SnapshotRegistryKey(hive, view, subKey, defaultRegistry);

        public ISnapshot SnapshotRegistryKey(RegistryHive hive, RegistryView view, string subKey, IRegistryProxy registry)
        {
            subKey = RegistryPath.GetFullPath(subKey);

            var id = $"[Registry]{hive}\\{view}\\{subKey}".ToLower();

            if (!TryGetSnapshot(id, out var snapshot))
            {
                snapshot = CreateRegistryKeySnapshot(id, hive, view, subKey, registry);
                snapshots.Add(snapshot);
            }

            return snapshot;
        }

        public ISnapshot SnapshotRegistryValue(RegistryHive hive, RegistryView view, string subKey, string name)
            => SnapshotRegistryValue(hive, view, subKey, name, defaultRegistry);

        public ISnapshot SnapshotRegistryValue(RegistryHive hive, RegistryView view, string subKey, string name, IRegistryProxy registry)
        {
            subKey = RegistryPath.GetFullPath(subKey);

            var id = $"[Registry]{hive}\\{view}\\{subKey}\\\\{name ?? "(Default)"}".ToLower();

            if (!TryGetSnapshot(id, out var snapshot))
            {
                snapshot = CreateRegistryValueSnapshot(id, hive, view, subKey, name, registry);
                snapshots.Add(snapshot);
            }

            return snapshot;
        }

        private bool TryGetSnapshot(string id, out ISnapshot snapshot)
        {
            snapshot = snapshots.SingleOrDefault(c => c.ID.Equals(id));
            return snapshot != null;
        }

        protected virtual ISnapshot CreateEnvironmentVariableSnapshot(string id, string name, EnvironmentVariableTarget target, IEnvironmentProxy environment)
        {
            var originator = new EnvironmentVariableOriginator(name, target, environment);
            return new Caretaker<EnvironmentVariableOriginator, EnvironmentVariableMemento>(id, originator);
        }

        protected virtual ISnapshot CreateDirectorySnapshot(string id, string path, IFileSystemProxy fileSystem)
        {
            var originator = new DirectoryOriginator(path, fileSystem);
            return new Caretaker<DirectoryOriginator, DirectoryMemento>(id, originator);
        }

        protected virtual ISnapshot CreateFileSnapshot(string id, string path, IBlobStore fileCache, IFileSystemProxy fileSystem)
        {
            var originator = new FileOriginator(path, fileCache, fileSystem);
            return new Caretaker<FileOriginator, FileMemento>(id, originator);
        }

        protected virtual ISnapshot CreateRegistryKeySnapshot(string id, RegistryHive hive, RegistryView view, string subKey, IRegistryProxy registry)
        {
            var originator = new RegistryKeyOriginator(hive, view, subKey, registry);
            return new Caretaker<RegistryKeyOriginator, RegistryKeyMemento>(id, originator);
        }

        protected virtual ISnapshot CreateRegistryValueSnapshot(string id, RegistryHive hive, RegistryView view, string subKey, string name, IRegistryProxy registry)
        {
            var originator = new RegistryValueOriginator(hive, view, subKey, name, registry);
            return new Caretaker<RegistryValueOriginator, RegistryValueMemento>(id, originator);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    var exceptions = new List<Exception>();
                    foreach (var snapshot in snapshots)
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
    }
}
