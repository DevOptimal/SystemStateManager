using FileSystem;
using LiteDB;
using MachineStateManager.Core.FileSystem;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MachineStateManager.Persistence.FileSystem
{
    internal class PersistentDirectoryCaretaker : PersistentCaretaker<DirectoryOriginator, DirectoryMemento>
    {
        static PersistentDirectoryCaretaker()
        {
            BsonMapper.Global.RegisterType(SerializeOriginator, DeserializeOriginator);
        }

        public PersistentDirectoryCaretaker(string path, IFileSystem fileSystem)
            : this(new DirectoryOriginator(path, fileSystem))
        {
        }

        public PersistentDirectoryCaretaker(DirectoryOriginator originator)
            : base(GetID(originator), originator)
        {
        }

        [BsonCtor]
        public PersistentDirectoryCaretaker(string _id, int processID, DateTime processStartTime, BsonDocument originator, BsonDocument memento)
            : base(_id, processID, processStartTime, BsonMapper.Global.ToObject<DirectoryOriginator>(originator), BsonMapper.Global.ToObject<DirectoryMemento>(memento))
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

        private static BsonValue SerializeOriginator(DirectoryOriginator originator)
        {
            return new BsonDocument
            {
                [nameof(DirectoryOriginator.Path)] = originator.Path,
                [nameof(DirectoryOriginator.FileSystem)] = BsonMapper.Global.ToDocument(originator.FileSystem),
            };
        }

        private static DirectoryOriginator DeserializeOriginator(BsonValue bson)
        {
            return new DirectoryOriginator(
                path: bson[nameof(DirectoryOriginator.Path)],
                fileSystem: BsonMapper.Global.ToObject<IFileSystem>(bson[nameof(DirectoryOriginator.FileSystem)].AsDocument));
        }
    }
}
