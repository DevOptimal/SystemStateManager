using LiteDB;
using MachineStateManager.Core.Registry;
using Microsoft.Win32;
using Registry;

namespace MachineStateManager.Persistence.Registry
{
    internal class PersistentRegistryValueOriginator : RegistryValueOriginator
    {
        [BsonCtor]
        public PersistentRegistryValueOriginator(RegistryHive hive, RegistryView view, string subKey, string name, BsonDocument registry)
            : this(hive, view, subKey, name, BsonMapper.Global.ToObject<IRegistry>(registry))
        {
        }

        public PersistentRegistryValueOriginator(RegistryHive hive, RegistryView view, string subKey, string name, IRegistry registry)
            : base(hive, view, subKey, name, registry)
        {
        }
    }
}
