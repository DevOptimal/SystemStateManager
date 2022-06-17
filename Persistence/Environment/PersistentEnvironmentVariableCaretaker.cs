using Environment;
using LiteDB;
using MachineStateManager.Core.Environment;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MachineStateManager.Persistence.Environment
{
    internal class PersistentEnvironmentVariableCaretaker : PersistentCaretaker<EnvironmentVariableOriginator, EnvironmentVariableMemento>
    {
        static PersistentEnvironmentVariableCaretaker()
        {
            BsonMapper.Global.RegisterType(SerializeOriginator, DeserializeOriginator);
        }

        public PersistentEnvironmentVariableCaretaker(string name, IEnvironment environment)
            : this(name, EnvironmentVariableTarget.Process, environment)
        {
        }

        public PersistentEnvironmentVariableCaretaker(string name, EnvironmentVariableTarget target, IEnvironment environment)
            : this(new EnvironmentVariableOriginator(name, target, environment))
        {
        }

        public PersistentEnvironmentVariableCaretaker(EnvironmentVariableOriginator originator)
            : base(GetID(originator), originator)
        {
        }

        [BsonCtor]
        public PersistentEnvironmentVariableCaretaker(string _id, int processID, DateTime processStartTime, BsonDocument originator, BsonDocument memento)
            : base(_id, processID, processStartTime, BsonMapper.Global.ToObject<EnvironmentVariableOriginator>(originator), BsonMapper.Global.ToObject<EnvironmentVariableMemento>(memento))
        {
        }

        public static IEnumerable<IDisposable> GetAbandonedCaretakers(Dictionary<int, DateTime?> processes)
            => GetAbandonedCaretakers<PersistentEnvironmentVariableCaretaker>(processes);

        private static string GetID(EnvironmentVariableOriginator originator)
        {
            if (originator == null)
            {
                throw new ArgumentNullException(nameof(originator));
            }

            var id = string.Join("\\", originator.Target, originator.Name);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                id = id.ToLower();
            }

            return id;
        }

        private static BsonValue SerializeOriginator(EnvironmentVariableOriginator originator)
        {
            return new BsonDocument
            {
                [nameof(EnvironmentVariableOriginator.Name)] = originator.Name,
                [nameof(EnvironmentVariableOriginator.Target)] = originator.Target.ToString(),
                [nameof(EnvironmentVariableOriginator.Environment)] = BsonMapper.Global.ToDocument(originator.Environment),
            };
        }

        private static EnvironmentVariableOriginator DeserializeOriginator(BsonValue bson)
        {
            return new EnvironmentVariableOriginator(
                name: bson[nameof(EnvironmentVariableOriginator.Name)],
                target: (EnvironmentVariableTarget)Enum.Parse(typeof(EnvironmentVariableTarget), bson[nameof(EnvironmentVariableOriginator.Target)]),
                environment: BsonMapper.Global.ToObject<IEnvironment>(bson[nameof(EnvironmentVariableOriginator.Environment)].AsDocument));
        }
    }
}
