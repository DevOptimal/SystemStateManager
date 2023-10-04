using DevOptimal.SystemStateManager.Registry;
using System;

namespace DevOptimal.SystemStateManager.Persistence.Registry
{
    internal class PersistentRegistryValueCaretaker : PersistentCaretaker<RegistryValueOriginator, RegistryValueMemento>
    {
        public PersistentRegistryValueCaretaker(string id, RegistryValueOriginator originator)
            : base(id, originator)
        {
        }

        public PersistentRegistryValueCaretaker(string id, int processID, DateTime processStartTime, RegistryValueOriginator originator, RegistryValueMemento memento)
            : base(id, processID, processStartTime, originator, memento)
        {
        }
    }
}
