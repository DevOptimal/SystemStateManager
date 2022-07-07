using DevOptimal.SystemUtilities.Environment;
using DevOptimal.SystemUtilities.FileSystem;
using DevOptimal.SystemUtilities.Registry;

namespace DevOptimal.SystemStateManager.Tests
{
    public class MockSystemStateManager : SystemStateManager
    {
        public MockSystemStateManager(MockEnvironment environment)
            : this(environment, new MockFileSystem(), new MockRegistry())
        {
        }

        public MockSystemStateManager(MockFileSystem fileSystem)
            : this(new MockEnvironment(), fileSystem, new MockRegistry())
        {
        }

        public MockSystemStateManager(MockRegistry registry)
            : this(new MockEnvironment(), new MockFileSystem(), registry)
        {
        }

        private MockSystemStateManager(
            MockEnvironment environment,
            MockFileSystem fileSystem,
            MockRegistry registry)
            : base(environment, fileSystem, registry)
        {
        }
    }
}
