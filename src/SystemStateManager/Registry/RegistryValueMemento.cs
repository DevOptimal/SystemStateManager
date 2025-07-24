using Microsoft.Win32;

namespace DevOptimal.SystemStateManager.Registry
{
    internal class RegistryValueMemento : IMemento
    {
        public object Value { get; set; }

        public RegistryValueKind Kind { get; set; }
    }
}
