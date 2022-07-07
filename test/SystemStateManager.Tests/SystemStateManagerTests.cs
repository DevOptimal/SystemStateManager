using DevOptimal.SystemUtilities.Environment;
using System;

namespace DevOptimal.SystemStateManager.Tests
{
    [TestClass]
    public class SystemStateManagerTests
    {
        private MockEnvironment environment;

        private MockSystemStateManager systemStateManager;

        private const string name = "foo";

        private const EnvironmentVariableTarget target = EnvironmentVariableTarget.Machine;

        private const string expectedValue = "bar";

        [TestInitialize]
        public void TestInitializeAttribute()
        {
            environment = new MockEnvironment();

            environment.SetEnvironmentVariable(name, expectedValue, target);

            systemStateManager = new MockSystemStateManager(environment);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            systemStateManager.Dispose();
        }

        [TestMethod]
        public void SystemStateManagerCorrectlyDisposes()
        {
            using var snapshot = systemStateManager.SnapshotEnvironmentVariable(name, target);

            environment.SetEnvironmentVariable(name, null, target);

            systemStateManager.Dispose();

            Assert.AreEqual(expectedValue, environment.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void SnapshotCorrectlyDisposes()
        {
            using var snapshot = systemStateManager.SnapshotEnvironmentVariable(name, target);

            environment.SetEnvironmentVariable(name, null, target);

            snapshot.Dispose();

            Assert.AreEqual(expectedValue, environment.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void ReuseExistingSnapshot()
        {
            using (var snapshot1 = systemStateManager.SnapshotEnvironmentVariable(name, target))
            using (var snapshot2 = systemStateManager.SnapshotEnvironmentVariable(name, target))
            {
                environment.SetEnvironmentVariable(name, null, target);

                Assert.IsTrue(ReferenceEquals(snapshot1, snapshot2));
            }

            Assert.AreEqual(expectedValue, environment.GetEnvironmentVariable(name, target));
        }
    }
}
