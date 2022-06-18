using Environment;
using LiteDB;
using MachineStateManager.Core.Environment;
using System;

namespace MachineStateManager.Persistence.Environment
{
    internal class PersistentEnvironmentVariableOriginator : EnvironmentVariableOriginator
    {
        [BsonCtor]
        public PersistentEnvironmentVariableOriginator(string name, EnvironmentVariableTarget target, BsonDocument environment)
            : this(name, target, BsonMapper.Global.ToObject<IEnvironment>(environment))
        {
        }

        public PersistentEnvironmentVariableOriginator(string name, EnvironmentVariableTarget target, IEnvironment environment)
            : base(name, target, environment)
        {
        }
    }
}
