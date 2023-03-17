using System;

namespace DevOptimal.SystemStateManager.Persistence.SQLite
{
    internal interface IPersistentSnapshot : ISnapshot
    {
        string ProcessID { get; }
    }
}
