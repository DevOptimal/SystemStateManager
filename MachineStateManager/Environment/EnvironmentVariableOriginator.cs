﻿using bradselw.System.Resources.Environment;
using System;

namespace bradselw.MachineStateManager.Environment
{
    internal class EnvironmentVariableOriginator : IOriginator<EnvironmentVariableMemento>
    {
        public string Name { get; }

        public EnvironmentVariableTarget Target { get; }

        public IEnvironmentProxy Environment { get; }

        public EnvironmentVariableOriginator(string name, EnvironmentVariableTarget target, IEnvironmentProxy environment)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Target = target;
            Environment = environment ?? throw new ArgumentNullException(nameof(environment));
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
