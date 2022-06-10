using MachineStateManager.Core;

namespace MachineStateManager.Persistence
{
    internal interface IPersistentCaretaker : ICaretaker
    {
        string ID { get; }

        int ProcessID { get; }

        DateTime ProcessStartTime { get; }
    }
}
