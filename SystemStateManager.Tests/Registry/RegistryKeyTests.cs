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
        private MockRegistryProxy proxy;

        private MockSystemStateManager systemStateManager;

        private const RegistryHive hive = RegistryHive.LocalMachine;

        private const RegistryView view = RegistryView.Default;

        private const string subKey = @"SOTFWARE\Microsoft\Windows";

        [TestInitialize]
        public void TestInitializeAttribute()
        {
            proxy = new MockRegistryProxy();

            systemStateManager = new MockSystemStateManager(proxy);
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
                proxy.CreateRegistryKey(hive, view, subKey);
            }

            Assert.IsFalse(proxy.RegistryKeyExists(hive, view, subKey));
        }

        [TestMethod]
        public void RevertsRegistryKeyCreationWithChildren()
        {
            using (systemStateManager.SnapshotRegistryKey(hive, view, subKey))
            {
                proxy.CreateRegistryKey(hive, view, subKey);
                proxy.CreateRegistryKey(hive, view, Path.Combine(subKey, "foo"));
                proxy.SetRegistryValue(hive, view, subKey, "bar", "Hello, world!", RegistryValueKind.String);
            }

            Assert.IsFalse(proxy.RegistryKeyExists(hive, view, subKey));
        }

        [TestMethod]
        public void RevertsRegistryKeyDeletion()
        {
            proxy.CreateRegistryKey(hive, view, subKey);

            using (systemStateManager.SnapshotRegistryKey(hive, view, subKey))
            {
                proxy.DeleteRegistryKey(hive, view, subKey, recursive: true);
            }

            Assert.IsTrue(proxy.RegistryKeyExists(hive, view, subKey));
        }
    }
}
