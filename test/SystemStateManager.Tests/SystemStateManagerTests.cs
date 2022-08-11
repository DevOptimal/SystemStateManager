using System;

namespace DevOptimal.SystemStateManager.Tests
{
    [TestClass]
    public class SystemStateManagerTests : TestBase
    {
        [TestMethod]
        public void SystemStateManagerCorrectlyDisposes()
        {
            var name = "foo";
            var target = EnvironmentVariableTarget.Machine;
            var expectedValue = "bar";

            environment.SetEnvironmentVariable(name, expectedValue, target);

            using (var systemStateManager = CreateSystemStateManager())
            {

                systemStateManager.SnapshotEnvironmentVariable(name, target);

                environment.SetEnvironmentVariable(name, null, target);
            }

            Assert.AreEqual(expectedValue, environment.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void SnapshotCorrectlyDisposes()
        {
            var name = "foo";
            var target = EnvironmentVariableTarget.Machine;
            var expectedValue = "bar";

            environment.SetEnvironmentVariable(name, expectedValue, target);

            using var systemStateManager = CreateSystemStateManager();

            using (systemStateManager.SnapshotEnvironmentVariable(name, target))
            {
                environment.SetEnvironmentVariable(name, null, target);
            }

            Assert.AreEqual(expectedValue, environment.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void ReuseExistingSnapshot()
        {
            var name = "foo";
            var target = EnvironmentVariableTarget.Machine;
            var expectedValue = "bar";

            environment.SetEnvironmentVariable(name, expectedValue, target);

            using var systemStateManager = CreateSystemStateManager();
            using (var snapshot1 = systemStateManager.SnapshotEnvironmentVariable(name, target))
            using (var snapshot2 = systemStateManager.SnapshotEnvironmentVariable(name, target))
            {
                Assert.IsTrue(ReferenceEquals(snapshot1, snapshot2));

                environment.SetEnvironmentVariable(name, null, target);
            }

            Assert.AreEqual(expectedValue, environment.GetEnvironmentVariable(name, target));
        }
    }
}
