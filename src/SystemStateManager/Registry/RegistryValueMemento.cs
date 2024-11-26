using Microsoft.Win32;

namespace DevOptimal.SystemStateManager.Registry
{
    public class RegistryValueMemento : IMemento
    {
        public object Value { get; set; }

        public RegistryValueKind Kind { get; set; }
    }
}
