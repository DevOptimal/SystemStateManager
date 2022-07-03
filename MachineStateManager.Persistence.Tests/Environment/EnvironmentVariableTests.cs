using bradselw.System.Resources.Environment;
using System;

namespace bradselw.MachineStateManager.Persistence.Tests.Environment
{
    [TestClass]
    public class EnvironmentTests
    {
        private MockEnvironmentProxy proxy;

        private MockMachineStateManager machineStateManager;

        private const string name = "foo";

        private const EnvironmentVariableTarget target = EnvironmentVariableTarget.Machine;

        [TestInitialize]
        public void TestInitializeAttribute()
        {
            proxy = new MockEnvironmentProxy();

            machineStateManager = new MockMachineStateManager(proxy);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            machineStateManager.Dispose();
        }

        [TestMethod]
        public void RevertsEnvironmentVariableCreation()
        {
            using (machineStateManager.SnapshotEnvironmentVariable(name, target))
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

            using (machineStateManager.SnapshotEnvironmentVariable(name, target))
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

            using (machineStateManager.SnapshotEnvironmentVariable(name, target))
            {
                proxy.SetEnvironmentVariable(name, "baz", target);
            }

            Assert.AreEqual(expectedValue, proxy.GetEnvironmentVariable(name, target));
        }
    }
}