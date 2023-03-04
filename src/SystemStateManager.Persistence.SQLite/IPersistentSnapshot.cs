using System;

namespace DevOptimal.SystemStateManager.Persistence.SQLite
{
    internal interface IPersistentSnapshot : ISnapshot
    {
        int ProcessID { get; }

        DateTime ProcessStartTime { get; }
    }
}
