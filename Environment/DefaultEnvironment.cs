using System;

namespace Environment
{
    public class DefaultEnvironment : IEnvironment
    {
        public string GetEnvironmentVariable(string name, EnvironmentVariableTarget target)
        {
            return System.Environment.GetEnvironmentVariable(name, target);
        }

        public void SetEnvironmentVariable(string name, string value, EnvironmentVariableTarget target)
        {
            System.Environment.SetEnvironmentVariable(name, value, target);
        }
    }
}
