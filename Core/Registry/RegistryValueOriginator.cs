using Microsoft.Win32;

namespace MachineStateManager.Core.Registry
{
    internal class RegistryValueOriginator : IOriginator<RegistryValueMemento>
    {
        public RegistryHive Hive { get; }

        public RegistryView View { get; }

        public string SubKey { get; }

        public string Name { get; }

        public RegistryValueOriginator(RegistryHive hive, RegistryView view, string subKey, string name)
        {
            Hive = hive;
            View = view;
            SubKey = subKey;
            Name = name;
        }

        public RegistryValueMemento GetState()
        {
            var regKey = RegistryKey.OpenBaseKey(Hive, View).OpenSubKey(SubKey);

            return new RegistryValueMemento(regKey?.GetValue(Name), regKey?.GetValueKind(Name) ?? RegistryValueKind.None);
        }

        public void SetState(RegistryValueMemento memento)
        {
            var regKey = RegistryKey.OpenBaseKey(Hive, View).OpenSubKey(SubKey, writable: true);

            if (regKey == null)
            {
                regKey = RegistryKey.OpenBaseKey(Hive, View).CreateSubKey(SubKey);
            }

            if (memento.Value == null)
            {
                if (regKey.GetValue(Name) != null)
                {
                    regKey.DeleteValue(Name, throwOnMissingValue: false);
                }
            }
            else
            {
                regKey.SetValue(Name, memento.Value, memento.Kind);
            }
        }
    }
}
