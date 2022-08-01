using DevOptimal.SystemStateManager.Registry;
using DevOptimal.SystemUtilities.Registry;
using LiteDB;
using Microsoft.Win32;

namespace DevOptimal.SystemStateManager.Persistence.Registry
{
    internal class PersistentRegistryKeyOriginator : RegistryKeyOriginator
    {
        public PersistentRegistryKeyOriginator(RegistryHive hive, RegistryView view, string subKey, IRegistry registry)
            : base(hive, view, subKey, registry)
        {
        }

        [BsonCtor]
        public PersistentRegistryKeyOriginator(RegistryHive hive, RegistryView view, string subKey, BsonDocument registry)
            : this(hive, view, subKey, LiteDatabaseFactory.Mapper.ToObject<IRegistry>(registry))
        {
        }
    }
}
