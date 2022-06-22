using bradselw.MachineStateManager.Environment;
using bradselw.MachineStateManager.FileSystem;
using bradselw.MachineStateManager.FileSystem.Caching;
using bradselw.MachineStateManager.Registry;
using bradselw.SystemResources.Environment.Proxy;
using bradselw.SystemResources.FileSystem.Proxy;
using bradselw.SystemResources.Registry.Proxy;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace bradselw.MachineStateManager
{
    public class MachineStateManager : IDisposable
    {
        private readonly List<ICaretaker> caretakers;

        private readonly IBlobStore fileCache;

        protected readonly IEnvironmentProxy defaultEnvironment;

        protected readonly IFileSystemProxy defaultFileSystem;

        protected readonly IRegistryProxy defaultRegistry;

        private bool disposedValue;

        public MachineStateManager()
            : this(new DefaultEnvironmentProxy(), new DefaultFileSystemProxy(), new DefaultRegistryProxy())
        {
        }

        public MachineStateManager(IEnvironmentProxy environment, IFileSystemProxy fileSystem, IRegistryProxy registry)
            : this(new LocalBlobStore(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData), nameof(MachineStateManager), "FileCache"), fileSystem), environment, fileSystem, registry)
        {
        }

        internal MachineStateManager(IBlobStore fileCache, IEnvironmentProxy environment, IFileSystemProxy fileSystem, IRegistryProxy registry)
            : this(new List<ICaretaker>(), fileCache, environment, fileSystem, registry)
        {
        }

        internal MachineStateManager(List<ICaretaker> caretakers, IBlobStore fileCache)
            : this(caretakers, fileCache, new DefaultEnvironmentProxy(), new DefaultFileSystemProxy(), new DefaultRegistryProxy())
        {
        }

        internal MachineStateManager(List<ICaretaker> caretakers, IBlobStore fileCache, IEnvironmentProxy environment, IFileSystemProxy fileSystem, IRegistryProxy registry)
        {
            this.caretakers = caretakers;
            this.fileCache = fileCache;

            defaultEnvironment = environment;
            defaultFileSystem = fileSystem;
            defaultRegistry = registry;
        }

        public ICaretaker SnapshotEnvironmentVariable(string name) => SnapshotEnvironmentVariable(name, EnvironmentVariableTarget.Process);

        public virtual ICaretaker SnapshotEnvironmentVariable(string name, EnvironmentVariableTarget target)
            => SnapshotEnvironmentVariable(name, target, defaultEnvironment);

        public ICaretaker SnapshotEnvironmentVariable(string name, EnvironmentVariableTarget target, IEnvironmentProxy environment)
        {
            var id = string.Join("\\", "EnvironmentVariable", target, name);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                id = id.ToLower();
            }

            var caretaker = caretakers.SingleOrDefault(c => c.ID.Equals(id));

            if (caretaker == null)
            {
                caretaker = GetEnvironmentVariableCaretaker(id, name, target, environment);
                caretakers.Add(caretaker);
            }

            return caretaker;
        }

        public ICaretaker SnapshotDirectory(string path)
            => SnapshotDirectory(path, defaultFileSystem);

        public ICaretaker SnapshotDirectory(string path, IFileSystemProxy fileSystem)
        {
            var caretaker = GetDirectoryCaretaker(path, fileSystem);
            caretakers.Add(caretaker);
            return caretaker;
        }

        public ICaretaker SnapshotFile(string path)
            => SnapshotFile(path, defaultFileSystem);

        public ICaretaker SnapshotFile(string path, IFileSystemProxy fileSystem)
        {
            var caretaker = GetFileCaretaker(path, fileCache, fileSystem);
            caretakers.Add(caretaker);
            return caretaker;
        }

        public ICaretaker SnapshotRegistryKey(RegistryHive hive, RegistryView view, string subKey)
            => SnapshotRegistryKey(hive, view, subKey, defaultRegistry);

        public ICaretaker SnapshotRegistryKey(RegistryHive hive, RegistryView view, string subKey, IRegistryProxy registry)
        {
            var caretaker = GetRegistryKeyCaretaker(hive, view, subKey, registry);
            caretakers.Add(caretaker);
            return caretaker;
        }

        public ICaretaker SnapshotRegistryValue(RegistryHive hive, RegistryView view, string subKey, string name)
            => SnapshotRegistryValue(hive, view, subKey, name, defaultRegistry);

        public ICaretaker SnapshotRegistryValue(RegistryHive hive, RegistryView view, string subKey, string name, IRegistryProxy registry)
        {
            var caretaker = GetRegistryValueCaretaker(hive, view, subKey, name, registry);
            caretakers.Add(caretaker);
            return caretaker;
        }

        protected bool TryGetCaretaker<TCaretaker>(string id, out TCaretaker caretaker)
            where TCaretaker : ICaretaker
        {
            caretaker = caretakers.OfType<TCaretaker>().SingleOrDefault(c => c.ID.Equals(id));

            return caretaker != null;
        }

        protected virtual ICaretaker GetEnvironmentVariableCaretaker(string id, string name, EnvironmentVariableTarget target, IEnvironmentProxy environment)
        {
            var originator = new EnvironmentVariableOriginator(name, target, environment);
            return new Caretaker<EnvironmentVariableOriginator, EnvironmentVariableMemento>(id, originator);
        }

        protected virtual ICaretaker GetDirectoryCaretaker(string path, IFileSystemProxy fileSystem)
        {
            var originator = new DirectoryOriginator(path, fileSystem);

            if (!TryGetCaretaker<Caretaker<DirectoryOriginator, DirectoryMemento>>(originator.ID, out var caretaker))
            {
                caretaker = new Caretaker<DirectoryOriginator, DirectoryMemento>(originator);
            }

            return caretaker;
        }

        protected virtual ICaretaker GetFileCaretaker(string path, IBlobStore fileCache, IFileSystemProxy fileSystem)
        {
            var originator = new FileOriginator(path, fileCache, fileSystem);

            if (!TryGetCaretaker<Caretaker<FileOriginator, FileMemento>>(originator.ID, out var caretaker))
            {
                caretaker = new Caretaker<FileOriginator, FileMemento>(originator);
            }

            return caretaker;
        }

        protected virtual ICaretaker GetRegistryKeyCaretaker(RegistryHive hive, RegistryView view, string subKey, IRegistryProxy registry)
        {
            var originator = new RegistryKeyOriginator(hive, view, subKey, registry);

            if (!TryGetCaretaker<Caretaker<RegistryKeyOriginator, RegistryKeyMemento>>(originator.ID, out var caretaker))
            {
                caretaker = new Caretaker<RegistryKeyOriginator, RegistryKeyMemento>(originator);
            }

            return caretaker;
        }

        protected virtual ICaretaker GetRegistryValueCaretaker(RegistryHive hive, RegistryView view, string subKey, string name, IRegistryProxy registry)
        {
            var originator = new RegistryValueOriginator(hive, view, subKey, name, registry);

            if (!TryGetCaretaker<Caretaker<RegistryValueOriginator, RegistryValueMemento>>(originator.ID, out var caretaker))
            {
                caretaker = new Caretaker<RegistryValueOriginator, RegistryValueMemento>(originator);
            }

            return caretaker;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    var exceptions = new List<Exception>();
                    foreach (var caretaker in caretakers)
                    {
                        try
                        {
                            caretaker.Dispose();
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
        // ~MachineStateManager()
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
