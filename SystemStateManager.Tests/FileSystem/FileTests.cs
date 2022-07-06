using DevOptimal.SystemUtilities.FileSystem;
using System;
using System.IO;

namespace DevOptimal.SystemStateManager.Tests.FileSystem
{
    [TestClass]
    public class FileTests
    {
        private MockFileSystemProxy proxy;

        private MockSystemStateManager systemStateManager;

        private const string path = @"C:\foo\bar.dat";

        [TestInitialize]
        public void TestInitializeAttribute()
        {
            proxy = new MockFileSystemProxy();

            systemStateManager = new MockSystemStateManager(proxy);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            systemStateManager.Dispose();
        }

        [TestMethod]
        public void RevertsFileAlteration()
        {
            var expectedFileBytes = Guid.NewGuid().ToByteArray();
            WriteBytes(path, expectedFileBytes);

            using (systemStateManager.SnapshotFile(path))
            {
                WriteBytes(path, Guid.NewGuid().ToByteArray());
            }

            CollectionAssert.AreEqual(expectedFileBytes, ReadBytes(path));
        }

        [TestMethod]
        public void RevertsFileCreation()
        {
            using (systemStateManager.SnapshotFile(path))
            {
                proxy.CreateFile(path);
            }

            Assert.IsFalse(proxy.FileExists(path));
        }

        [TestMethod]
        public void RevertsFileDeletion()
        {
            var expectedFileBytes = Guid.NewGuid().ToByteArray();
            WriteBytes(path, expectedFileBytes);

            using (systemStateManager.SnapshotFile(path))
            {
                proxy.DeleteFile(path);
            }

            CollectionAssert.AreEqual(expectedFileBytes, ReadBytes(path));
        }

        [TestMethod]
        public void RevertsMultipleFileDeletionsWithSameContent()
        {
            var expectedFileBytes = Guid.NewGuid().ToByteArray();
            WriteBytes(path, expectedFileBytes);

            var path2 = @"C:\foo\baz.dat";
            WriteBytes(path2, expectedFileBytes);

            using (var snapshot = systemStateManager.SnapshotFile(path))
            {
                proxy.DeleteFile(path);
                Assert.IsFalse(proxy.FileExists(path));

                using (var snapshot2 = systemStateManager.SnapshotFile(path2))
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
