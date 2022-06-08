using System.Runtime.Versioning;

namespace MachineStateManager.Core.Registry
{
    [SupportedOSPlatform("windows")]
    internal class RegistryKeyMemento : IMemento
    {
        public bool Exists { get; }

        public RegistryKeyMemento(bool exists)
        {
            Exists = exists;
        }
    }
}
