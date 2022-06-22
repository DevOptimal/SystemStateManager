using bradselw.MachineStateManager.Environment;
using LiteDB;
using System;

namespace bradselw.MachineStateManager.Persistence.Environment
{
    internal class PersistentEnvironmentVariableCaretaker : PersistentCaretaker<PersistentEnvironmentVariableOriginator, EnvironmentVariableMemento>
    {
        public PersistentEnvironmentVariableCaretaker(string id, PersistentEnvironmentVariableOriginator originator)
            : base(id, originator)
        {
        }

        [BsonCtor]
        public PersistentEnvironmentVariableCaretaker(string _id, int processID, DateTime processStartTime, BsonDocument originator, BsonDocument memento)
            : base(_id, processID, processStartTime, BsonMapper.Global.ToObject<PersistentEnvironmentVariableOriginator>(originator), BsonMapper.Global.ToObject<EnvironmentVariableMemento>(memento))
        {
        }
    }
}
