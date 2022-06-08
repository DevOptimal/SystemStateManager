using Microsoft.Win32;
using System.Runtime.Versioning;

namespace MachineStateManager.Core.Registry
{
    [SupportedOSPlatform("windows")]
    internal class RegistryKeyOriginator : IOriginator<RegistryKeyMemento>
    {
        public RegistryHive Hive { get; }

        public RegistryView View { get; }

        public string SubKey { get; }

        public RegistryKeyOriginator(RegistryHive hive, RegistryView view, string subKey)
        {
            Hive = hive;
            View = view;
            SubKey = subKey;
        }

        public RegistryKeyMemento GetState()
        {
            return new RegistryKeyMemento(RegistryKey.OpenBaseKey(Hive, View).OpenSubKey(SubKey) != null);
        }

        public void SetState(RegistryKeyMemento memento)
        {
            if (RegistryKey.OpenBaseKey(Hive, View).OpenSubKey(SubKey) == null)
            {
                if (memento.Exists)
                {
                    RegistryKey.OpenBaseKey(Hive, View).CreateSubKey(SubKey);
                }
            }
            else
            {
                if (!memento.Exists)
                {
                    RegistryKey.OpenBaseKey(Hive, View).DeleteSubKey(SubKey);
                }
            }
        }
    }
}
