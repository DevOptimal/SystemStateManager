using DevOptimal.SystemStateManager.Registry;
using System;

namespace DevOptimal.SystemStateManager.Persistence.Registry
{
    internal class PersistentRegistryKeyCaretaker : PersistentCaretaker<RegistryKeyOriginator, RegistryKeyMemento>
    {
        public PersistentRegistryKeyCaretaker(string id, RegistryKeyOriginator originator)
            : base(id, originator)
        {
        }

        public PersistentRegistryKeyCaretaker(string id, int processID, DateTime processStartTime, RegistryKeyOriginator originator, RegistryKeyMemento memento)
            : base(id, processID, processStartTime, originator, memento)
        {
        }
    }
}
