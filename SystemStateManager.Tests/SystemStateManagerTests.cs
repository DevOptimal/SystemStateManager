using DevOptimal.System.Resources.Environment;
using System;
using System.IO;

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
            using var caretaker = systemStateManager.SnapshotEnvironmentVariable(name, target);

            proxy.SetEnvironmentVariable(name, null, target);

            systemStateManager.Dispose();

            Assert.AreEqual(expectedValue, proxy.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void CaretakerCorrectlyDisposes()
        {
            using var caretaker = systemStateManager.SnapshotEnvironmentVariable(name, target);

            proxy.SetEnvironmentVariable(name, null, target);

            caretaker.Dispose();

            Assert.AreEqual(expectedValue, proxy.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void ReuseExistingCaretaker()
        {
            using (var caretaker1 = systemStateManager.SnapshotEnvironmentVariable(name, target))
            using (var caretaker2 = systemStateManager.SnapshotEnvironmentVariable(name, target))
            {
                proxy.SetEnvironmentVariable(name, null, target);

                Assert.IsTrue(ReferenceEquals(caretaker1, caretaker2));
            }

            Assert.AreEqual(expectedValue, proxy.GetEnvironmentVariable(name, target));
        }
    }
}
