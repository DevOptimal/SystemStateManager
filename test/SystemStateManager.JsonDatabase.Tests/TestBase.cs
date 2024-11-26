using DevOptimal.SystemUtilities.Environment;
using DevOptimal.SystemUtilities.FileSystem;
using DevOptimal.SystemUtilities.Registry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevOptimal.SystemStateManager.JsonDatabase.Tests
{
    [TestClass]
    public abstract class TestBase
    {
        protected JsonDatabase database;
        protected IEnvironment environment;
        protected IFileSystem fileSystem;
        protected IRegistry registry;

        [TestInitialize]
        public void TestInitialize()
        {
            environment = new MockEnvironment();
            fileSystem = new MockFileSystem();
            registry = new MockRegistry();
            database = new JsonDatabase(environment, fileSystem, registry);
        }

        protected SystemStateManager CreateSystemStateManager() => new(database, environment, fileSystem, registry);
    }
}
