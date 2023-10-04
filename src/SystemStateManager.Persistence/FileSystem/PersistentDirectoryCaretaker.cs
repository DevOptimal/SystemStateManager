using DevOptimal.SystemStateManager.FileSystem;
using System;

namespace DevOptimal.SystemStateManager.Persistence.FileSystem
{
    internal class PersistentDirectoryCaretaker : PersistentCaretaker<DirectoryOriginator, DirectoryMemento>
    {
        public PersistentDirectoryCaretaker(string id, DirectoryOriginator originator)
            : base(id, originator)
        {
        }

        public PersistentDirectoryCaretaker(string id, int processID, DateTime processStartTime, DirectoryOriginator originator, DirectoryMemento memento)
            : base(id, processID, processStartTime, originator, memento)
        {
        }
    }
}
