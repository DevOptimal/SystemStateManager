using DevOptimal.SystemStateManager.FileSystem;
using System;

namespace DevOptimal.SystemStateManager.Persistence.FileSystem
{
    internal class PersistentFileCaretaker : PersistentCaretaker<FileOriginator, FileMemento>
    {
        public PersistentFileCaretaker(string id, FileOriginator originator)
            : base(id, originator)
        {
        }

        public PersistentFileCaretaker(string id, int processID, DateTime processStartTime, FileOriginator originator, FileMemento memento)
            : base(id, processID, processStartTime, originator, memento)
        {
        }
    }
}
