﻿namespace bradselw.MachineStateManager.Environment
{
    internal class EnvironmentVariableMemento : IMemento
    {
        public string Value { get; }

        public EnvironmentVariableMemento(string value)
        {
            Value = value;
        }
    }
}