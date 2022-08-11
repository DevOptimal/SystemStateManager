using DevOptimal.SystemStateManager.Environment;
using DevOptimal.SystemUtilities.Environment;
using LiteDB;
using System;

namespace DevOptimal.SystemStateManager.Persistence.Environment
{
    internal class PersistentEnvironmentVariableOriginator : EnvironmentVariableOriginator
    {
        public PersistentEnvironmentVariableOriginator(string name, EnvironmentVariableTarget target, IEnvironment environment)
            : base(name, target, environment)
        {
        }

        [BsonCtor]
        public PersistentEnvironmentVariableOriginator(string name, EnvironmentVariableTarget target, BsonDocument environment)
            : this(name, target, LiteDatabaseFactory.Mapper.ToObject<IEnvironment>(environment))
        {
        }
    }
}
