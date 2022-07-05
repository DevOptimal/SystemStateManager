using DevOptimal.SystemStateManager.Registry;
using DevOptimal.System.Resources.Registry;
using LiteDB;
using Microsoft.Win32;

namespace DevOptimal.SystemStateManager.Persistence.Registry
{
    internal class PersistentRegistryValueOriginator : RegistryValueOriginator
    {
        [BsonCtor]
        public PersistentRegistryValueOriginator(RegistryHive hive, RegistryView view, string subKey, string name, BsonDocument registry)
            : this(hive, view, subKey, name, BsonMapper.Global.ToObject<IRegistryProxy>(registry))
        {
        }

        public PersistentRegistryValueOriginator(RegistryHive hive, RegistryView view, string subKey, string name, IRegistryProxy registry)
            : base(hive, view, subKey, name, registry)
        {
        }
    }
}
