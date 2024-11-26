namespace DevOptimal.SystemStateManager
{
    public interface IOriginator<TMemento>
        where TMemento : IMemento
    {
        TMemento GetState();

        void SetState(TMemento memento);
    }
}
