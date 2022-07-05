using DevOptimal.System.Resources.FileSystem;
using System.IO;

namespace DevOptimal.SystemStateManager.Tests.FileSystem
{
    [TestClass]
    public class DirectoryTests
    {
        private MockFileSystemProxy proxy;

        private MockSystemStateManager systemStateManager;

        private const string path = @"C:\foo\bar";

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
        public void RevertsDirectoryCreation()
        {
            using (systemStateManager.SnapshotDirectory(path))
            {
                proxy.CreateDirectory(path);

                Assert.IsTrue(proxy.DirectoryExists(path));
            }

            Assert.IsFalse(proxy.DirectoryExists(path));
        }

        [TestMethod]
        public void RevertsDirectoryCreationWithChildren()
        {
            using (systemStateManager.SnapshotDirectory(path))
            {
                proxy.CreateDirectory(path);
                proxy.CreateDirectory(Path.Combine(path, "blah"));
                proxy.CreateFile(Path.Combine(path, "log.txt"));
            }

            Assert.IsFalse(proxy.DirectoryExists(path));
        }

        [TestMethod]
        public void RevertsDirectoryDeletion()
        {
            proxy.CreateDirectory(path);

            using (systemStateManager.SnapshotDirectory(path))
            {
                proxy.DeleteDirectory(path, recursive: true);

                Assert.IsFalse(proxy.DirectoryExists(path));
            }

            Assert.IsTrue(proxy.DirectoryExists(path));
        }
    }
}
