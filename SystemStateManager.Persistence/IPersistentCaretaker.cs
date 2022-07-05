using System;

namespace DevOptimal.SystemStateManager.Persistence
{
    internal interface IPersistentCaretaker : ICaretaker
    {
        int ProcessID { get; }

        DateTime ProcessStartTime { get; }
    }
}
