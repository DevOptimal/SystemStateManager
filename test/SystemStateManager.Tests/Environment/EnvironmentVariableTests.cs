using DevOptimal.SystemUtilities.Environment;
using System;

namespace DevOptimal.SystemStateManager.Tests.Environment
{
    [TestClass]
    public class EnvironmentTests
    {
        private MockEnvironment environment;

        private MockSystemStateManager systemStateManager;

        private const string name = "foo";

        private const EnvironmentVariableTarget target = EnvironmentVariableTarget.Machine;

        [TestInitialize]
        public void TestInitializeAttribute()
        {
            environment = new MockEnvironment();

            systemStateManager = new MockSystemStateManager(environment);
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
                environment.SetEnvironmentVariable(name, "bar", target);
            }

            Assert.AreEqual(null, environment.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void RevertsEnvironmentVariableDeletion()
        {
            var expectedValue = "bar";
            environment.SetEnvironmentVariable(name, expectedValue, target);

            using (systemStateManager.SnapshotEnvironmentVariable(name, target))
            {
                environment.SetEnvironmentVariable(name, null, target);
            }

            Assert.AreEqual(expectedValue, environment.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void RevertsEnvironmentVariableAlteration()
        {
            var expectedValue = "bar";
            environment.SetEnvironmentVariable(name, expectedValue, target);

            using (systemStateManager.SnapshotEnvironmentVariable(name, target))
            {
                environment.SetEnvironmentVariable(name, "baz", target);
            }

            Assert.AreEqual(expectedValue, environment.GetEnvironmentVariable(name, target));
        }
    }
}