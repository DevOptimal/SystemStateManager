using DevOptimal.SystemStateManager.Persistence.SQLite;
using DevOptimal.SystemUtilities.Environment;
using DevOptimal.SystemUtilities.FileSystem;
using DevOptimal.SystemUtilities.Registry;
using Microsoft.QualityTools.Testing.Fakes;
using System;
using System.Diagnostics.Fakes;
using System.IO;

namespace DevOptimal.SystemStateManager.Persistence.Tests
{
    [TestClass]
    public abstract class TestBase
    {
        protected IEnvironment environment;
        protected IFileSystem fileSystem;
        protected IRegistry registry;

        private readonly int fakeProcessID = System.Environment.ProcessId + 1;
        private readonly DateTime fakeProcessStartTime = DateTime.Now;

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext testContext)
        {
            PersistentSystemStateManager.PersistenceURI = new Uri(Path.Combine(testContext.ResultsDirectory, "persistence.db"));
        }

        [TestInitialize]
        public void TestInitialize()
        {
            environment = new MockEnvironment();
            fileSystem = new MockFileSystem();
            registry = new MockRegistry();
        }

        protected PersistentSystemStateManager CreatePersistentSystemStateManager() => new(environment, fileSystem, registry);

        protected IDisposable CreateShimsContext()
        {
            var context = ShimsContext.Create();

            ShimProcess.AllInstances.IdGet = p => fakeProcessID;
            ShimProcess.AllInstances.StartTimeGet = p => fakeProcessStartTime;

            return context;
        }
    }
}
