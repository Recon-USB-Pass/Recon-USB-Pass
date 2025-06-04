using System;
using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;

namespace RUSBP.Helpers
{
    public static class BitLockerStatus
    {
        /// <summary>
        /// Devuelve true si la unidad tiene BitLocker y está *bloqueada*.
        /// </summary>
        /// <summary>Devuelve true si el volumen está bloqueado; false si está desbloqueado o no se pudo determinar.</summary>
        public static bool IsLocked(string driveLetter)
        {
            try
            {
                string dl = driveLetter.Trim().TrimEnd('\\').TrimEnd(':') + ":";

                var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "manage-bde.exe",
                    Arguments = $"-status {dl}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                })!;

                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(3000);

                // Ejemplo de línea relevante:
                // "    Estado de bloqueo:      Bloqueado"
                var match = Regex.Match(output, @"Estado de bloqueo:\s+(Bloqueado|Desbloqueado)", RegexOptions.IgnoreCase);
                if (!match.Success) return false;                 // <-- no encontrado ⇒ asumimos desbloqueado

                return match.Groups[1].Value.StartsWith("Bloqueado", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                // Cualquier error ⇒ asumimos desbloqueado y NO lanzamos UI
                return false;
            }
        }
    }
}
