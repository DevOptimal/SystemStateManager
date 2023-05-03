using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DevOptimal.SystemStateManager.Persistence.Tests
{
    [TestClass]
    public class PersistentSystemStateManagerTests : TestBase
    {
        [TestMethod]
        public void DoesNotRestoreSnapshotsFromCurrentProcess()
        {
            var name = "foo";
            var target = EnvironmentVariableTarget.Machine;
            var expectedValue = "bar";

            using var systemStateManager = CreatePersistentSystemStateManager();

            systemStateManager.SnapshotEnvironmentVariable(name, target);

            environment.SetEnvironmentVariable(name, null, target);

            PersistentSystemStateManager.RestoreAbandonedSnapshots(environment, fileSystem, registry);
            Assert.AreNotEqual(expectedValue, environment.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void SnapshotIsThreadSafe()
        {
            using var systemStateManager = CreatePersistentSystemStateManager();

            var target = EnvironmentVariableTarget.Machine;
            var expectedValue = "bar";

            var tasks = new List<Task>();

            for (var i = 0; i < 100; i++)
            {
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    var name = Guid.NewGuid().ToString();
                    using (systemStateManager.SnapshotEnvironmentVariable(name, target))
                    {
                        environment.SetEnvironmentVariable(name, expectedValue, target);
                        Assert.AreEqual(expectedValue, environment.GetEnvironmentVariable(name, target));
                    }
                    Assert.AreEqual(null, environment.GetEnvironmentVariable(name, target));
                }));
            }

            Task.WaitAll(tasks.ToArray());
        }

        [TestMethod]
        public void ConcurrentManagersCanSnapshotEnvironmentVariables()
        {
            var concurrentThreads = 100;

            var names = new string[concurrentThreads];
            var expectedValues = new string[concurrentThreads];
            var target = EnvironmentVariableTarget.Machine;

            for (var i = 0; i < concurrentThreads; i++)
            {
                var name = $"variable{i}";
                var value = Guid.NewGuid().ToString();
                environment.SetEnvironmentVariable(name, value, target);
                names[i] = name;
                expectedValues[i] = value;
            }

            Parallel.For(0, concurrentThreads, i =>
            {
                var name = names[i];
                var value = expectedValues[i];
                using var manager = CreatePersistentSystemStateManager();
                using (var caretaker = manager.SnapshotEnvironmentVariable(name, target))
                {
                    environment.SetEnvironmentVariable(name, null, target);
                    Assert.AreEqual(null, environment.GetEnvironmentVariable(name, target));
                }

                Assert.AreEqual(expectedValues[i], environment.GetEnvironmentVariable(name, target));
            });
        }

        [TestMethod]
        public void ConcurrentManagersCanSnapshotFiles()
        {
            var concurrentThreads = 100;

            var filePaths = new string[concurrentThreads];
            var expectedContent = new byte[concurrentThreads][];

            for (var i = 0; i < concurrentThreads; i++)
            {
                var file = $@"C:\file{i}.txt";
                fileSystem.CreateFile(file);
                filePaths[i] = file;

                var content = Guid.NewGuid().ToByteArray();
                using var stream = fileSystem.OpenFile(file, FileMode.Open, FileAccess.Write, FileShare.None);
                stream.Write(content, 0, content.Length);
                expectedContent[i] = content;
            }

            Parallel.For(0, concurrentThreads, i =>
            {
                var file = filePaths[i];
                using var manager = CreatePersistentSystemStateManager();
                using (var caretaker = manager.SnapshotFile(file))
                {
                    fileSystem.DeleteFile(file);
                    Assert.IsFalse(fileSystem.FileExists(file));
                }

                Assert.IsTrue(fileSystem.FileExists(file));
                var content = expectedContent[i];
                using var stream = fileSystem.OpenFile(file, FileMode.Open, FileAccess.Read, FileShare.None);
                var readContent = new byte[content.Length];
                stream.Read(readContent, 0, readContent.Length);
                Assert.IsTrue(readContent.SequenceEqual(content));
            });
        }

        [TestMethod]
        [ExpectedException(typeof(ResourceLockedException))]
        public void ThrowsWhenSnapshotLockedResource()
        {
            var name = "foo";
            var target = EnvironmentVariableTarget.Machine;

            using var systemStateManager1 = CreatePersistentSystemStateManager();
            using var systemStateManager2 = CreatePersistentSystemStateManager();

            systemStateManager1.SnapshotEnvironmentVariable(name, target);
            systemStateManager2.SnapshotEnvironmentVariable(name, target);
        }
    }
}
