using MachineStateManager.Core;
using MachineStateManager.Core.Environment;
using MachineStateManager.Core.FileSystem;
using MachineStateManager.Core.FileSystem.Caching;
using MachineStateManager.Core.Registry;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace MachineStateManager
{
    public class MachineStateManager : IDisposable
    {
        private readonly IBlobStore fileCache;

        protected readonly List<IDisposable> caretakers;

        protected bool disposedValue;

        public MachineStateManager()
            : this(new LocalBlobStore(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData), nameof(MachineStateManager), "FileCache")))
        {
        }

        internal MachineStateManager(IBlobStore fileCache)
            : this(fileCache, new List<IDisposable>())
        {
        }

        internal MachineStateManager(IBlobStore fileCache, List<IDisposable> caretakers)
        {
            this.fileCache = fileCache;
            this.caretakers = caretakers;
        }

        public virtual IDisposable SnapshotEnvironmentVariable(string name)
        {
            var originator = new EnvironmentVariableOriginator(name);
            var caretaker = new Caretaker<EnvironmentVariableOriginator, EnvironmentVariableMemento>(originator);
            caretakers.Add(caretaker);
            return caretaker;
        }
        public virtual IDisposable SnapshotEnvironmentVariable(string name, EnvironmentVariableTarget target)
        {
            var originator = new EnvironmentVariableOriginator(name, target);
            var caretaker = new Caretaker<EnvironmentVariableOriginator, EnvironmentVariableMemento>(originator);
            caretakers.Add(caretaker);
            return caretaker;
        }

        public virtual IDisposable SnapshotDirectory(string path)
        {
            var originator = new DirectoryOriginator(path);
            var caretaker = new Caretaker<DirectoryOriginator, DirectoryMemento>(originator);
            caretakers.Add(caretaker);
            return caretaker;
        }

        public virtual IDisposable SnapshotFile(string path)
        {
            var originator = new FileOriginator(path, fileCache);
            var caretaker = new Caretaker<FileOriginator, FileMemento>(originator);
            caretakers.Add(caretaker);
            return caretaker;
        }

        [SupportedOSPlatform("windows")]
        public virtual IDisposable SnapshotRegistryKey(RegistryHive hive, RegistryView view, string subKey)
        {
            var originator = new RegistryKeyOriginator(hive, view, subKey);
            var caretaker = new Caretaker<RegistryKeyOriginator, RegistryKeyMemento>(originator);
            caretakers.Add(caretaker);
            return caretaker;
        }

        [SupportedOSPlatform("windows")]
        public virtual IDisposable SnapshotRegistryValue(RegistryHive hive, RegistryView view, string subKey, string name)
        {
            var originator = new RegistryValueOriginator(hive, view, subKey, name);
            var caretaker = new Caretaker<RegistryValueOriginator, RegistryValueMemento>(originator);
            caretakers.Add(caretaker);
            return caretaker;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                foreach (var caretaker in caretakers)
                {
                    caretaker.Dispose();
                }

                disposedValue = true;
            }
        }

        ~MachineStateManager()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
