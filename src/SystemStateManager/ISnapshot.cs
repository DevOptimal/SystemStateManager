using System;

namespace DevOptimal.SystemStateManager
{
    public interface ISnapshot : IDisposable
    {
        string ID { get; }
    }
}
