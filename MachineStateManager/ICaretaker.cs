using System;

namespace bradselw.MachineStateManager
{
    public interface ICaretaker : IDisposable
    {
        string ID { get; }
    }
}
