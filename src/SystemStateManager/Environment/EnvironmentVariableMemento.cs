﻿namespace DevOptimal.SystemStateManager.Environment
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