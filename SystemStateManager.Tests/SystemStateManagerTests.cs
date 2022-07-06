using DevOptimal.SystemUtilities.Environment;
using System;

namespace DevOptimal.SystemStateManager.Tests
{
    [TestClass]
    public class SystemStateManagerTests
    {
        private MockEnvironmentProxy proxy;

        private MockSystemStateManager systemStateManager;

        private const string name = "foo";

        private const EnvironmentVariableTarget target = EnvironmentVariableTarget.Machine;

        private const string expectedValue = "bar";

        [TestInitialize]
        public void TestInitializeAttribute()
        {
            proxy = new MockEnvironmentProxy();

            proxy.SetEnvironmentVariable(name, expectedValue, target);

            systemStateManager = new MockSystemStateManager(proxy);
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

            proxy.SetEnvironmentVariable(name, null, target);

            systemStateManager.Dispose();

            Assert.AreEqual(expectedValue, proxy.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void SnapshotCorrectlyDisposes()
        {
            using var snapshot = systemStateManager.SnapshotEnvironmentVariable(name, target);

            proxy.SetEnvironmentVariable(name, null, target);

            snapshot.Dispose();

            Assert.AreEqual(expectedValue, proxy.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void ReuseExistingSnapshot()
        {
            using (var snapshot1 = systemStateManager.SnapshotEnvironmentVariable(name, target))
            using (var snapshot2 = systemStateManager.SnapshotEnvironmentVariable(name, target))
            {
                proxy.SetEnvironmentVariable(name, null, target);

                Assert.IsTrue(ReferenceEquals(snapshot1, snapshot2));
            }

            Assert.AreEqual(expectedValue, proxy.GetEnvironmentVariable(name, target));
        }
    }
}
