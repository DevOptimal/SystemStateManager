using DevOptimal.System.Resources.Environment;
using System;
using System.IO;

namespace DevOptimal.MachineStateManager.Tests
{
    [TestClass]
    public class MachineStateManagerTests
    {
        private MockEnvironmentProxy proxy;

        private MockMachineStateManager machineStateManager;

        private const string name = "foo";

        private const EnvironmentVariableTarget target = EnvironmentVariableTarget.Machine;

        private const string expectedValue = "bar";

        [TestInitialize]
        public void TestInitializeAttribute()
        {
            proxy = new MockEnvironmentProxy();

            proxy.SetEnvironmentVariable(name, expectedValue, target);

            machineStateManager = new MockMachineStateManager(proxy);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            machineStateManager.Dispose();
        }

        [TestMethod]
        public void MachineStateManagerCorrectlyDisposes()
        {
            using var caretaker = machineStateManager.SnapshotEnvironmentVariable(name, target);

            proxy.SetEnvironmentVariable(name, null, target);

            machineStateManager.Dispose();

            Assert.AreEqual(expectedValue, proxy.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void CaretakerCorrectlyDisposes()
        {
            using var caretaker = machineStateManager.SnapshotEnvironmentVariable(name, target);

            proxy.SetEnvironmentVariable(name, null, target);

            caretaker.Dispose();

            Assert.AreEqual(expectedValue, proxy.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void ReuseExistingCaretaker()
        {
            using (var caretaker1 = machineStateManager.SnapshotEnvironmentVariable(name, target))
            using (var caretaker2 = machineStateManager.SnapshotEnvironmentVariable(name, target))
            {
                proxy.SetEnvironmentVariable(name, null, target);

                Assert.IsTrue(ReferenceEquals(caretaker1, caretaker2));
            }

            Assert.AreEqual(expectedValue, proxy.GetEnvironmentVariable(name, target));
        }
    }
}
