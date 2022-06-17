using Microsoft.Win32;

namespace Registry
{
    public interface IRegistry
    {
        void CreateRegistryKey(RegistryHive hive, RegistryView view, string subKey);

        void DeleteRegistryKey(RegistryHive hive, RegistryView view, string subKey);

        void DeleteRegistryValue(RegistryHive hive, RegistryView view, string subKey, string name);

        (object value, RegistryValueKind kind) GetRegistryValue(RegistryHive hive, RegistryView view, string subKey, string name);

        bool RegistryKeyExists(RegistryHive hive, RegistryView view, string subKey);

        bool RegistryValueExists(RegistryHive hive, RegistryView view, string subKey, string name);

        void SetRegistryValue(RegistryHive hive, RegistryView view, string subKey, string name, object value, RegistryValueKind kind);
    }
}
