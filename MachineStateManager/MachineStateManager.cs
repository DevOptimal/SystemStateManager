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

namespace bradselw.MachineStateManager
{
    public class MachineStateManager : IDisposable
    {
        private readonly List<IDisposable> caretakers;

        private readonly IBlobStore fileCache;

        private readonly IEnvironmentProxy defaultEnvironment;

        private readonly IFileSystemProxy defaultFileSystem;

        private readonly IRegistryProxy defaultRegistry;

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
            : this(new List<IDisposable>(), fileCache, environment, fileSystem, registry)
        {
        }

        internal MachineStateManager(List<IDisposable> caretakers, IBlobStore fileCache)
            : this(caretakers, fileCache, new DefaultEnvironmentProxy(), new DefaultFileSystemProxy(), new DefaultRegistryProxy())
        {
        }

        internal MachineStateManager(List<IDisposable> caretakers, IBlobStore fileCache, IEnvironmentProxy environment, IFileSystemProxy fileSystem, IRegistryProxy registry)
        {
            this.caretakers = caretakers;
            this.fileCache = fileCache;

            defaultEnvironment = environment;
            defaultFileSystem = fileSystem;
            defaultRegistry = registry;
        }

        public IDisposable SnapshotEnvironmentVariable(string name) => SnapshotEnvironmentVariable(name, EnvironmentVariableTarget.Process);

        public virtual IDisposable SnapshotEnvironmentVariable(string name, EnvironmentVariableTarget target)
            => SnapshotEnvironmentVariable(name, target, defaultEnvironment);

        public IDisposable SnapshotEnvironmentVariable(string name, EnvironmentVariableTarget target, IEnvironmentProxy environment)
        {
            var caretaker = GetEnvironmentVariableCaretaker(name, target, environment);
            caretakers.Add(caretaker);
            return caretaker;
        }

        protected virtual IDisposable GetEnvironmentVariableCaretaker(string name, EnvironmentVariableTarget target, IEnvironmentProxy environment)
        {
            var originator = new EnvironmentVariableOriginator(name, target, defaultEnvironment);
            return new Caretaker<EnvironmentVariableOriginator, EnvironmentVariableMemento>(originator);
        }

        public IDisposable SnapshotDirectory(string path)
            => SnapshotDirectory(path, defaultFileSystem);

        public IDisposable SnapshotDirectory(string path, IFileSystemProxy fileSystem)
        {
            var caretaker = GetDirectoryCaretaker(path, fileSystem);
            caretakers.Add(caretaker);
            return caretaker;
        }

        protected virtual IDisposable GetDirectoryCaretaker(string path, IFileSystemProxy fileSystem)
        {
            var originator = new DirectoryOriginator(path, fileSystem);
            return new Caretaker<DirectoryOriginator, DirectoryMemento>(originator);
        }

        public IDisposable SnapshotFile(string path)
            => SnapshotFile(path, defaultFileSystem);

        public IDisposable SnapshotFile(string path, IFileSystemProxy fileSystem)
        {
            var caretaker = GetFileCaretaker(path, fileSystem);
            caretakers.Add(caretaker);
            return caretaker;
        }

        protected virtual IDisposable GetFileCaretaker(string path, IFileSystemProxy fileSystem)
        {
            var originator = new FileOriginator(path, fileCache, fileSystem);
            return new Caretaker<FileOriginator, FileMemento>(originator);
        }

        public IDisposable SnapshotRegistryKey(RegistryHive hive, RegistryView view, string subKey)
            => SnapshotRegistryKey(hive, view, subKey, defaultRegistry);

        public IDisposable SnapshotRegistryKey(RegistryHive hive, RegistryView view, string subKey, IRegistryProxy registry)
        {
            var caretaker = GetRegistryKeyCaretaker(hive, view, subKey, registry);
            caretakers.Add(caretaker);
            return caretaker;
        }

        protected virtual IDisposable GetRegistryKeyCaretaker(RegistryHive hive, RegistryView view, string subKey, IRegistryProxy registry)
        {
            var originator = new RegistryKeyOriginator(hive, view, subKey, registry);
            return new Caretaker<RegistryKeyOriginator, RegistryKeyMemento>(originator);
        }

        public IDisposable SnapshotRegistryValue(RegistryHive hive, RegistryView view, string subKey, string name)
            => SnapshotRegistryValue(hive, view, subKey, name, defaultRegistry);

        public IDisposable SnapshotRegistryValue(RegistryHive hive, RegistryView view, string subKey, string name, IRegistryProxy registry)
        {
            var caretaker = GetRegistryValueCaretaker(hive, view, subKey, name, registry);
            caretakers.Add(caretaker);
            return caretaker;
        }

        protected virtual IDisposable GetRegistryValueCaretaker(RegistryHive hive, RegistryView view, string subKey, string name, IRegistryProxy registry)
        {
            var originator = new RegistryValueOriginator(hive, view, subKey, name, registry);
            return new Caretaker<RegistryValueOriginator, RegistryValueMemento>(originator);
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
