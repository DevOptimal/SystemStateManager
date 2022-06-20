using bradselw.SystemResources.FileSystem.Proxy;
using LiteDB;
using bradselw.MachineStateManager.FileSystem;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace bradselw.MachineStateManager.Persistence.FileSystem
{
    internal class PersistentDirectoryCaretaker : PersistentCaretaker<PersistentDirectoryOriginator, DirectoryMemento>
    {
        public PersistentDirectoryCaretaker(string path, IFileSystemProxy fileSystem)
            : this(new PersistentDirectoryOriginator(path, fileSystem))
        {
        }

        public PersistentDirectoryCaretaker(PersistentDirectoryOriginator originator)
            : base(GetID(originator), originator)
        {
        }

        [BsonCtor]
        public PersistentDirectoryCaretaker(string _id, int processID, DateTime processStartTime, BsonDocument originator, BsonDocument memento)
            : base(_id, processID, processStartTime, BsonMapper.Global.ToObject<PersistentDirectoryOriginator>(originator), BsonMapper.Global.ToObject<DirectoryMemento>(memento))
        {
        }

        public static IEnumerable<IDisposable> GetAbandonedCaretakers(Dictionary<int, DateTime?> processes)
            => GetAbandonedCaretakers<PersistentDirectoryCaretaker>(processes);

        private static string GetID(DirectoryOriginator originator)
        {
            if (originator == null)
            {
                throw new ArgumentNullException(nameof(originator));
            }

            var id = originator.Path;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                id = id.ToLower();
            }

            return id;
        }
    }
}
