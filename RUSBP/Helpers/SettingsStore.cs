using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RUSBP.Helpers
{
    public static class SettingsStore
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RUSBP", "settings.dat");

        public static void Save(string rpRoot, string backendIp)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var data = $"{rpRoot}\n{backendIp}";
            var bytes = Encoding.UTF8.GetBytes(data);
            var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(FilePath, encrypted);
        }

        public static (string rpRoot, string backendIp)? Load()
        {
            if (!File.Exists(FilePath)) return null;
            var encrypted = File.ReadAllBytes(FilePath);
            var bytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            var parts = Encoding.UTF8.GetString(bytes).Split('\n');
            if (parts.Length >= 2)
                return (parts[0], parts[1]);
            return null;
        }

        public static void Clear()
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }
    }
}
