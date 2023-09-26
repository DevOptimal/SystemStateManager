using System;
using System.Collections.Generic;
using System.Text;

namespace DevOptimal.SystemStateManager.Persistence
{
    internal static class Database
    {
        public static void Persist<T>(T snapshot)
            where T : IPersistentSnapshot
        {

        }

        public static T Unpersist<T>(string id)
            where T : IPersistentSnapshot
        {

        }
    }
}
