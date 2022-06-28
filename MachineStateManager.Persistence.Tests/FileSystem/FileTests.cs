using bradselw.System.Resources.FileSystem;
using System;
using System.IO;

namespace bradselw.MachineStateManager.Persistence.Tests.FileSystem
{
    [TestClass]
    public class FileTests
    {
        private MockFileSystemProxy proxy;

        private const string path = @"C:\temp\foo.bar";

        private MockMachineStateManager machineStateManager;

        private byte[] expectedFileBytes;

        [TestInitialize]
        public void TestInitializeAttribute()
        {
            proxy = new MockFileSystemProxy();

            expectedFileBytes = Guid.NewGuid().ToByteArray();

            WriteBytes(path, expectedFileBytes);

            machineStateManager = new MockMachineStateManager(proxy);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            machineStateManager.Dispose();
        }

        [TestMethod]
        public void RestoresChangedFile()
        {
            using (machineStateManager.SnapshotFile(path))
            {
                var someOtherData = Guid.NewGuid().ToByteArray();
                WriteBytes(path, someOtherData);

                var readOtherData = ReadBytes(path);
                CollectionAssert.AreEqual(someOtherData, readOtherData);
            }

            var actualFileBytes = ReadBytes(path);

            CollectionAssert.AreEqual(expectedFileBytes, actualFileBytes);
        }

        [TestMethod]
        public void RestoresDeletedFile()
        {
            using (machineStateManager.SnapshotFile(path))
            {
                proxy.DeleteFile(path);
            }

            CollectionAssert.AreEqual(expectedFileBytes, ReadBytes(path));
        }

        [TestMethod]
        public void RevertCreatedFile()
        {
            var path2 = @"C:\temp\foo.baz";

            using (machineStateManager.SnapshotFile(path2))
            {
                proxy.CreateFile(path2);
            }

            Assert.IsFalse(proxy.FileExists(path2));
        }

        [TestMethod]
        public void MachineStateManagerCorrectlyDisposes()
        {
            using var caretaker = machineStateManager.SnapshotFile(path);

            proxy.DeleteFile(path);

            machineStateManager.Dispose();

            CollectionAssert.AreEqual(expectedFileBytes, ReadBytes(path));
        }

        [TestMethod]
        public void CaretakerCorrectlyDisposes()
        {
            using var caretaker = machineStateManager.SnapshotFile(path);

            proxy.DeleteFile(path);

            caretaker.Dispose();

            CollectionAssert.AreEqual(expectedFileBytes, ReadBytes(path));
        }

        [TestMethod]
        public void CorrectlyRestoresTwoDifferentFilesWithSameContent()
        {
            var path2 = @"C:\temp\foo.baz";
            WriteBytes(path2, expectedFileBytes);

            using (var caretaker = machineStateManager.SnapshotFile(path))
            {
                proxy.DeleteFile(path);
                Assert.IsFalse(proxy.FileExists(path));

                using (var caretaker2 = machineStateManager.SnapshotFile(path2))
                {
                    proxy.DeleteFile(path2);
                    Assert.IsFalse(proxy.FileExists(path2));
                }

                CollectionAssert.AreEqual(expectedFileBytes, ReadBytes(path2));
            }

            CollectionAssert.AreEqual(expectedFileBytes, ReadBytes(path));
        }

        private void WriteBytes(string path, byte[] bytes)
        {
            using var stream = proxy.OpenFile(path, FileMode.Create, FileAccess.Write, FileShare.None);

            stream.Write(bytes, 0, bytes.Length);
        }

        private byte[] ReadBytes(string path)
        {
            using var stream = proxy.OpenFile(path, FileMode.Open, FileAccess.Read, FileShare.None);
            var result = new byte[stream.Length];
            stream.Read(result, 0, result.Length);
            return result;
        }
    }
}
