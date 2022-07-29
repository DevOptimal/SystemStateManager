using Microsoft.Win32;
using System;

namespace DevOptimal.SystemStateManager.Registry
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

        // The following constructors are provided to aid with deserialization.
        // Logic based on https://docs.microsoft.com/en-us/dotnet/api/microsoft.win32.registrykey.getvalue
        public RegistryValueMemento(string value, RegistryValueKind kind)
        {
            switch (kind)
            {
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                    Value = value;
                    Kind = kind;
                    break;
                default:
                    throw new ArgumentException($"Expected {nameof(value)} to be a string.");
            }
        }

        public RegistryValueMemento(byte[] value, RegistryValueKind kind)
        {
            switch (kind)
            {
                case RegistryValueKind.Binary:
                    Value = value;
                    Kind = kind;
                    break;
                default:
                    throw new ArgumentException($"Expected {nameof(value)} to be a byte array.");
            }
        }

        public RegistryValueMemento(int value, RegistryValueKind kind)
        {
            switch (kind)
            {
                case RegistryValueKind.DWord:
                    Value = value;
                    Kind = kind;
                    break;
                default:
                    throw new ArgumentException($"Expected {nameof(value)} to be an integer.");
            }
        }

        public RegistryValueMemento(long value, RegistryValueKind kind)
        {
            switch (kind)
            {
                case RegistryValueKind.QWord:
                    Value = value;
                    Kind = kind;
                    break;
                default:
                    throw new ArgumentException($"Expected {nameof(value)} to be a long.");
            }
        }

        public RegistryValueMemento(string[] value, RegistryValueKind kind)
        {
            switch (kind)
            {
                case RegistryValueKind.MultiString:
                    Value = value;
                    Kind = kind;
                    break;
                default:
                    throw new ArgumentException($"Expected {nameof(value)} to be a string array.");
            }
        }
    }
}
