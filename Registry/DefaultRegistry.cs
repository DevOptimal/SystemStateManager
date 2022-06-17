using Microsoft.Win32;

namespace Registry
{
    public class DefaultRegistry : IRegistry
    {
        public void CreateRegistryKey(RegistryHive hive, RegistryView view, string subKey)
        {
            RegistryKey.OpenBaseKey(hive, view).CreateSubKey(subKey);
        }

        public void DeleteRegistryKey(RegistryHive hive, RegistryView view, string subKey)
        {
            RegistryKey.OpenBaseKey(hive, view).DeleteSubKey(subKey);
        }

        public void DeleteRegistryValue(RegistryHive hive, RegistryView view, string subKey, string name)
        {
            RegistryKey.OpenBaseKey(hive, view).OpenSubKey(subKey, writable: true).DeleteValue(name, throwOnMissingValue: false);
        }

        public (object value, RegistryValueKind kind) GetRegistryValue(RegistryHive hive, RegistryView view, string subKey, string name)
        {
            var regKey = RegistryKey.OpenBaseKey(hive, view).OpenSubKey(subKey);
            return (regKey?.GetValue(name), regKey?.GetValueKind(name) ?? RegistryValueKind.None);
        }

        public bool RegistryKeyExists(RegistryHive hive, RegistryView view, string subKey)
        {
            return RegistryKey.OpenBaseKey(hive, view).OpenSubKey(subKey) != null;
        }

        public bool RegistryValueExists(RegistryHive hive, RegistryView view, string subKey, string name)
        {
            return RegistryKey.OpenBaseKey(hive, view).OpenSubKey(subKey).GetValue(name) != null;
        }

        public void SetRegistryValue(RegistryHive hive, RegistryView view, string subKey, string name, object value, RegistryValueKind kind)
        {
            RegistryKey.OpenBaseKey(hive, view).OpenSubKey(subKey, writable: true).SetValue(name, value, kind);
        }
    }
}
