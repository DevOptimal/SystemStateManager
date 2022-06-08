﻿namespace MachineStateManager.Core
{
    internal interface IOriginator<TMemento>
        where TMemento : IMemento
    {
        TMemento GetState();

        void SetState(TMemento memento);
    }
}
