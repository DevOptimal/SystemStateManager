using System;

namespace DevOptimal.SystemStateManager.Tests.Environment
{
    [TestClass]
    public class EnvironmentTests : TestBase
    {
        [TestMethod]
        public void RevertsEnvironmentVariableCreation()
        {
            var name = "foo";
            var target = EnvironmentVariableTarget.Machine;
            environment.SetEnvironmentVariable(name, null, target);

            using var systemStateManager = CreateSystemStateManager();
            using (systemStateManager.SnapshotEnvironmentVariable(name, target))
            {
                environment.SetEnvironmentVariable(name, "bar", target);
            }

            Assert.AreEqual(null, environment.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void RevertsEnvironmentVariableDeletion()
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
        public void RevertsEnvironmentVariableAlteration()
        {
            var name = "foo";
            var target = EnvironmentVariableTarget.Machine;
            var expectedValue = "bar";
            environment.SetEnvironmentVariable(name, expectedValue, target);

            using var systemStateManager = CreateSystemStateManager();
            using (systemStateManager.SnapshotEnvironmentVariable(name, target))
            {
                environment.SetEnvironmentVariable(name, "baz", target);
            }

            Assert.AreEqual(expectedValue, environment.GetEnvironmentVariable(name, target));
        }
    }
}