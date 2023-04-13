using DevOptimal.SystemStateManager.Persistence;
using System;
using System.Collections.Generic;
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
