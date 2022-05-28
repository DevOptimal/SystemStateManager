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

        public string? Value
        {
            get => System.Environment.GetEnvironmentVariable(Name, Target);
            set => System.Environment.SetEnvironmentVariable(Name, value, Target);
        }

        public EnvironmentVariableMemento GetState()
        {
            return new EnvironmentVariableMemento(Value);
        }

        public void SetState(EnvironmentVariableMemento memento)
        {
            Value = memento.Value;
        }
    }
}
