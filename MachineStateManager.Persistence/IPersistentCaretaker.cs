using System;

namespace bradselw.MachineStateManager.Persistence
{
    internal interface IPersistentCaretaker : ICaretaker
    {
        int ProcessID { get; }

        DateTime ProcessStartTime { get; }
    }
}
