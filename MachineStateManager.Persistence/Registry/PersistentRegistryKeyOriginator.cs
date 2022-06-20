﻿using bradselw.SystemResources.Registry.Proxy;
using LiteDB;
using bradselw.MachineStateManager.Registry;
using Microsoft.Win32;

namespace bradselw.MachineStateManager.Persistence.Registry
{
    internal class PersistentRegistryKeyOriginator : RegistryKeyOriginator
    {
        [BsonCtor]
        public PersistentRegistryKeyOriginator(RegistryHive hive, RegistryView view, string subKey, BsonDocument registry)
            : this(hive, view, subKey, BsonMapper.Global.ToObject<IRegistryProxy>(registry))
        {
        }

        public PersistentRegistryKeyOriginator(RegistryHive hive, RegistryView view, string subKey, IRegistryProxy registry)
            : base(hive, view, subKey, registry)
        {
        }
    }
}