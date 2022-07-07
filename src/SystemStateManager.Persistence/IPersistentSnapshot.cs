using System;

namespace DevOptimal.SystemStateManager.Persistence
{
    internal interface IPersistentSnapshot : ISnapshot
    {
        int ProcessID { get; }

        DateTime ProcessStartTime { get; }
    }
}
