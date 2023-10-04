using DevOptimal.SystemStateManager.Environment;
using DevOptimal.SystemStateManager.FileSystem;
using DevOptimal.SystemStateManager.FileSystem.Caching;
using DevOptimal.SystemStateManager.Persistence.Environment;
using DevOptimal.SystemStateManager.Persistence.FileSystem;
using DevOptimal.SystemStateManager.Persistence.Registry;
using DevOptimal.SystemStateManager.Registry;
using DevOptimal.SystemUtilities.Environment;
using DevOptimal.SystemUtilities.FileSystem;
using DevOptimal.SystemUtilities.FileSystem.Extensions;
using DevOptimal.SystemUtilities.Registry;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Xml;

namespace DevOptimal.SystemStateManager.Persistence
{
    internal class DatabaseConnection : FileStream
    {
        public static FileInfo DefaultDatabaseFile { get; set; } = new FileInfo(
            Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData),
                nameof(SystemStateManager),
                nameof(Persistence) + ".json"));

        private readonly Mutex mutex;

        private readonly string targetFilePath;

        private readonly string backupFilePath;

        private readonly FileStream targetFileStream;

        private readonly List<IPersistentSnapshot> snapshots;

        private bool disposedValue;

        public DatabaseConnection()
            : this(new DefaultEnvironment(), new DefaultFileSystem(), new DefaultRegistry())
        { }

        public DatabaseConnection(IEnvironment environment, IFileSystem fileSystem, IRegistry registry)
            : this(DefaultDatabaseFile, new LocalFileCache(DefaultDatabaseFile.Directory.GetDirectory(nameof(LocalFileCache)), fileSystem), environment, fileSystem, registry)
        { }

        public DatabaseConnection(IFileCache fileCache, IEnvironment environment, IFileSystem fileSystem, IRegistry registry)
            : this(DefaultDatabaseFile, fileCache, environment, fileSystem, registry)
        { }

        public DatabaseConnection(FileInfo databaseFile, IFileCache fileCache, IEnvironment environment, IFileSystem fileSystem, IRegistry registry)
            : this(databaseFile, fileCache, environment, fileSystem, registry, Timeout.InfiniteTimeSpan)
        { }

        public DatabaseConnection(FileInfo databaseFile, IFileCache fileCache, IEnvironment environment, IFileSystem fileSystem, IRegistry registry, TimeSpan timeout)
            : this(GetFileMutex(databaseFile, timeout), GetTargetFilePath(databaseFile), GetBackupFilePath(databaseFile), fileCache, environment, fileSystem, registry)
        { }

        private DatabaseConnection(Mutex mutex, string targetFilePath, string backupFilePath, IFileCache fileCache, IEnvironment environment, IFileSystem fileSystem, IRegistry registry)
            : base(backupFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)
        {
            this.mutex = mutex;
            this.targetFilePath = targetFilePath;
            this.backupFilePath = backupFilePath;

            // Load snapshots into memory
            targetFileStream = new FileStream(targetFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            snapshots = new List<IPersistentSnapshot>();
            if (targetFileStream.Length > 0 )
            {
                var settings = new XmlReaderSettings
                {
                    IgnoreComments = true,
                    IgnoreWhitespace = true,
                    IgnoreProcessingInstructions = true,
                };

                using (var reader = XmlReader.Create(targetFileStream, settings))
                {
                    var empty = reader.IsEmptyElement;

                    reader.ReadStartElement("Caretakers");

                    if (!empty)
                    {
                        while (reader.NodeType == XmlNodeType.Element)
                        {
                            IPersistentSnapshot snapshot;

                            var id = reader.GetAttribute(nameof(IPersistentSnapshot.ID));
                            var processID = int.Parse(reader.GetAttribute(nameof(IPersistentSnapshot.ProcessID)));
                            var processStartTime = new DateTime(long.Parse(reader.GetAttribute(nameof(IPersistentSnapshot.ProcessStartTime))));

                            if (reader.IsEmptyElement)
                            {
                                throw new XmlException();
                            }

                            switch (reader.LocalName)
                            {
                                case nameof(PersistentEnvironmentVariableCaretaker):
                                    reader.ReadStartElement(nameof(PersistentEnvironmentVariableCaretaker));

                                    if (reader.LocalName != nameof(EnvironmentVariableOriginator))
                                    {
                                        throw new XmlException();
                                    }

                                    if (!reader.IsEmptyElement)
                                    {
                                        throw new XmlException();
                                    }

                                    var environmentVariableName = reader.GetAttribute(nameof(EnvironmentVariableOriginator.Name));
                                    var environmentVariableTarget = (EnvironmentVariableTarget)Enum.Parse(typeof(EnvironmentVariableTarget), reader.GetAttribute(nameof(EnvironmentVariableOriginator.Target)));
                                    var environmentVariableOriginator = new EnvironmentVariableOriginator(environmentVariableName, environmentVariableTarget, environment);

                                    reader.ReadStartElement(nameof(EnvironmentVariableOriginator));

                                    if (reader.LocalName != nameof(EnvironmentVariableMemento))
                                    {
                                        throw new XmlException();
                                    }

                                    if (!reader.IsEmptyElement)
                                    {
                                        throw new XmlException();
                                    }

                                    var environmentVariableValue = reader.GetAttribute(nameof(EnvironmentVariableMemento.Value));
                                    var environmentVariableMemento = new EnvironmentVariableMemento
                                    {
                                        Value = environmentVariableValue,
                                    };

                                    reader.ReadStartElement(nameof(EnvironmentVariableMemento));

                                    snapshot = new PersistentEnvironmentVariableCaretaker(id, processID, processStartTime, environmentVariableOriginator, environmentVariableMemento);
                                    break;
                                case nameof(PersistentDirectoryCaretaker):
                                    reader.ReadStartElement(nameof(PersistentDirectoryCaretaker));

                                    if (reader.LocalName != nameof(DirectoryOriginator))
                                    {
                                        throw new XmlException();
                                    }

                                    if (!reader.IsEmptyElement)
                                    {
                                        throw new XmlException();
                                    }

                                    var directoryPath = reader.GetAttribute(nameof(DirectoryOriginator.Path));
                                    var directoryOriginator = new DirectoryOriginator(directoryPath, fileSystem);

                                    reader.ReadStartElement(nameof(DirectoryOriginator));

                                    if (reader.LocalName != nameof(DirectoryMemento))
                                    {
                                        throw new XmlException();
                                    }

                                    if (!reader.IsEmptyElement)
                                    {
                                        throw new XmlException();
                                    }

                                    var directoryExists = bool.Parse(reader.GetAttribute(nameof(DirectoryMemento.Exists)));
                                    var directoryMemento = new DirectoryMemento
                                    {
                                        Exists = directoryExists,
                                    };

                                    reader.ReadStartElement(nameof(DirectoryMemento));

                                    snapshot = new PersistentDirectoryCaretaker(id, processID, processStartTime, directoryOriginator, directoryMemento);
                                    break;
                                case nameof(PersistentFileCaretaker):
                                    reader.ReadStartElement(nameof(PersistentFileCaretaker));

                                    if (reader.LocalName != nameof(FileOriginator))
                                    {
                                        throw new XmlException();
                                    }

                                    if (!reader.IsEmptyElement)
                                    {
                                        throw new XmlException();
                                    }

                                    var filePath = reader.GetAttribute(nameof(FileOriginator.Path));
                                    var fileOriginator = new FileOriginator(filePath, fileCache, fileSystem);

                                    reader.ReadStartElement(nameof(FileOriginator));

                                    if (reader.LocalName != nameof(FileMemento))
                                    {
                                        throw new XmlException();
                                    }

                                    if (!reader.IsEmptyElement)
                                    {
                                        throw new XmlException();
                                    }

                                    var fileHash = reader.GetAttribute(nameof(FileMemento.Hash));
                                    var fileMemento = new FileMemento
                                    {
                                        Hash = fileHash,
                                    };

                                    reader.ReadStartElement(nameof(FileMemento));

                                    snapshot = new PersistentFileCaretaker(id, processID, processStartTime, fileOriginator, fileMemento);
                                    break;
                                case nameof(PersistentRegistryKeyCaretaker):
                                    reader.ReadStartElement(nameof(PersistentRegistryKeyCaretaker));

                                    if (reader.LocalName != nameof(RegistryKeyOriginator))
                                    {
                                        throw new XmlException();
                                    }

                                    if (!reader.IsEmptyElement)
                                    {
                                        throw new XmlException();
                                    }

                                    var registryKeyHive = (RegistryHive)Enum.Parse(typeof(RegistryHive), reader.GetAttribute(nameof(RegistryKeyOriginator.Hive)));
                                    var registryKeyView = (RegistryView)Enum.Parse(typeof(RegistryView), reader.GetAttribute(nameof(RegistryKeyOriginator.View)));
                                    var registryKeySubKey = reader.GetAttribute(nameof(RegistryKeyOriginator.SubKey));
                                    var registryKeyOriginator = new RegistryKeyOriginator(registryKeyHive, registryKeyView, registryKeySubKey, registry);

                                    reader.ReadStartElement(nameof(RegistryKeyOriginator));

                                    if (reader.LocalName != nameof(RegistryKeyMemento))
                                    {
                                        throw new XmlException();
                                    }

                                    if (!reader.IsEmptyElement)
                                    {
                                        throw new XmlException();
                                    }

                                    var registryKeyExists = bool.Parse(reader.GetAttribute(nameof(RegistryKeyMemento.Exists)));
                                    var registryKeyMemento = new RegistryKeyMemento
                                    {
                                        Exists = registryKeyExists,
                                    };

                                    reader.ReadStartElement(nameof(RegistryKeyMemento));

                                    snapshot = new PersistentRegistryKeyCaretaker(id, processID, processStartTime, registryKeyOriginator, registryKeyMemento);
                                    break;
                                case nameof(PersistentRegistryValueCaretaker):
                                    reader.ReadStartElement(nameof(PersistentRegistryValueCaretaker));

                                    if (reader.LocalName != nameof(RegistryValueOriginator))
                                    {
                                        throw new XmlException();
                                    }

                                    if (!reader.IsEmptyElement)
                                    {
                                        throw new XmlException();
                                    }

                                    var registryValueHive = (RegistryHive)Enum.Parse(typeof(RegistryHive), reader.GetAttribute(nameof(RegistryValueOriginator.Hive)));
                                    var registryValueView = (RegistryView)Enum.Parse(typeof(RegistryView), reader.GetAttribute(nameof(RegistryValueOriginator.View)));
                                    var registryValueSubKey = reader.GetAttribute(nameof(RegistryValueOriginator.SubKey));
                                    var registryValueName = reader.GetAttribute(nameof(RegistryValueOriginator.Name));
                                    var registryValueOriginator = new RegistryValueOriginator(registryValueHive, registryValueView, registryValueSubKey, registryValueName, registry);

                                    reader.ReadStartElement(nameof(RegistryValueOriginator));

                                    if (reader.LocalName != nameof(RegistryValueMemento))
                                    {
                                        throw new XmlException();
                                    }

                                    if (!reader.IsEmptyElement)
                                    {
                                        throw new XmlException();
                                    }

                                    var registryValueValue = ConvertBytesToValue(Convert.FromBase64String(reader.GetAttribute(nameof(RegistryValueMemento.Value))));
                                    var registryValueKind = (RegistryValueKind)Enum.Parse(typeof(RegistryValueKind), reader.GetAttribute(nameof(RegistryValueMemento.Kind)));
                                    var registryValueMemento = new RegistryValueMemento
                                    {
                                        Value = registryValueValue,
                                        Kind = registryValueKind,
                                    };

                                    reader.ReadStartElement(nameof(RegistryValueMemento));

                                    snapshot = new PersistentRegistryValueCaretaker(id, processID, processStartTime, registryValueOriginator, registryValueMemento);
                                    break;
                                default:
                                    throw new XmlException();
                            }

                            reader.ReadEndElement();

                            snapshots.Add(snapshot);
                        }

                        reader.ReadEndElement();
                    }
                }
            }

            // Clear out the backup file stream
            SetLength(0);
            Flush();
        }

        public void Add(IPersistentSnapshot snapshot)
        {
            if (snapshots.SingleOrDefault(s => s.ID == snapshot.ID) != null)
            {
                throw new ResourceLockedException($"The resource '{snapshot.ID}' is locked by another instance.");
            }
            snapshots.Add(snapshot);
        }

        public IEnumerable<IPersistentSnapshot> List()
        {
            return snapshots;
        }

        public void Remove(IPersistentSnapshot snapshot)
        {
            var existingSnapshot = snapshots.SingleOrDefault(s => s.ID == snapshot.ID);
            if (existingSnapshot == null)
            {
                throw new KeyNotFoundException($"A snapshot with the ID '{snapshot.ID}' was not found in the database.");
            }
            snapshots.Remove(existingSnapshot);
        }

        public void ReadXml(XmlReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteXml(XmlWriter writer)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposedValue)
            {
                if (disposing)
                {
                    // Write snapshots to the backup file
                    var settings = new XmlWriterSettings
                    {
                        
                    };
                    targetFileStream.Position = 0;
                    using (var writer = XmlWriter.Create(targetFileStream, settings))
                    {
                        writer.WriteStartElement("Caretakers");

                        foreach (var snapshot in snapshots)
                        {
                            writer.WriteStartElement(snapshot.GetType().Name);
                            writer.WriteAttributeString(nameof(IPersistentSnapshot.ID), snapshot.ID);
                            writer.WriteAttributeString(nameof(IPersistentSnapshot.ProcessID), snapshot.ProcessID.ToString());
                            writer.WriteAttributeString(nameof(IPersistentSnapshot.ID), snapshot.ProcessStartTime.Ticks.ToString());

                            switch (snapshot)
                            {
                                case PersistentEnvironmentVariableCaretaker persistentEnvironmentVariableCaretaker:
                                    var environmentVariableOriginator = persistentEnvironmentVariableCaretaker.Originator;
                                    writer.WriteStartElement(nameof(EnvironmentVariableOriginator));
                                    writer.WriteAttributeString(nameof(EnvironmentVariableOriginator.Name), environmentVariableOriginator.Name);
                                    writer.WriteAttributeString(nameof(EnvironmentVariableOriginator.Target), environmentVariableOriginator.Target.ToString());
                                    writer.WriteEndElement();

                                    var environmentVariableMemento = persistentEnvironmentVariableCaretaker.Memento;
                                    writer.WriteStartElement(nameof(EnvironmentVariableMemento));
                                    writer.WriteAttributeString(nameof(EnvironmentVariableMemento.Value), environmentVariableMemento.Value.ToString());
                                    writer.WriteEndElement();
                                    break;
                                case PersistentDirectoryCaretaker persistentDirectoryCaretaker:
                                    var directoryOriginator = persistentDirectoryCaretaker.Originator;
                                    writer.WriteStartElement(nameof(DirectoryOriginator));
                                    writer.WriteAttributeString(nameof(DirectoryOriginator.Path), directoryOriginator.Path);
                                    writer.WriteEndElement();

                                    var directoryMemento = persistentDirectoryCaretaker.Memento;
                                    writer.WriteStartElement(nameof(DirectoryMemento));
                                    writer.WriteAttributeString(nameof(DirectoryMemento.Exists), directoryMemento.Exists.ToString());
                                    writer.WriteEndElement();
                                    break;
                                case PersistentFileCaretaker persistentFileCaretaker:
                                    var fileOriginator = persistentFileCaretaker.Originator;
                                    writer.WriteStartElement(nameof(FileOriginator));
                                    writer.WriteAttributeString(nameof(FileOriginator.Path), fileOriginator.Path);
                                    writer.WriteEndElement();

                                    var fileMemento = persistentFileCaretaker.Memento;
                                    writer.WriteStartElement(nameof(FileMemento));
                                    writer.WriteAttributeString(nameof(FileMemento.Hash), fileMemento.Hash.ToString());
                                    writer.WriteEndElement();
                                    break;
                                case PersistentRegistryKeyCaretaker persistentRegistryKeyCaretaker:
                                    var registryKeyOriginator = persistentRegistryKeyCaretaker.Originator;
                                    writer.WriteStartElement(nameof(RegistryKeyOriginator));
                                    writer.WriteAttributeString(nameof(RegistryKeyOriginator.Hive), registryKeyOriginator.Hive.ToString());
                                    writer.WriteAttributeString(nameof(RegistryKeyOriginator.View), registryKeyOriginator.View.ToString());
                                    writer.WriteAttributeString(nameof(RegistryKeyOriginator.SubKey), registryKeyOriginator.SubKey);
                                    writer.WriteEndElement();

                                    var registryKeyMemento = persistentRegistryKeyCaretaker.Memento;
                                    writer.WriteStartElement(nameof(RegistryKeyMemento));
                                    writer.WriteAttributeString(nameof(RegistryKeyMemento.Exists), registryKeyMemento.Exists.ToString());
                                    writer.WriteEndElement();
                                    break;
                                case PersistentRegistryValueCaretaker persistentRegistryValueCaretaker:
                                    var registryValueOriginator = persistentRegistryValueCaretaker.Originator;
                                    writer.WriteStartElement(nameof(RegistryValueOriginator));
                                    writer.WriteAttributeString(nameof(RegistryValueOriginator.Hive), registryValueOriginator.Hive.ToString());
                                    writer.WriteAttributeString(nameof(RegistryValueOriginator.View), registryValueOriginator.View.ToString());
                                    writer.WriteAttributeString(nameof(RegistryValueOriginator.SubKey), registryValueOriginator.SubKey);
                                    writer.WriteAttributeString(nameof(RegistryValueOriginator.Name), registryValueOriginator.Name);
                                    writer.WriteEndElement();

                                    var registryValueMemento = persistentRegistryValueCaretaker.Memento;
                                    writer.WriteStartElement(nameof(RegistryValueMemento));
                                    writer.WriteAttributeString(nameof(RegistryValueMemento.Value), Convert.ToBase64String(ConvertValueToBytes(registryValueMemento.Value)));
                                    writer.WriteAttributeString(nameof(RegistryValueMemento.Kind), registryValueMemento.Kind.ToString());
                                    writer.WriteEndElement();
                                    break;
                            }

                            writer.WriteEndElement();
                        }

                        writer.WriteEndElement();
                    }

                    // Overwrite the target file with the backup file
                    targetFileStream.Dispose();
                    File.Copy(backupFilePath, targetFilePath, overwrite: true);
                    File.Delete(backupFilePath);

                    mutex.ReleaseMutex();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        private static Mutex GetFileMutex(FileInfo databaseFile, TimeSpan timeout)
        {
            // Get normalized file path
            var databaseFilePath = Path.GetFullPath(databaseFile.FullName).Replace('\\', '/');
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                databaseFilePath = databaseFilePath.ToLower();
            }

            // unique id for global mutex - Global prefix means it is global to the machine
            var mutexId = $"Global\\{nameof(PersistentSystemStateManager)}:/{databaseFilePath}";

            Mutex mutex;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // edited by Jeremy Wiebe to add example of setting up security for multi-user usage
                // edited by 'Marc' to work also on localized systems (don't use just "Everyone") 
                var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, domainSid: null), MutexRights.FullControl, AccessControlType.Allow);
                var securitySettings = new MutexSecurity();
                securitySettings.AddAccessRule(allowEveryoneRule);

                // edited by MasonGZhwiti to prevent race condition on security settings via VanNguyen
#if NETSTANDARD2_0
                mutex = MutexAcl.Create(false, mutexId, out var createdNew, securitySettings);
#else
                mutex = new Mutex(false, mutexId, out var createdNew, securitySettings);
#endif
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                mutex = new Mutex(false, mutexId, out var createdNew);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            try
            {
                if (!mutex.WaitOne(timeout, false))
                {
                    throw new TimeoutException("Timeout waiting for exclusive access");
                }
            }
            catch (AbandonedMutexException)
            {
                // Log the fact that the mutex was abandoned in another process,
                // it will still get acquired
            }

            return mutex;
        }

        private static string GetTargetFilePath(FileInfo targetFile)
        {
            var directory = targetFile.Directory;
            if (!directory.Exists)
            {
                directory.Create();
            }
            return targetFile.FullName;
        }

        private static string GetBackupFilePath(FileInfo databaseFile)
        {
            return $"{databaseFile.FullName}.backup";
        }

        private static byte[] ConvertValueToBytes(object value)
        {
            if (value == null)
            {
                return null;
            }

            var result = new List<byte>();
            switch (value)
            {
                case byte[] byteValue:
                    result.Add(0x0);
                    result.AddRange(byteValue);
                    break;
                case int intValue:
                    result.Add(0x1);
                    result.AddRange(BitConverter.GetBytes(intValue));
                    break;
                case long longValue:
                    result.Add(0x2);
                    result.AddRange(BitConverter.GetBytes(longValue));
                    break;
                case string stringValue:
                    result.Add(0x3);
                    result.AddRange(Encoding.ASCII.GetBytes(stringValue));
                    break;
                case string[] stringArrayValue:
                    result.Add(0x4);
                    foreach (var stringValue in stringArrayValue)
                    {
                        result.AddRange(Encoding.ASCII.GetBytes(stringValue));
                        result.Add(0x0);
                    }
                    break;
                default:
                    throw new NotSupportedException($"{value.GetType().Name} is not a supported registry type.");
            }
            return result.ToArray();
        }

        private static object ConvertBytesToValue(byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }

            switch (bytes[0])
            {
                case 0x0:
                    return bytes.Skip(1).ToArray();
                case 0x1:
                    return BitConverter.ToInt32(bytes, 1);
                case 0x2:
                    return BitConverter.ToInt64(bytes, 1);
                case 0x3:
                    return Encoding.ASCII.GetString(bytes, 1, bytes.Length - 1);
                case 0x4:
                    var result = new List<string>();
                    var currentStartIndex = 1;
                    for (var i = currentStartIndex; i < bytes.Length; i++)
                    {
                        if (bytes[i] == 0x0)
                        {
                            result.Add(Encoding.ASCII.GetString(bytes, currentStartIndex, i - currentStartIndex));
                            currentStartIndex = i + 1;
                        }
                    }
                    return result.ToArray();
                default:
                    throw new NotSupportedException($"Unknown type byte.");
            }
        }
    }
}
