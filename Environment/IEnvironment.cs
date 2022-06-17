using System;

namespace Environment
{
    public interface IEnvironment
    {
        string GetEnvironmentVariable(string name, EnvironmentVariableTarget target);

        void SetEnvironmentVariable(string name, string value, EnvironmentVariableTarget target);
    }
}
