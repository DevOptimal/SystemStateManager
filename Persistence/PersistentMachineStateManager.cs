using MachineStateManager.Persistence.Environment;
using MachineStateManager.Persistence.FileSystem;
using MachineStateManager.Persistence.FileSystem.Caching;
using MachineStateManager.Persistence.Registry;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace MachineStateManager.Persistence
{
    public class PersistentMachineStateManager : MachineStateManager
    {
        public PersistentMachineStateManager()
            : base(new LiteDBBlobStore())
        {
        }

        private PersistentMachineStateManager(List<IDisposable> caretakers)
            : base(new LiteDBBlobStore(), caretakers)
        {
        }

        public override IDisposable SnapshotEnvironmentVariable(string name)
        {
            var caretaker = new PersistentEnvironmentVariableCaretaker(name);
            caretakers.Add(caretaker);
            return caretaker;
        }

        public override IDisposable SnapshotEnvironmentVariable(string name, EnvironmentVariableTarget target)
        {
            var caretaker = new PersistentEnvironmentVariableCaretaker(name, target);
            caretakers.Add(caretaker);
            return caretaker;
        }

        public override IDisposable SnapshotDirectory(string path)
        {
            var caretaker = new PersistentDirectoryCaretaker(path);
            caretakers.Add(caretaker);
            return caretaker;
        }

        public override IDisposable SnapshotFile(string path)
        {
            var caretaker = new PersistentFileCaretaker(path, new LiteDBBlobStore());
            caretakers.Add(caretaker);
            return caretaker;
        }

        [SupportedOSPlatform("windows")]
        public override IDisposable SnapshotRegistryKey(RegistryHive hive, RegistryView view, string subKey)
        {
            var caretaker = new PersistentRegistryKeyCaretaker(hive, view, subKey);
            caretakers.Add(caretaker);
            return caretaker;
        }

        [SupportedOSPlatform("windows")]
        public override IDisposable SnapshotRegistryValue(RegistryHive hive, RegistryView view, string subKey, string name)
        {
            var caretaker = new PersistentRegistryValueCaretaker(hive, view, subKey, name);
            caretakers.Add(caretaker);
            return caretaker;
        }

        public static void RestoreAbandonedCaretakers()
        {
            var abandonedCaretakers = new List<IDisposable>();

            abandonedCaretakers.AddRange(PersistentEnvironmentVariableCaretaker.GetAbandonedCaretakers());
            abandonedCaretakers.AddRange(PersistentDirectoryCaretaker.GetAbandonedCaretakers());
            abandonedCaretakers.AddRange(PersistentFileCaretaker.GetAbandonedCaretakers());
            if (OperatingSystem.IsWindows())
            {
                abandonedCaretakers.AddRange(PersistentRegistryKeyCaretaker.GetAbandonedCaretakers());
                abandonedCaretakers.AddRange(PersistentRegistryValueCaretaker.GetAbandonedCaretakers());
            }

            var machineStateManager = new PersistentMachineStateManager(abandonedCaretakers);

            machineStateManager.Dispose();
        }
    }
}