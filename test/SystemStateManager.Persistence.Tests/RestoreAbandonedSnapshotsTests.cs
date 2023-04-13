using DevOptimal.SystemStateManager.Persistence;
using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Text;

namespace DevOptimal.SystemStateManager.Persistence.Tests
{
    [TestClass]
    [TestCategory("OmitFromCI")] // Fakes require Visual Studio Enterprise, but agent machines only have Community installed.
    public class RestoreAbandonedSnapshotsTests : TestBase
    {
        [TestMethod]
        public void RestoresAbandonedEnvironmentVariableSnapshots()
        {
            var environmentVariableName = "foo";
            var environmentVariableTarget = EnvironmentVariableTarget.Machine;
            var expectedEnvironmentVariableValue = "bar";

            /*
             * First, we will test restoring an environment variable that didn't exist when the snapshot was taken, but has since been created
             */
            // Delete the environment variable
            environment.SetEnvironmentVariable(environmentVariableName, null, environmentVariableTarget);

            // Simulate taking a snapshot of the environment variable from another process
            using (CreateShimsContext())
            {
                var systemStateManager = CreatePersistentSystemStateManager();
                systemStateManager.SnapshotEnvironmentVariable(environmentVariableName, environmentVariableTarget);
            }

            // Create the environment variable
            environment.SetEnvironmentVariable(environmentVariableName, expectedEnvironmentVariableValue, environmentVariableTarget);

            // Restore the snapshot
            PersistentSystemStateManager.RestoreAbandonedSnapshots(environment, fileSystem, registry);

            // Verify that the environment variable has been deleted
            Assert.AreEqual(null, environment.GetEnvironmentVariable(environmentVariableName, environmentVariableTarget));

            /*
             * Next, we will test restoring an environment variable that did exist when the snapshot was taken, but has since been deleted
             */
            // Create the environment variable
            environment.SetEnvironmentVariable(environmentVariableName, expectedEnvironmentVariableValue, environmentVariableTarget);

            // Simulate taking a snapshot of the environment variable from another process
            using (CreateShimsContext())
            {
                var systemStateManager = CreatePersistentSystemStateManager();
                systemStateManager.SnapshotEnvironmentVariable(environmentVariableName, environmentVariableTarget);
            }

            // Delete the environment variable
            environment.SetEnvironmentVariable(environmentVariableName, null, environmentVariableTarget);

            // Restore the snapshot
            PersistentSystemStateManager.RestoreAbandonedSnapshots(environment, fileSystem, registry);

            // Verify that the environment variable has been created
            Assert.AreEqual(expectedEnvironmentVariableValue, environment.GetEnvironmentVariable(environmentVariableName, environmentVariableTarget));
        }

        [TestMethod]
        public void RestoresAbandonedDirectorySnapshots()
        {
            var directoryPath = @"C:\foo";

            /*
             * First, we will test restoring a directory that didn't exist when the snapshot was taken, but has since been created
             */
            // Delete the directory, if it exists
            if (fileSystem.DirectoryExists(directoryPath))
            {
                fileSystem.DeleteDirectory(directoryPath, recursive: true);
            }

            // Simulate taking a snapshot of the directory from another process
            using (CreateShimsContext())
            {
                var systemStateManager = CreatePersistentSystemStateManager();
                systemStateManager.SnapshotDirectory(directoryPath);
            }

            // Create the directory
            fileSystem.CreateDirectory(directoryPath);

            // Restore the snapshot
            PersistentSystemStateManager.RestoreAbandonedSnapshots(environment, fileSystem, registry);

            // Verify that the directory has been deleted
            Assert.IsFalse(fileSystem.DirectoryExists(directoryPath));

            /*
             * Next, we will test restoring a directory that did exist when the snapshot was taken, but has since been deleted
             */
            // Create the directory
            fileSystem.CreateDirectory(directoryPath);

            // Simulate taking a snapshot of the directory from another process
            using (CreateShimsContext())
            {
                var systemStateManager = CreatePersistentSystemStateManager();
                systemStateManager.SnapshotDirectory(directoryPath);
            }

            // Delete the directory
            fileSystem.DeleteDirectory(directoryPath, recursive: true);

            // Restore the snapshot
            PersistentSystemStateManager.RestoreAbandonedSnapshots(environment, fileSystem, registry);

            // Verify that the directory has been created
            Assert.IsTrue(fileSystem.DirectoryExists(directoryPath));
        }

        [TestMethod]
        public void RestoresAbandonedFileSnapshots()
        {
            var filePath = @"C:\foo\bar.txt";
            var expectedFileBytes = Encoding.UTF8.GetBytes("Hello, world!");

            /*
             * First, we will test restoring a file that didn't exist when the snapshot was taken, but has since been created
             */
            // Delete the file, if it exists
            if (fileSystem.FileExists(filePath))
            {
                fileSystem.DeleteFile(filePath);
            }

            // Simulate taking a snapshot of the file from another process
            using (CreateShimsContext())
            {
                var systemStateManager = CreatePersistentSystemStateManager();
                systemStateManager.SnapshotFile(filePath);
            }

            // Create the file
            using (var stream = fileSystem.OpenFile(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(expectedFileBytes);
            }

            // Restore the snapshot
            PersistentSystemStateManager.RestoreAbandonedSnapshots(environment, fileSystem, registry);

            // Verify that the file has been deleted
            Assert.IsFalse(fileSystem.FileExists(filePath));

            /*
             * Next, we will test restoring a file that did exist when the snapshot was taken, but has since been deleted
             */
            // Create the file
            using (var stream = fileSystem.OpenFile(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(expectedFileBytes);
            }

            // Simulate taking a snapshot of the file from another process
            using (CreateShimsContext())
            {
                var systemStateManager = CreatePersistentSystemStateManager();
                systemStateManager.SnapshotFile(filePath);
            }

            // Delete the file
            fileSystem.DeleteFile(filePath);

            // Restore the snapshot
            PersistentSystemStateManager.RestoreAbandonedSnapshots(environment, fileSystem, registry);

            // Verify that the file has been created
            var actualFileBytes = new byte[expectedFileBytes.Length];
            using (var stream = fileSystem.OpenFile(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                stream.Read(actualFileBytes, 0, expectedFileBytes.Length);
            }
            CollectionAssert.AreEqual(expectedFileBytes, actualFileBytes);
        }

        [TestMethod]
        [SupportedOSPlatform("windows")]
        public void RestoresAbandonedRegistryKeySnapshots()
        {
            var registryHive = RegistryHive.LocalMachine;
            var registryView = RegistryView.Default;
            var registrySubKey = @"SOFTWARE\Microsoft\StrongName\Verification";

            /*
             * First, we will test restoring a registry key that didn't exist when the snapshot was taken, but has since been created
             */
            // Delete the registry key, if it exists
            if (registry.RegistryKeyExists(registryHive, registryView, registrySubKey))
            {
                registry.DeleteRegistryKey(registryHive, registryView, registrySubKey, recursive: true);
            }

            // Simulate taking a snapshot of the registry key from another process
            using (CreateShimsContext())
            {
                var systemStateManager = CreatePersistentSystemStateManager();
                systemStateManager.SnapshotRegistryKey(registryHive, registryView, registrySubKey);
            }

            // Create the registry key
            registry.CreateRegistryKey(registryHive, registryView, registrySubKey);

            // Restore the snapshot
            PersistentSystemStateManager.RestoreAbandonedSnapshots(environment, fileSystem, registry);

            // Verify that the registry key has been deleted
            Assert.IsFalse(registry.RegistryKeyExists(registryHive, registryView, registrySubKey));

            /*
             * Next, we will test restoring a registry key that did exist when the snapshot was taken, but has since been deleted
             */
            // Create the registry key
            registry.CreateRegistryKey(registryHive, registryView, registrySubKey);

            // Simulate taking a snapshot of the registry key from another process
            using (CreateShimsContext())
            {
                var systemStateManager = CreatePersistentSystemStateManager();
                systemStateManager.SnapshotRegistryKey(registryHive, registryView, registrySubKey);
            }

            // Delete the registry key
            registry.DeleteRegistryKey(registryHive, registryView, registrySubKey, recursive: true);

            // Restore the snapshot
            PersistentSystemStateManager.RestoreAbandonedSnapshots(environment, fileSystem, registry);

            // Verify that the registry key has been created
            Assert.IsTrue(registry.RegistryKeyExists(registryHive, registryView, registrySubKey));
        }

        [TestMethod]
        [SupportedOSPlatform("windows")]
        public void RestoresAbandonedRegistryValueSnapshots()
        {
            // Create registry key
            var registryHive = RegistryHive.LocalMachine;
            var registryView = RegistryView.Default;
            var registrySubKey = @"SOFTWARE\Microsoft\StrongName\Verification";
            registry.CreateRegistryKey(registryHive, registryView, registrySubKey);

            // One registry key value for each supported type
            var stringRegistryValueName = RegistryValueKind.String.ToString();
            var stringRegistryValueExpectedValue = "foo";
            var stringRegistryValueExpectedKind = RegistryValueKind.String;
            var expandStringRegistryValueName = RegistryValueKind.ExpandString.ToString();
            var expandStringRegistryValueExpectedValue = "bar";
            var expandStringRegistryValueExpectedKind = RegistryValueKind.ExpandString;
            var binaryRegistryValueName = RegistryValueKind.Binary.ToString();
            var binaryRegistryValueExpectedValue = Encoding.UTF8.GetBytes("Hello, world!");
            var binaryRegistryValueExpectedKind = RegistryValueKind.Binary;
            var dwordRegistryValueName = RegistryValueKind.DWord.ToString();
            var dwordRegistryValueExpectedValue = (int)3;
            var dwordRegistryValueExpectedKind = RegistryValueKind.DWord;
            var qwordRegistryValueName = RegistryValueKind.QWord.ToString();
            var qwordRegistryValueExpectedValue = (long)3;
            var qwordRegistryValueExpectedKind = RegistryValueKind.QWord;
            var multiStringRegistryValueName = RegistryValueKind.MultiString.ToString();
            var multiStringRegistryValueExpectedValue = new[] { "hello", "world" };
            var multiStringRegistryValueExpectedKind = RegistryValueKind.MultiString;

            /*
             * First, we will test restoring values that didn't exist when the snapshot was taken, but have since been created
             */
            // Delete the registry values, if they exist
            if (registry.RegistryValueExists(registryHive, registryView, registrySubKey, stringRegistryValueName))
            {
                registry.DeleteRegistryValue(registryHive, registryView, registrySubKey, stringRegistryValueName);
            }
            if (registry.RegistryValueExists(registryHive, registryView, registrySubKey, expandStringRegistryValueName))
            {
                registry.DeleteRegistryValue(registryHive, registryView, registrySubKey, expandStringRegistryValueName);
            }
            if (registry.RegistryValueExists(registryHive, registryView, registrySubKey, binaryRegistryValueName))
            {
                registry.DeleteRegistryValue(registryHive, registryView, registrySubKey, binaryRegistryValueName);
            }
            if (registry.RegistryValueExists(registryHive, registryView, registrySubKey, dwordRegistryValueName))
            {
                registry.DeleteRegistryValue(registryHive, registryView, registrySubKey, dwordRegistryValueName);
            }
            if (registry.RegistryValueExists(registryHive, registryView, registrySubKey, qwordRegistryValueName))
            {
                registry.DeleteRegistryValue(registryHive, registryView, registrySubKey, qwordRegistryValueName);
            }
            if (registry.RegistryValueExists(registryHive, registryView, registrySubKey, multiStringRegistryValueName))
            {
                registry.DeleteRegistryValue(registryHive, registryView, registrySubKey, multiStringRegistryValueName);
            }

            // Simulate taking snapshots of the registry values from another process
            using (CreateShimsContext())
            {
                var systemStateManager = CreatePersistentSystemStateManager();
                systemStateManager.SnapshotRegistryValue(registryHive, registryView, registrySubKey, stringRegistryValueName);
                systemStateManager.SnapshotRegistryValue(registryHive, registryView, registrySubKey, expandStringRegistryValueName);
                systemStateManager.SnapshotRegistryValue(registryHive, registryView, registrySubKey, binaryRegistryValueName);
                systemStateManager.SnapshotRegistryValue(registryHive, registryView, registrySubKey, dwordRegistryValueName);
                systemStateManager.SnapshotRegistryValue(registryHive, registryView, registrySubKey, qwordRegistryValueName);
                systemStateManager.SnapshotRegistryValue(registryHive, registryView, registrySubKey, multiStringRegistryValueName);
            }

            // Create the registry values
            registry.SetRegistryValue(registryHive, registryView, registrySubKey, stringRegistryValueName, stringRegistryValueExpectedValue, stringRegistryValueExpectedKind);
            registry.SetRegistryValue(registryHive, registryView, registrySubKey, expandStringRegistryValueName, expandStringRegistryValueExpectedValue, expandStringRegistryValueExpectedKind);
            registry.SetRegistryValue(registryHive, registryView, registrySubKey, binaryRegistryValueName, binaryRegistryValueExpectedValue, binaryRegistryValueExpectedKind);
            registry.SetRegistryValue(registryHive, registryView, registrySubKey, dwordRegistryValueName, dwordRegistryValueExpectedValue, dwordRegistryValueExpectedKind);
            registry.SetRegistryValue(registryHive, registryView, registrySubKey, qwordRegistryValueName, qwordRegistryValueExpectedValue, qwordRegistryValueExpectedKind);
            registry.SetRegistryValue(registryHive, registryView, registrySubKey, multiStringRegistryValueName, multiStringRegistryValueExpectedValue, multiStringRegistryValueExpectedKind);

            // Restore the snapshots
            PersistentSystemStateManager.RestoreAbandonedSnapshots(environment, fileSystem, registry);

            // Verify that the registry values have been deleted
            Assert.IsFalse(registry.RegistryValueExists(registryHive, registryView, registrySubKey, stringRegistryValueName));
            Assert.IsFalse(registry.RegistryValueExists(registryHive, registryView, registrySubKey, expandStringRegistryValueName));
            Assert.IsFalse(registry.RegistryValueExists(registryHive, registryView, registrySubKey, binaryRegistryValueName));
            Assert.IsFalse(registry.RegistryValueExists(registryHive, registryView, registrySubKey, dwordRegistryValueName));
            Assert.IsFalse(registry.RegistryValueExists(registryHive, registryView, registrySubKey, qwordRegistryValueName));
            Assert.IsFalse(registry.RegistryValueExists(registryHive, registryView, registrySubKey, multiStringRegistryValueName));

            /*
             * Next, we will test restoring values that did exist when the snapshot was taken, but have since been deleted
             */
            // Create a bunch of registry values
            registry.SetRegistryValue(registryHive, registryView, registrySubKey, stringRegistryValueName, stringRegistryValueExpectedValue, stringRegistryValueExpectedKind);
            registry.SetRegistryValue(registryHive, registryView, registrySubKey, expandStringRegistryValueName, expandStringRegistryValueExpectedValue, expandStringRegistryValueExpectedKind);
            registry.SetRegistryValue(registryHive, registryView, registrySubKey, binaryRegistryValueName, binaryRegistryValueExpectedValue, binaryRegistryValueExpectedKind);
            registry.SetRegistryValue(registryHive, registryView, registrySubKey, dwordRegistryValueName, dwordRegistryValueExpectedValue, dwordRegistryValueExpectedKind);
            registry.SetRegistryValue(registryHive, registryView, registrySubKey, qwordRegistryValueName, qwordRegistryValueExpectedValue, qwordRegistryValueExpectedKind);
            registry.SetRegistryValue(registryHive, registryView, registrySubKey, multiStringRegistryValueName, multiStringRegistryValueExpectedValue, multiStringRegistryValueExpectedKind);

            // Simulate taking snapshots of the registry values from another process
            using (CreateShimsContext())
            {
                var systemStateManager = CreatePersistentSystemStateManager();
                systemStateManager.SnapshotRegistryValue(registryHive, registryView, registrySubKey, stringRegistryValueName);
                systemStateManager.SnapshotRegistryValue(registryHive, registryView, registrySubKey, expandStringRegistryValueName);
                systemStateManager.SnapshotRegistryValue(registryHive, registryView, registrySubKey, binaryRegistryValueName);
                systemStateManager.SnapshotRegistryValue(registryHive, registryView, registrySubKey, dwordRegistryValueName);
                systemStateManager.SnapshotRegistryValue(registryHive, registryView, registrySubKey, qwordRegistryValueName);
                systemStateManager.SnapshotRegistryValue(registryHive, registryView, registrySubKey, multiStringRegistryValueName);
            }

            // Delete the registry values
            registry.DeleteRegistryValue(registryHive, registryView, registrySubKey, stringRegistryValueName);
            registry.DeleteRegistryValue(registryHive, registryView, registrySubKey, expandStringRegistryValueName);
            registry.DeleteRegistryValue(registryHive, registryView, registrySubKey, binaryRegistryValueName);
            registry.DeleteRegistryValue(registryHive, registryView, registrySubKey, dwordRegistryValueName);
            registry.DeleteRegistryValue(registryHive, registryView, registrySubKey, qwordRegistryValueName);
            registry.DeleteRegistryValue(registryHive, registryView, registrySubKey, multiStringRegistryValueName);

            // Restore the snapshots
            PersistentSystemStateManager.RestoreAbandonedSnapshots(environment, fileSystem, registry);

            // Verify that the registry values have been restored
            var (stringRegistryValueActualValue, stringRegistryValueActualKind) = registry.GetRegistryValue(registryHive, registryView, registrySubKey, stringRegistryValueName);
            Assert.AreEqual(stringRegistryValueExpectedValue, stringRegistryValueActualValue);
            Assert.AreEqual(stringRegistryValueExpectedKind, stringRegistryValueActualKind);
            var (expandStringRegistryValueActualValue, expandStringRegistryValueActualKind) = registry.GetRegistryValue(registryHive, registryView, registrySubKey, expandStringRegistryValueName);
            Assert.AreEqual(expandStringRegistryValueExpectedValue, expandStringRegistryValueActualValue);
            Assert.AreEqual(expandStringRegistryValueExpectedKind, expandStringRegistryValueActualKind);
            var (binaryRegistryValueActualValue, binaryRegistryValueActualKind) = registry.GetRegistryValue(registryHive, registryView, registrySubKey, binaryRegistryValueName);
            CollectionAssert.AreEqual(binaryRegistryValueExpectedValue, (byte[])binaryRegistryValueActualValue);
            Assert.AreEqual(binaryRegistryValueExpectedKind, binaryRegistryValueActualKind);
            var (dwordRegistryValueActualValue, dwordRegistryValueActualKind) = registry.GetRegistryValue(registryHive, registryView, registrySubKey, dwordRegistryValueName);
            Assert.AreEqual(dwordRegistryValueExpectedValue, dwordRegistryValueActualValue);
            Assert.AreEqual(dwordRegistryValueExpectedKind, dwordRegistryValueActualKind);
            var (qwordRegistryValueActualValue, qwordRegistryValueActualKind) = registry.GetRegistryValue(registryHive, registryView, registrySubKey, qwordRegistryValueName);
            Assert.AreEqual(qwordRegistryValueExpectedValue, qwordRegistryValueActualValue);
            Assert.AreEqual(qwordRegistryValueExpectedKind, qwordRegistryValueActualKind);
            var (multiStringRegistryValueActualValue, multiStringRegistryValueActualKind) = registry.GetRegistryValue(registryHive, registryView, registrySubKey, multiStringRegistryValueName);
            CollectionAssert.AreEqual(multiStringRegistryValueExpectedValue, (object[])multiStringRegistryValueActualValue);
            Assert.AreEqual(multiStringRegistryValueExpectedKind, multiStringRegistryValueActualKind);
        }
    }
}

