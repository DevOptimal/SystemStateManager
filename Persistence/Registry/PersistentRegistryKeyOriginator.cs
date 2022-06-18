using LiteDB;
using MachineStateManager.Core.Registry;
using Microsoft.Win32;
using Registry;

namespace MachineStateManager.Persistence.Registry
{
    internal class PersistentRegistryKeyOriginator : RegistryKeyOriginator
    {
        [BsonCtor]
        public PersistentRegistryKeyOriginator(RegistryHive hive, RegistryView view, string subKey, BsonDocument registry)
            : this(hive, view, subKey, BsonMapper.Global.ToObject<IRegistry>(registry))
        {
        }

        public PersistentRegistryKeyOriginator(RegistryHive hive, RegistryView view, string subKey, IRegistry registry)
            : base(hive, view, subKey, registry)
        {
        }
    }
}
