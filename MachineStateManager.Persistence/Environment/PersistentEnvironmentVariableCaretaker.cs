using bradselw.MachineStateManager.Environment;
using bradselw.SystemResources.Environment.Proxy;
using LiteDB;
using System;
using System.Collections.Generic;

namespace bradselw.MachineStateManager.Persistence.Environment
{
    internal class PersistentEnvironmentVariableCaretaker : PersistentCaretaker<PersistentEnvironmentVariableOriginator, EnvironmentVariableMemento>
    {
        public PersistentEnvironmentVariableCaretaker(string name, IEnvironmentProxy environment)
            : this(name, EnvironmentVariableTarget.Process, environment)
        {
        }

        public PersistentEnvironmentVariableCaretaker(string name, EnvironmentVariableTarget target, IEnvironmentProxy environment)
            : this(new PersistentEnvironmentVariableOriginator(name, target, environment))
        {
        }

        public PersistentEnvironmentVariableCaretaker(PersistentEnvironmentVariableOriginator originator)
            : base(originator)
        {
        }

        public PersistentEnvironmentVariableCaretaker(string id, PersistentEnvironmentVariableOriginator originator)
            : base(id, originator)
        {
        }

        [BsonCtor]
        public PersistentEnvironmentVariableCaretaker(string _id, int processID, DateTime processStartTime, BsonDocument originator, BsonDocument memento)
            : base(_id, processID, processStartTime, BsonMapper.Global.ToObject<PersistentEnvironmentVariableOriginator>(originator), BsonMapper.Global.ToObject<EnvironmentVariableMemento>(memento))
        {
        }

        public static IEnumerable<ICaretaker> GetAbandonedCaretakers(Dictionary<int, DateTime?> processes)
            => GetAbandonedCaretakers<PersistentEnvironmentVariableCaretaker>(processes);
    }
}
