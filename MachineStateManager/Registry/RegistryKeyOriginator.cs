using bradselw.SystemResources.Registry.Proxy;
using Microsoft.Win32;

namespace bradselw.MachineStateManager.Registry
{
    internal class RegistryKeyOriginator : IOriginator<RegistryKeyMemento>
    {
        public string ID
        {
            get
            {
                return string.Join("\\", Hive, View, SubKey).ToLower();
            }
        }

        public RegistryHive Hive { get; }

        public RegistryView View { get; }

        public string SubKey { get; }

        public IRegistryProxy Registry { get; }

        public RegistryKeyOriginator(RegistryHive hive, RegistryView view, string subKey, IRegistryProxy registry)
        {
            Hive = hive;
            View = view;
            SubKey = subKey;
            Registry = registry;
        }

        public RegistryKeyMemento GetState()
        {
            return new RegistryKeyMemento(Registry.RegistryKeyExists(Hive, View, SubKey));
        }

        public void SetState(RegistryKeyMemento memento)
        {
            if (Registry.RegistryKeyExists(Hive, View, SubKey))
            {
                if (!memento.Exists)
                {
                    Registry.DeleteRegistryKey(Hive, View, SubKey);
                }
            }
            else
            {
                if (memento.Exists)
                {
                    Registry.CreateRegistryKey(Hive, View, SubKey);
                }
            }
        }
    }
}
