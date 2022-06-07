namespace MachineStateManager.Environment
{
    internal class EnvironmentVariableOriginator : IOriginator<EnvironmentVariableMemento>
    {
        public string Name { get; }

        public EnvironmentVariableTarget Target { get; }

        public EnvironmentVariableOriginator(string name)
            : this(name, EnvironmentVariableTarget.Process)
        { }

        public EnvironmentVariableOriginator(string name, EnvironmentVariableTarget target)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Target = target;
        }

        public EnvironmentVariableMemento GetState()
        {
            return new EnvironmentVariableMemento(System.Environment.GetEnvironmentVariable(Name, Target));
        }

        public void SetState(EnvironmentVariableMemento memento)
        {
            System.Environment.SetEnvironmentVariable(Name, memento.Value, Target);
        }
    }
}
