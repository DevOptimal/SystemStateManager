using DevOptimal.SystemUtilities.Registry;
using Microsoft.Win32;
using System.IO;
using System.Runtime.Versioning;

namespace DevOptimal.SystemStateManager.Tests.Registry
{
    [TestClass]
    [SupportedOSPlatform("windows")]
    public class RegistryKeyTests : TestBase
    {
        [TestMethod]
        public void RevertsRegistryKeyCreation()
        {
            var hive = RegistryHive.LocalMachine;
            var view = RegistryView.Default;
            var subKey = @"SOTFWARE\Microsoft\Windows";

            using var systemStateManager = CreateSystemStateManager();
            using (systemStateManager.SnapshotRegistryKey(hive, view, subKey))
            {
                registry.CreateRegistryKey(hive, view, subKey);
            }

            Assert.IsFalse(registry.RegistryKeyExists(hive, view, subKey));
        }

        [TestMethod]
        public void RevertsRegistryKeyCreationWithChildren()
        {
            var hive = RegistryHive.LocalMachine;
            var view = RegistryView.Default;
            var subKey = @"SOTFWARE\Microsoft\Windows";

            using var systemStateManager = CreateSystemStateManager();
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
            var hive = RegistryHive.LocalMachine;
            var view = RegistryView.Default;
            var subKey = @"SOTFWARE\Microsoft\Windows";

            registry.CreateRegistryKey(hive, view, subKey);

            using var systemStateManager = CreateSystemStateManager();
            using (systemStateManager.SnapshotRegistryKey(hive, view, subKey))
            {
                registry.DeleteRegistryKey(hive, view, subKey, recursive: true);
            }

            Assert.IsTrue(registry.RegistryKeyExists(hive, view, subKey));
        }
    }
}
