using DevOptimal.SystemStateManager.Environment;
using System;

namespace DevOptimal.SystemStateManager.Persistence.Environment
{
    internal class PersistentEnvironmentVariableCaretaker : PersistentCaretaker<EnvironmentVariableOriginator, EnvironmentVariableMemento>
    {
        internal PersistentEnvironmentVariableCaretaker()
        { }

        public PersistentEnvironmentVariableCaretaker(string id, EnvironmentVariableOriginator originator)
            : base(id, originator)
        {
        }

        public PersistentEnvironmentVariableCaretaker(string id, long processID, DateTime processStartTime, EnvironmentVariableOriginator originator, EnvironmentVariableMemento memento)
            : base(id, (int)processID, processStartTime, originator, memento)
        {
        }
    }
}
