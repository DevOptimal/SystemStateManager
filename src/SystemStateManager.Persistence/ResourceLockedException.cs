using System;

namespace DevOptimal.SystemStateManager.Persistence
{
    public class ResourceLockedException : Exception
    {
        public ResourceLockedException()
        {
        }

        public ResourceLockedException(string message)
            : base(message)
        {
        }

        public ResourceLockedException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
