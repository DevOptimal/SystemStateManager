using DevOptimal.SystemUtilities.Environment;
using System;

namespace DevOptimal.SystemStateManager.Tests.Environment
{
    [TestClass]
    public class EnvironmentTests
    {
        private MockEnvironmentProxy proxy;

        private MockSystemStateManager systemStateManager;

        private const string name = "foo";

        private const EnvironmentVariableTarget target = EnvironmentVariableTarget.Machine;

        [TestInitialize]
        public void TestInitializeAttribute()
        {
            proxy = new MockEnvironmentProxy();

            systemStateManager = new MockSystemStateManager(proxy);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            systemStateManager.Dispose();
        }

        [TestMethod]
        public void RevertsEnvironmentVariableCreation()
        {
            using (systemStateManager.SnapshotEnvironmentVariable(name, target))
            {
                proxy.SetEnvironmentVariable(name, "bar", target);
            }

            Assert.AreEqual(null, proxy.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void RevertsEnvironmentVariableDeletion()
        {
            var expectedValue = "bar";
            proxy.SetEnvironmentVariable(name, expectedValue, target);

            using (systemStateManager.SnapshotEnvironmentVariable(name, target))
            {
                proxy.SetEnvironmentVariable(name, null, target);
            }

            Assert.AreEqual(expectedValue, proxy.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void RevertsEnvironmentVariableAlteration()
        {
            var expectedValue = "bar";
            proxy.SetEnvironmentVariable(name, expectedValue, target);

            using (systemStateManager.SnapshotEnvironmentVariable(name, target))
            {
                proxy.SetEnvironmentVariable(name, "baz", target);
            }

            Assert.AreEqual(expectedValue, proxy.GetEnvironmentVariable(name, target));
        }
    }
}