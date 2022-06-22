using bradselw.SystemResources.Registry.Proxy;
using Microsoft.Win32;

namespace bradselw.MachineStateManager.Registry
{
    internal class RegistryValueOriginator : IOriginator<RegistryValueMemento>
    {
        public string ID
        {
            get
            {
                return string.Join("\\", Hive, View, SubKey, Name ?? "(Default)").ToLower();
            }
        }

        public RegistryHive Hive { get; }

        public RegistryView View { get; }

        public string SubKey { get; }

        public string Name { get; }

        public IRegistryProxy Registry { get; }

        public RegistryValueOriginator(RegistryHive hive, RegistryView view, string subKey, string name, IRegistryProxy registry)
        {
            Hive = hive;
            View = view;
            SubKey = subKey;
            Name = name;
            Registry = registry;
        }

        public RegistryValueMemento GetState()
        {
            var (value, kind) = Registry.GetRegistryValue(Hive, View, SubKey, Name);

            return new RegistryValueMemento(value, kind);
        }

        public void SetState(RegistryValueMemento memento)
        {
            if (!Registry.RegistryKeyExists(Hive, View, SubKey))
            {
                Registry.CreateRegistryKey(Hive, View, SubKey);
            }

            if (memento.Value == null)
            {
                if (Registry.RegistryValueExists(Hive, View, SubKey, Name))
                {
                    Registry.DeleteRegistryValue(Hive, View, SubKey, Name);
                }
            }
            else
            {
                Registry.SetRegistryValue(Hive, View, SubKey, Name, memento.Value, memento.Kind);
            }
        }
    }
}
