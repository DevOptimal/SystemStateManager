using System;

namespace DevOptimal.MachineStateManager.Persistence
{
    internal interface IPersistentCaretaker : ICaretaker
    {
        int ProcessID { get; }

        DateTime ProcessStartTime { get; }
    }
}
