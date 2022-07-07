namespace DevOptimal.SystemStateManager.Registry
{
    internal class RegistryKeyMemento : IMemento
    {
        public bool Exists { get; }

        public RegistryKeyMemento(bool exists)
        {
            Exists = exists;
        }
    }
}
