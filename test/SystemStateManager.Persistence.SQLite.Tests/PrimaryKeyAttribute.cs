using System;

namespace DevOptimal.SystemStateManager.Persistence.SQLite.Tests
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class PrimaryKeyAttribute : Attribute
    {
    }
}
