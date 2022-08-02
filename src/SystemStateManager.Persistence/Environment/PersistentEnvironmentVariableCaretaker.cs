using DevOptimal.SystemStateManager.Environment;
using LiteDB;
using System;

namespace DevOptimal.SystemStateManager.Persistence.Environment
{
    internal class PersistentEnvironmentVariableCaretaker : PersistentCaretaker<PersistentEnvironmentVariableOriginator, EnvironmentVariableMemento>
    {
        public PersistentEnvironmentVariableCaretaker(string id, PersistentEnvironmentVariableOriginator originator)
            : base(id, originator)
        {
        }

        [BsonCtor]
        public PersistentEnvironmentVariableCaretaker(string _id, int processID, DateTime processStartTime, BsonDocument originator, BsonDocument memento)
            : base(_id, processID, processStartTime, LiteDatabaseFactory.Mapper.ToObject<PersistentEnvironmentVariableOriginator>(originator), LiteDatabaseFactory.Mapper.ToObject<EnvironmentVariableMemento>(memento))
        {
        }
    }
}
