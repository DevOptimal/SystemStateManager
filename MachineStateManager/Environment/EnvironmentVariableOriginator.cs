using bradselw.SystemResources.Environment.Proxy;
using System;
using System.Runtime.InteropServices;

namespace bradselw.MachineStateManager.Environment
{
    internal class EnvironmentVariableOriginator : IOriginator<EnvironmentVariableMemento>
    {
        public string ID
        {
            get
            {
                var id = string.Join("\\", Target, Name);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    id = id.ToLower();
                }

                return id;
            }
        }

        public string Name { get; }

        public EnvironmentVariableTarget Target { get; }

        public IEnvironmentProxy Environment { get; }

        public EnvironmentVariableOriginator(string name, EnvironmentVariableTarget target, IEnvironmentProxy environment)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Target = target;
            Environment = environment;
        }

        public EnvironmentVariableMemento GetState()
        {
            return new EnvironmentVariableMemento(Environment.GetEnvironmentVariable(Name, Target));
        }

        public void SetState(EnvironmentVariableMemento memento)
        {
            Environment.SetEnvironmentVariable(Name, memento.Value, Target);
        }
    }
}
