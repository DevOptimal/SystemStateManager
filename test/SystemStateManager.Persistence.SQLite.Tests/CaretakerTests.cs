using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace DevOptimal.SystemStateManager.Persistence.SQLite.Tests
{
    [TestClass]
    public class CaretakerTests : TestBase
    {
        private interface IMemento
        {
        }

        private class EnvironmentVariableMemento : IMemento
        {
            public string Value { get; set; }

            public EnvironmentVariableMemento(string value)
            {
                Value = value;
            }
        }

        private interface IOriginator<TMemento>
            where TMemento : IMemento
        {
        }

        private class EnvironmentVariableOriginator : IOriginator<EnvironmentVariableMemento>
        {
            public string Name { get; }

            public EnvironmentVariableTarget Target { get; }

            public EnvironmentVariableOriginator(string name, EnvironmentVariableTarget target)
            {
                Name = name;
                Target = target;
            }
        }

        private interface ISnapshot
        {
            string ID { get; }
        }

        private class Caretaker<TOriginator, TMemento> : ISnapshot
            where TOriginator : IOriginator<TMemento>
            where TMemento : IMemento
        {
            public string ID { get; }

            public TOriginator Originator { get; }

            public TMemento Memento { get; }

            public Caretaker(string id, TOriginator originator, TMemento memento)
            {
                ID = id;
                Originator = originator;
                Memento = memento;
            }
        }

        private interface IPersistentSnapshot : ISnapshot
        {
            string ProcessID { get; }
        }

        private abstract class PersistentCaretaker<TOriginator, TMemento> : Caretaker<TOriginator, TMemento>, IPersistentSnapshot
            where TOriginator : IOriginator<TMemento>
            where TMemento : IMemento
        {
            public string ProcessID { get; }

            protected PersistentCaretaker(string id, string processID, TOriginator originator, TMemento memento)
                : base(id, originator, memento)
            {
                ProcessID = processID;
            }
        }

        private class PersistentEnvironmentVariableCaretaker : PersistentCaretaker<EnvironmentVariableOriginator, EnvironmentVariableMemento>
        {
            public string Name => Originator.Name;

            public EnvironmentVariableTarget Target => Originator.Target;

            public string Value => Memento.Value;

            public PersistentEnvironmentVariableCaretaker(string id, string processID, EnvironmentVariableOriginator originator, EnvironmentVariableMemento memento)
                : base(id, processID, originator, memento)
            {
            }

            public PersistentEnvironmentVariableCaretaker(string id, string processID, string name, long target, string value)
                : base(id, processID, new EnvironmentVariableOriginator(name, (EnvironmentVariableTarget)target), new EnvironmentVariableMemento(value))
            {
            }
        }

        [TestMethod]
        public void PersistCaretaker()
        {
            var name = "Foo";
            var target = EnvironmentVariableTarget.Process;
            var id = $"[EnvironmentVariable]{target}\\{name}";
            var value = "Bar";
            var processID = 12345;
            var processStartTime = new DateTime(2023, 3, 15, 21, 17, 35, 678);
            var caretaker = new PersistentEnvironmentVariableCaretaker(id, $"{processID}/{processStartTime.Ticks}", new EnvironmentVariableOriginator(name, target), new EnvironmentVariableMemento(value));

            connection.Execute($@"CREATE TABLE IF NOT EXISTS {nameof(PersistentEnvironmentVariableCaretaker)} (
                {nameof(caretaker.ID)} TEXT PRIMARY KEY,
                {nameof(caretaker.ProcessID)} TEXT,
                {nameof(caretaker.Name)} TEXT,
                {nameof(caretaker.Target)} INTEGER,
                {nameof(caretaker.Value)} TEXT
            );");

            connection.Execute($@"INSERT INTO {nameof(PersistentEnvironmentVariableCaretaker)} (
                {nameof(caretaker.ID)},
                {nameof(caretaker.ProcessID)},
                {nameof(caretaker.Name)},
                {nameof(caretaker.Target)},
                {nameof(caretaker.Value)}
            ) VALUES (
                @{nameof(caretaker.ID)},
                @{nameof(caretaker.ProcessID)},
                @{nameof(caretaker.Name)},
                @{nameof(caretaker.Target)},
                @{nameof(caretaker.Value)}
            );", caretaker);

            var caretakers = connection.Query<PersistentEnvironmentVariableCaretaker>($@"SELECT * FROM {nameof(PersistentEnvironmentVariableCaretaker)}").ToList();

            connection.Execute($@"DELETE FROM {nameof(PersistentEnvironmentVariableCaretaker)} WHERE {nameof(caretaker.ID)} = @{nameof(caretaker.ID)};", caretaker);
        }
    }
}
