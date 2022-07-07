using DevOptimal.SystemUtilities.Registry;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace DevOptimal.SystemStateManager.Tests.Registry
{
    [TestClass]
    [SupportedOSPlatform("windows")]
    public class RegistryValueTests
    {
        private MockRegistry registry;

        private MockSystemStateManager systemStateManager;

        private const RegistryHive hive = RegistryHive.LocalMachine;
        private const RegistryView view = RegistryView.Default;
        private const string subKey = @"SOFTWARE\Microsoft\Windows";
        private const string name = "foo";

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
        public void RevertsRegistryValueAlteration()
        {
            registry.CreateRegistryKey(hive, view, subKey);

            var value = "bar";
            var kind = RegistryValueKind.String;
            registry.SetRegistryValue(hive, view, subKey, name, value, kind);

            using (systemStateManager.SnapshotRegistryValue(hive, view, subKey, name))
            {
                registry.SetRegistryValue(hive, view, subKey, name, 10, RegistryValueKind.DWord);
            }

            Assert.IsTrue(registry.RegistryValueExists(hive, view, subKey, name));
            var (actualValue, actualKind) = registry.GetRegistryValue(hive, view, subKey, name);
            Assert.AreEqual(value, actualValue);
            Assert.AreEqual(kind, actualKind);
        }

        [TestMethod]
        public void RevertsRegistryValueCreation()
        {
            registry.CreateRegistryKey(hive, view, subKey);

            using (systemStateManager.SnapshotRegistryValue(hive, view, subKey, name))
            {
                registry.SetRegistryValue(hive, view, subKey, name, "bar", RegistryValueKind.String);
            }

            Assert.IsFalse(registry.RegistryValueExists(hive, view, subKey, name));
        }

        [TestMethod]
        public void RevertsRegistryValueDeletion()
        {
            registry.CreateRegistryKey(hive, view, subKey);

            var value = "bar";
            var kind = RegistryValueKind.String;
            registry.SetRegistryValue(hive, view, subKey, name, value, kind);

            using (systemStateManager.SnapshotRegistryValue(hive, view, subKey, name))
            {
                registry.DeleteRegistryValue(hive, view, subKey, name);
            }

            Assert.IsTrue(registry.RegistryValueExists(hive, view, subKey, name));
            var (actualValue, actualKind) = registry.GetRegistryValue(hive, view, subKey, name);
            Assert.AreEqual(value, actualValue);
            Assert.AreEqual(kind, actualKind);
        }

        [TestMethod]
        public void RevertsDefaultRegistryValueAlteration()
        {
            registry.CreateRegistryKey(hive, view, subKey);

            var value = "bar";
            var kind = RegistryValueKind.String;
            registry.SetRegistryValue(hive, view, subKey, null, value, kind);

            using (systemStateManager.SnapshotRegistryValue(hive, view, subKey, null))
            {
                registry.SetRegistryValue(hive, view, subKey, null, 10, RegistryValueKind.DWord);
            }

            Assert.IsTrue(registry.RegistryValueExists(hive, view, subKey, null));
            var (actualValue, actualKind) = registry.GetRegistryValue(hive, view, subKey, null);
            Assert.AreEqual(value, actualValue);
            Assert.AreEqual(kind, actualKind);
        }

        [TestMethod]
        public void RevertsDefaultRegistryValueCreation()
        {
            registry.CreateRegistryKey(hive, view, subKey);

            using (systemStateManager.SnapshotRegistryValue(hive, view, subKey, null))
            {
                registry.SetRegistryValue(hive, view, subKey, null, "bar", RegistryValueKind.String);
            }

            Assert.IsFalse(registry.RegistryValueExists(hive, view, subKey, null));
        }

        [TestMethod]
        public void RevertsDefaultRegistryValueDeletion()
        {
            registry.CreateRegistryKey(hive, view, subKey);

            var value = "bar";
            var kind = RegistryValueKind.String;
            registry.SetRegistryValue(hive, view, subKey, null, value, kind);

            using (systemStateManager.SnapshotRegistryValue(hive, view, subKey, null))
            {
                registry.DeleteRegistryValue(hive, view, subKey, null);
            }

            Assert.IsTrue(registry.RegistryValueExists(hive, view, subKey, null));
            var (actualValue, actualKind) = registry.GetRegistryValue(hive, view, subKey, null);
            Assert.AreEqual(value, actualValue);
            Assert.AreEqual(kind, actualKind);
        }
    }
}
