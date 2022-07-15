using DevOptimal.SystemStateManager.Registry;
using DevOptimal.SystemUtilities.Registry;
using LiteDB;
using Microsoft.Win32;

namespace DevOptimal.SystemStateManager.Persistence.Registry
{
    internal class PersistentRegistryValueOriginator : RegistryValueOriginator
    {
        public PersistentRegistryValueOriginator(RegistryHive hive, RegistryView view, string subKey, string name, IRegistry registry)
            : base(hive, view, subKey, name, registry)
        {
        }

        [BsonCtor]
        public PersistentRegistryValueOriginator(RegistryHive hive, RegistryView view, string subKey, string name, BsonDocument registry)
            : this(hive, view, subKey, name, BsonMapper.Global.ToObject<IRegistry>(registry))
        {
        }
    }
}
