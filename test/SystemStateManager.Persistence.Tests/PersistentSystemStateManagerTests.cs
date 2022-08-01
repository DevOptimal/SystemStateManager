using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics.Fakes;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DevOptimal.SystemStateManager.Persistence.Tests
{
    [TestClass]
    public class PersistentSystemStateManagerTests : TestBase
    {
        [TestMethod]
        [TestCategory("OmitFromCI")] // Fakes require VS Enterprise, but agent machines only have Community installed.
        public void RestoresAbandonedSnapshots()
        {
            var environmentVariableName = "foo";
            var environmentVariableTarget = EnvironmentVariableTarget.Machine;
            var expectedEnvironmentVariableValue = "bar";
            environment.SetEnvironmentVariable(environmentVariableName, expectedEnvironmentVariableValue, environmentVariableTarget);

            var directoryPath = @"C:\foo";
            fileSystem.CreateDirectory(directoryPath);
            var filePath = @"C:\foo\bar.txt";
            var expectedFileBytes = Encoding.UTF8.GetBytes("Hello, world!");
            using (var stream = fileSystem.OpenFile(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(expectedFileBytes);
            }

            var registryHive = RegistryHive.LocalMachine;
            var registryView = RegistryView.Default;
            var registrySubKey = @"SOFTWARE\Microsoft\StrongName\Verification";
            registry.CreateRegistryKey(registryHive, registryView, registrySubKey);
            var registryValueName = "foo";
            var expectedRegistryValue = "bar";
            var expectedRegistryValueKind = RegistryValueKind.String;
            registry.SetRegistryValue(registryHive, registryView, registrySubKey, registryValueName, expectedRegistryValue, expectedRegistryValueKind);
            var registryValueName2 = "fooint";
            var expectedRegistryValue2 = 3;
            var expectedRegistryValueKind2 = RegistryValueKind.DWord;
            registry.SetRegistryValue(registryHive, registryView, registrySubKey, registryValueName2, expectedRegistryValue2, expectedRegistryValueKind2);

            var fakeProcessID = System.Environment.ProcessId + 1;
            var fakeProcessStartTime = DateTime.Now;

            using (ShimsContext.Create())
            {
                ShimProcess.AllInstances.IdGet = p => fakeProcessID;
                ShimProcess.AllInstances.StartTimeGet = p => fakeProcessStartTime;

                var systemStateManager = new PersistentSystemStateManager(environment, fileSystem, registry);

                systemStateManager.SnapshotEnvironmentVariable(environmentVariableName, environmentVariableTarget);

                systemStateManager.SnapshotDirectory(directoryPath);
                systemStateManager.SnapshotFile(filePath);

                systemStateManager.SnapshotRegistryKey(registryHive, registryView, registrySubKey);
                systemStateManager.SnapshotRegistryValue(registryHive, registryView, registrySubKey, registryValueName);
                systemStateManager.SnapshotRegistryValue(registryHive, registryView, registrySubKey, registryValueName2);
            }

            registry.DeleteRegistryValue(registryHive, registryView, registrySubKey, registryValueName2);
            registry.DeleteRegistryValue(registryHive, registryView, registrySubKey, registryValueName);
            registry.DeleteRegistryKey(registryHive, registryView, registrySubKey, recursive: true);

            fileSystem.DeleteFile(filePath);
            fileSystem.DeleteDirectory(directoryPath, recursive: true);

            environment.SetEnvironmentVariable(environmentVariableName, null, environmentVariableTarget);

            PersistentSystemStateManager.RestoreAbandonedSnapshots();

            Assert.AreEqual(expectedEnvironmentVariableValue, environment.GetEnvironmentVariable(environmentVariableName, environmentVariableTarget));

            Assert.IsTrue(fileSystem.DirectoryExists(directoryPath));
            var actualFileBytes = new byte[expectedFileBytes.Length];
            using (var stream = fileSystem.OpenFile(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                stream.Read(actualFileBytes, 0, expectedFileBytes.Length);
            }
            CollectionAssert.AreEqual(expectedFileBytes, actualFileBytes);

            Assert.IsTrue(registry.RegistryKeyExists(registryHive, registryView, registrySubKey));
            Assert.AreEqual((expectedRegistryValue, expectedRegistryValueKind), registry.GetRegistryValue(registryHive, registryView, registrySubKey, registryValueName));
            Assert.AreEqual((expectedRegistryValue2, expectedRegistryValueKind2), registry.GetRegistryValue(registryHive, registryView, registrySubKey, registryValueName2));
        }

        [TestMethod]
        public void DoesNotRestoreSnapshotsFromCurrentProcess()
        {
            var name = "foo";
            var target = EnvironmentVariableTarget.Machine;
            var expectedValue = "bar";

            using var systemStateManager = new PersistentSystemStateManager(environment, fileSystem, registry);

            systemStateManager.SnapshotEnvironmentVariable(name, target);

            environment.SetEnvironmentVariable(name, null, target);

            PersistentSystemStateManager.RestoreAbandonedSnapshots();
            Assert.AreNotEqual(expectedValue, environment.GetEnvironmentVariable(name, target));
        }

        [TestMethod]
        public void RevertsSnapshotsConcurrently()
        {
            using var systemStateManager = new PersistentSystemStateManager(environment, fileSystem, registry);

            var target = EnvironmentVariableTarget.Machine;
            var expectedValue = "bar";

            var tasks = new List<Task>();

            for (var i = 0; i < 100; i++)
            {
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    var name = Guid.NewGuid().ToString();
                    using (systemStateManager.SnapshotEnvironmentVariable(name, target))
                    {
                        environment.SetEnvironmentVariable(name, expectedValue, target);
                        Assert.AreEqual(expectedValue, environment.GetEnvironmentVariable(name, target));
                    }
                    Assert.AreEqual(null, environment.GetEnvironmentVariable(name, target));
                }));
            }

            Task.WaitAll(tasks.ToArray());
        }
    }
}
