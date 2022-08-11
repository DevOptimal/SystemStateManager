using DevOptimal.SystemUtilities.Environment;
using DevOptimal.SystemUtilities.FileSystem;
using DevOptimal.SystemUtilities.Registry;

namespace DevOptimal.SystemStateManager.Tests
{
    [TestClass]
    public abstract class TestBase
    {
        protected IEnvironment environment;
        protected IFileSystem fileSystem;
        protected IRegistry registry;

        [TestInitialize]
        public void TestInitialize()
        {
            environment = new MockEnvironment();
            fileSystem = new MockFileSystem();
            registry = new MockRegistry();
        }

        protected SystemStateManager CreateSystemStateManager() => new(environment, fileSystem, registry);
    }
}
