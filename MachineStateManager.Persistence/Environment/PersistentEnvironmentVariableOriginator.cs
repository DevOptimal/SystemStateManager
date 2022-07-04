using DevOptimal.MachineStateManager.Environment;
using DevOptimal.System.Resources.Environment;
using LiteDB;
using System;

namespace DevOptimal.MachineStateManager.Persistence.Environment
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
