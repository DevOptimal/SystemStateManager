﻿using bradselw.MachineStateManager.Environment;
using bradselw.SystemResources.Environment.Proxy;
using LiteDB;
using System;

namespace bradselw.MachineStateManager.Persistence.Environment
{
    internal class PersistentEnvironmentVariableOriginator : EnvironmentVariableOriginator
    {
        [BsonCtor]
        public PersistentEnvironmentVariableOriginator(string name, EnvironmentVariableTarget target, BsonDocument environment)
            : this(name, target, BsonMapper.Global.ToObject<IEnvironmentProxy>(environment))
        {
        }

        public PersistentEnvironmentVariableOriginator(string name, EnvironmentVariableTarget target, IEnvironmentProxy environment)
            : base(name, target, environment)
        {
        }
    }
}