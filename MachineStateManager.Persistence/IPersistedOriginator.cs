namespace MachineStateManager.Persistence
{
    internal interface IPersistedOriginator<TMemento> : IOriginator<TMemento>
        where TMemento : IMemento
    {
        string ID { get; }
    }
}
