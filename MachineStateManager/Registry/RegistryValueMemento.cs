﻿using Microsoft.Win32;

namespace bradselw.MachineStateManager.Registry
{
    internal class RegistryValueMemento : IMemento
    {
        public object Value { get; }

        public RegistryValueKind Kind { get; }

        public RegistryValueMemento(object value, RegistryValueKind kind)
        {
            Value = value;
            Kind = kind;
        }
    }
}