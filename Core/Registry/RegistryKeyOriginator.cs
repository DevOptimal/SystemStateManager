using Microsoft.Win32;
using Registry;

namespace MachineStateManager.Core.Registry
{
    internal class RegistryKeyOriginator : IOriginator<RegistryKeyMemento>
    {
        public RegistryHive Hive { get; }

        public RegistryView View { get; }

        public string SubKey { get; }

        public IRegistry Registry { get; }

        public RegistryKeyOriginator(RegistryHive hive, RegistryView view, string subKey, IRegistry registry)
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
