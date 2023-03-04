using DevOptimal.SystemStateManager.Persistence.SQLite.Environment;
using DevOptimal.SystemStateManager.Persistence.SQLite.FileSystem.Caching;
using DevOptimal.SystemUtilities.Environment;
using DevOptimal.SystemUtilities.FileSystem;
using DevOptimal.SystemUtilities.Registry;
using System;
using System.Collections.Generic;
using System.IO;

namespace DevOptimal.SystemStateManager.Persistence.SQLite
{
    public class PersistentSystemStateManager : SystemStateManager
    {
        public static Uri PersistenceURI { get; set; } = new Uri(
            Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData),
                nameof(SystemStateManager),
                $"{nameof(Persistence)}.sqlite"));

        public PersistentSystemStateManager()
            : this(new DefaultEnvironment(), new DefaultFileSystem(), new DefaultRegistry())
        {
        }

        public PersistentSystemStateManager(IEnvironment environment, IFileSystem fileSystem, IRegistry registry)
            : base(new SQLiteFileCache(fileSystem), environment, fileSystem, registry)
        {
        }

        internal PersistentSystemStateManager(List<ISnapshot> snapshots)
            : base(snapshots, new SQLiteFileCache(new DefaultFileSystem()))
        {
        }

        protected override ISnapshot CreateEnvironmentVariableSnapshot(string id, string name, EnvironmentVariableTarget target, IEnvironment environment)
        {
            var originator = new PersistentEnvironmentVariableOriginator(name, target, environment);
            return new PersistentEnvironmentVariableCaretaker(id, originator);
        }
    }
}
