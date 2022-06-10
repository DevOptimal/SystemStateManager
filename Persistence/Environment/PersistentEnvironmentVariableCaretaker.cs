using LiteDB;
using MachineStateManager.Core.Environment;
using System.Diagnostics;

namespace MachineStateManager.Persistence.Environment
{
    internal class PersistentEnvironmentVariableCaretaker : PersistentCaretaker<EnvironmentVariableOriginator, EnvironmentVariableMemento>
    {
        public PersistentEnvironmentVariableCaretaker(string name, LiteDatabase database)
            : this(new EnvironmentVariableOriginator(name), database)
        {
        }

        public PersistentEnvironmentVariableCaretaker(string name, EnvironmentVariableTarget target, LiteDatabase database)
            : this(new EnvironmentVariableOriginator(name, target), database)
        {
        }

        public PersistentEnvironmentVariableCaretaker(EnvironmentVariableOriginator originator, LiteDatabase database)
            : base(GetID(originator), originator, database)
        {
        }

        public PersistentEnvironmentVariableCaretaker(string id, int processID, DateTime processStartTime, EnvironmentVariableOriginator originator, EnvironmentVariableMemento memento, LiteDatabase database)
            : base(id, processID, processStartTime, originator, memento, database)
        {
        }

        public static void RegisterType(LiteDatabase database)
        {
            var currentProcess = Process.GetCurrentProcess();

            database.Mapper.RegisterType(
                serialize: (caretaker) =>
                {
                    var dictionary = new Dictionary<string, BsonValue>
                    {
                        ["_id"] = caretaker.ID,
                        [nameof(Process) + nameof(Process.Id)] = currentProcess.Id,
                        [nameof(Process) + nameof(Process.StartTime)] = currentProcess.StartTime,
                        [nameof(Originator)] = database.Mapper.ToDocument(caretaker.Originator),
                        [nameof(Memento)] = database.Mapper.ToDocument(caretaker.Memento),
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
                    return new PersistentEnvironmentVariableCaretaker(bson["_id"].AsString, bson[nameof(ProcessID)].AsInt32, bson[nameof(ProcessStartTime)].AsDateTime, originator, memento, database);
                }
            );
        }

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
