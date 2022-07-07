using DevOptimal.SystemUtilities.Registry;
using Microsoft.Win32;
using System.IO;
using System.Runtime.Versioning;

namespace DevOptimal.SystemStateManager.Tests.Registry
{
    [TestClass]
    [SupportedOSPlatform("windows")]
    public class RegistryKeyTests
    {
        private MockRegistry registry;

        private MockSystemStateManager systemStateManager;

        private const RegistryHive hive = RegistryHive.LocalMachine;

        private const RegistryView view = RegistryView.Default;

        private const string subKey = @"SOTFWARE\Microsoft\Windows";

        [TestInitialize]
        public void TestInitializeAttribute()
        {
            registry = new MockRegistry();

            systemStateManager = new MockSystemStateManager(registry);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            systemStateManager.Dispose();
        }

        [TestMethod]
        public void RevertsRegistryKeyCreation()
        {
            using (systemStateManager.SnapshotRegistryKey(hive, view, subKey))
            {
                registry.CreateRegistryKey(hive, view, subKey);
            }

            Assert.IsFalse(registry.RegistryKeyExists(hive, view, subKey));
        }

        [TestMethod]
        public void RevertsRegistryKeyCreationWithChildren()
        {
            using (systemStateManager.SnapshotRegistryKey(hive, view, subKey))
            {
                registry.CreateRegistryKey(hive, view, subKey);
                registry.CreateRegistryKey(hive, view, Path.Combine(subKey, "foo"));
                registry.SetRegistryValue(hive, view, subKey, "bar", "Hello, world!", RegistryValueKind.String);
            }

            Assert.IsFalse(registry.RegistryKeyExists(hive, view, subKey));
        }

        [TestMethod]
        public void RevertsRegistryKeyDeletion()
        {
            registry.CreateRegistryKey(hive, view, subKey);

            using (systemStateManager.SnapshotRegistryKey(hive, view, subKey))
            {
                registry.DeleteRegistryKey(hive, view, subKey, recursive: true);
            }

            Assert.IsTrue(registry.RegistryKeyExists(hive, view, subKey));
        }
    }
}
