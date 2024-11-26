using System;

namespace DevOptimal.SystemStateManager
{
    public interface ISnapshot : IDisposable
    {
        string ID { get; }

        int ProcessID { get; }

        DateTime ProcessStartTime { get; }
    }
}
