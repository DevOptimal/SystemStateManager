using LiteDB;
using MachineStateManager.Core.Environment;
using System.Diagnostics;

namespace MachineStateManager.Persistence.Environment
{
    internal class PersistentEnvironmentVariableCaretaker : PersistentCaretaker<EnvironmentVariableOriginator, EnvironmentVariableMemento>
    {
        static PersistentEnvironmentVariableCaretaker()
        {
            var currentProcess = Process.GetCurrentProcess();

            BsonMapper.Global.RegisterType(
                serialize: (caretaker) =>
                {
                    var dictionary = new Dictionary<string, BsonValue>
                    {
                        ["_id"] = caretaker.ID,
                        [nameof(Process) + nameof(Process.Id)] = currentProcess.Id,
                        [nameof(Process) + nameof(Process.StartTime)] = currentProcess.StartTime,
                        [nameof(Originator)] = BsonMapper.Global.ToDocument(caretaker.Originator),
                        [nameof(Memento)] = BsonMapper.Global.ToDocument(caretaker.Memento),
                    };
                    return new BsonDocument(dictionary);
                },
                deserialize: (bson) =>
                {
                    var originator = new EnvironmentVariableOriginator(
                        bson[nameof(Originator)][nameof(EnvironmentVariableOriginator.Name)].AsString,
                        Enum.Parse<EnvironmentVariableTarget>(bson[nameof(Originator)][nameof(EnvironmentVariableOriginator.Target)].AsString));
                    var memento = new EnvironmentVariableMemento(
                        bson[nameof(Memento)][nameof(EnvironmentVariableMemento.Value)].AsString);
                    return new PersistentEnvironmentVariableCaretaker(bson["_id"].AsString, bson[nameof(ProcessID)].AsInt32, bson[nameof(ProcessStartTime)].AsDateTime, originator, memento);
                }
            );
        }

        public PersistentEnvironmentVariableCaretaker(string name)
            : this(new EnvironmentVariableOriginator(name))
        {
        }

        public PersistentEnvironmentVariableCaretaker(string name, EnvironmentVariableTarget target)
            : this(new EnvironmentVariableOriginator(name, target))
        {
        }

        public PersistentEnvironmentVariableCaretaker(EnvironmentVariableOriginator originator)
            : base(GetID(originator), originator)
        {
        }

        public PersistentEnvironmentVariableCaretaker(string id, int processID, DateTime processStartTime, EnvironmentVariableOriginator originator, EnvironmentVariableMemento memento)
            : base(id, processID, processStartTime, originator, memento)
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

            var id = string.Join('\\', originator.Target, originator.Name);

            if (OperatingSystem.IsWindows())
            {
                id = id.ToLower();
            }

            return id;
        }
    }
}
