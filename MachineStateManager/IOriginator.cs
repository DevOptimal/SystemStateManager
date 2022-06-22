namespace bradselw.MachineStateManager
{
    internal interface IOriginator<TMemento>
        where TMemento : IMemento
    {
        string ID { get; }

        TMemento GetState();

        void SetState(TMemento memento);
    }
}
