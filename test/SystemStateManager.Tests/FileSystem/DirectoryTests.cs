using DevOptimal.SystemUtilities.FileSystem;
using System.IO;

namespace DevOptimal.SystemStateManager.Tests.FileSystem
{
    [TestClass]
    public class DirectoryTests
    {
        private MockFileSystem fileSystem;

        private MockSystemStateManager systemStateManager;

        private const string path = @"C:\foo\bar";

        [TestInitialize]
        public void TestInitializeAttribute()
        {
            fileSystem = new MockFileSystem();

            systemStateManager = new MockSystemStateManager(fileSystem);
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
                fileSystem.CreateDirectory(path);

                Assert.IsTrue(fileSystem.DirectoryExists(path));
            }

            Assert.IsFalse(fileSystem.DirectoryExists(path));
        }

        [TestMethod]
        public void RevertsDirectoryCreationWithChildren()
        {
            using (systemStateManager.SnapshotDirectory(path))
            {
                fileSystem.CreateDirectory(path);
                fileSystem.CreateDirectory(Path.Combine(path, "blah"));
                fileSystem.CreateFile(Path.Combine(path, "log.txt"));
            }

            Assert.IsFalse(fileSystem.DirectoryExists(path));
        }

        [TestMethod]
        public void RevertsDirectoryDeletion()
        {
            fileSystem.CreateDirectory(path);

            using (systemStateManager.SnapshotDirectory(path))
            {
                fileSystem.DeleteDirectory(path, recursive: true);

                Assert.IsFalse(fileSystem.DirectoryExists(path));
            }

            Assert.IsTrue(fileSystem.DirectoryExists(path));
        }
    }
}
