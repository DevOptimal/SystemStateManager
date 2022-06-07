using MachineStateManager.Environment;

namespace MachineStateManager.Persistence.Environment
{
    internal class PersistedEnvironmentVariableOriginator : EnvironmentVariableOriginator, IPersistedOriginator<EnvironmentVariableMemento>
    {
        public string ID => $"{Target}={Name}";

        public PersistedEnvironmentVariableOriginator(string name) : base(name)
        {
        }

        public PersistedEnvironmentVariableOriginator(string name, EnvironmentVariableTarget target) : base(name, target)
        {
        }
    }
}
