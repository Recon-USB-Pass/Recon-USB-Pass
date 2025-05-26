using System;
using System.Management;

namespace RUSBP.Helpers
{
    public static class BitLockerStatus
    {
        /// <summary>
        /// Devuelve true si la unidad tiene BitLocker y está *bloqueada*.
        /// </summary>
        public static bool IsLocked(string driveLetter)
        {
            try
            {
                if (driveLetter.EndsWith("\\")) driveLetter = driveLetter.Substring(0, driveLetter.Length - 1);
                if (!driveLetter.EndsWith(":")) driveLetter += ":";

                using var searcher = new ManagementObjectSearcher(
                    @"root\CIMV2\Security\MicrosoftVolumeEncryption",
                    "SELECT * FROM Win32_EncryptableVolume");

                foreach (ManagementObject vol in searcher.Get())
                {
                    string? letter = vol["DriveLetter"]?.ToString();
                    if (string.Equals(letter, driveLetter, StringComparison.OrdinalIgnoreCase))
                    {
                        var protectionStatus = Convert.ToUInt32(vol["ProtectionStatus"]);
                        // LockStatus a veces puede venir vacío: revisa directorios después de desbloquear
                        var lockStatusObj = vol["LockStatus"];
                        int lockStatus = lockStatusObj != null && int.TryParse(lockStatusObj.ToString(), out var v) ? v : -1;

                        // BitLocker activo (protectionStatus == 2), y bloqueado si lockStatus == 1
                        return (protectionStatus == 2) && (lockStatus == 1);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error BitLockerStatus (IsLocked): {ex.Message}", "BitLockerStatus", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return false;
        }
    }
}
