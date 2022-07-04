using System;

namespace DevOptimal.MachineStateManager
{
    public interface ICaretaker : IDisposable
    {
        string ID { get; }
    }
}
