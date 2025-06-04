using System.Diagnostics;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using RUSBP.Helpers;
using System.IO;
using System.Windows.Forms;

namespace RUSBP.Core
{
    public class UsbCryptoService
    {
        public string? MountedRoot { get; set; }
        public string? Serial { get; private set; }
        public bool IsRoot { get; private set; }

        public bool TryLocateUsb()
        {
            foreach (var info in EnumerateUsbInfos())
            {
                foreach (var root in info.Roots)
                {
                    string pkiDir = Path.Combine(root, "pki");

                    /* ───── 1. ¿La unidad aún está cifrada? ───── */
                    if (!Directory.Exists(pkiDir))
                    {
                        string driveLetter = root[..2]; // “F:”

                        /* 1-a) Si YA tenemos la RP_root global => intentar unlock silencioso */
                        if (!string.IsNullOrWhiteSpace(RpRootGlobal) &&
                            UnlockBitLockerWithRecoveryPass(driveLetter, RpRootGlobal))
                        {
                            // damos tiempo a Windows a montar el volumen
                            Thread.Sleep(2500);
                            if (!Directory.Exists(pkiDir)) continue;
                        }
                        /* 1-b) Solo si no hay RpRootGlobal -> pedirla al usuario */
                        else
                        {
                            if (!Prompt.ForRecoveryPassword(out string rp))
                                continue;                               // canceló

                            RpRootGlobal = rp;                         // cache global
                            if (!UnlockBitLockerWithRecoveryPass(driveLetter, rp))
                            {
                                MessageBox.Show("No se pudo desbloquear el USB.",
                                    "Error BitLocker", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                continue;
                            }
                            Thread.Sleep(2500);
                            if (!Directory.Exists(pkiDir)) continue;
                        }
                    }

                    /* ───── 2. Estructura OK: asignar campos y salir ───── */
                    string sysDir = Path.Combine(root, "rusbp.sys");
                    bool hasRoot = File.Exists(Path.Combine(sysDir, ".btlk")) &&
                                   File.Exists(Path.Combine(sysDir, ".btlk-agente"));

                    Serial = info.Serial.ToUpperInvariant();
                    MountedRoot = root;
                    IsRoot = hasRoot;
                    return true;
                }
            }
            Serial = MountedRoot = null;
            IsRoot = false;
            return false;
        }


        public string LoadCertPem()
            => File.ReadAllText(Path.Combine(MountedRoot!, "pki", "cert.crt"));

        public string Sign(string challengeB64)
        {
            byte[] challenge = Convert.FromBase64String(challengeB64);
            string keyPem = File.ReadAllText(Path.Combine(MountedRoot!, "pki", "priv.key"));
            using var rsa = RSA.Create();
            rsa.ImportFromPem(keyPem);
            byte[] sig = rsa.SignData(challenge, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(sig);
        }

        public string? LoadConfigJson()
        {
            string path = Path.Combine(MountedRoot!, "config.json");
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        public string? GetSysDir() => MountedRoot is null ? null : Path.Combine(MountedRoot, "rusbp.sys");

        public string? LoadBackendIp(string recoveryPass)
        {
            var sysDir = GetSysDir();
            if (sysDir is null) return null;
            string btlkIpPath = Path.Combine(sysDir, ".btlk-ip");
            if (!File.Exists(btlkIpPath)) return null;
            return CryptoHelper.DecryptBtlkIp(btlkIpPath, recoveryPass);
        }

        // --- Desbloqueo BitLocker robusto ---
        public static bool UnlockBitLockerWithRecoveryPass(string driveLetter, string recoveryPass)
        {
            try
            {
                string normalized = driveLetter.Trim().TrimEnd('\\').TrimEnd(':') + ":";
                string args = $"-unlock {normalized} -RecoveryPassword {recoveryPass}";

                var proc = new Process();
                proc.StartInfo.FileName = "manage-bde.exe";
                proc.StartInfo.Arguments = args;
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;

                proc.Start();
                string output = proc.StandardOutput.ReadToEnd();
                string error = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                // Considera éxito si ya está desbloqueado (output o error)
                if (proc.ExitCode == 0 ||
                    output.Contains("ya está desbloqueado", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("ya está desbloqueado", StringComparison.OrdinalIgnoreCase) ||
                    output.Contains("already unlocked", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("already unlocked", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // Solo muestra error real
                MessageBox.Show($"Error al desbloquear BitLocker:\n{output}\n{error}", "BitLocker Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excepción al desbloquear BitLocker:\n{ex}", "BitLocker Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }


        public static List<UsbInfo> EnumerateUsbInfos()
        {
            var list = new List<UsbInfo>();
            var q = new ManagementObjectSearcher(
                "SELECT DeviceID, SerialNumber FROM Win32_DiskDrive WHERE InterfaceType='USB'");
            foreach (ManagementObject d in q.Get())
            {
                string serial = d["SerialNumber"]?.ToString()?.Trim() ?? "";
                if (serial.Length == 0) continue;
                var roots = new List<string>();
                foreach (ManagementObject part in d.GetRelated("Win32_DiskPartition"))
                    foreach (ManagementObject log in part.GetRelated("Win32_LogicalDisk"))
                        roots.Add(log["DeviceID"] + "\\");
                list.Add(new UsbInfo(serial, roots));
            }
            return list;
        }

        public record UsbInfo(string Serial, List<string> Roots);

        public static string SignWithKey(string privateKeyPem, string challengeB64)
        {
            byte[] challenge = Convert.FromBase64String(challengeB64);
            using var rsa = System.Security.Cryptography.RSA.Create();
            rsa.ImportFromPem(privateKeyPem);
            byte[] sig = rsa.SignData(challenge, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(sig);
        }
        // Contiene el RP_root descifrado (lo setea Program.cs al arrancar)
        public static string? RpRootGlobal { get; set; }

    }
}
