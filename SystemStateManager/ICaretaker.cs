using System;

namespace DevOptimal.SystemStateManager
{
    public interface ICaretaker : IDisposable
    {
        string ID { get; }
    }
}
