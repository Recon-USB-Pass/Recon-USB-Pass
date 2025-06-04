// RUSBP/Program.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using RUSBP.Core;
using RUSBP.Forms;
using RUSBP.Helpers;

namespace RUSBP
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // ───────────────────── 0. Mutex ─────────────────────
            using var _ = new Mutex(initiallyOwned: true, "RUSBP_USB_LOCK_AGENT", out bool created);
            if (!created) return;     // ya hay otro agente

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                string rpRoot, backendIp;

                /* ───────────── 1. Primera ejecución vs. posteriores ───────────── */
                var settings = SettingsStore.Load();
                if (settings is null)
                {
                    // Instalación: pide RP root, desbloquea unidad,
                    // descifra .btlk-ip y persiste settings.dat
                    (rpRoot, backendIp) = SetupFirstRunWithUsbRoot();
                    SettingsStore.Save(rpRoot, backendIp);
                }
                else
                {
                    rpRoot = settings.Value.rpRoot;
                    backendIp = settings.Value.backendIp;
                }

                //  RP_root queda disponible para todo el proceso
                UsbCryptoService.RpRootGlobal = rpRoot;

                /* ───────────── 2. DI container ───────────── */
                var services = new ServiceCollection();
                services.AddSingleton(new ApiClient(backendIp));
                services.AddSingleton<UsbCryptoService>();
                services.AddSingleton<LogSyncService>();

                using var sp = services.BuildServiceProvider();

                /* ───────────── 3. Arranca pantalla de login ───────────── */
                var login = new LoginForm(
                    sp.GetRequiredService<ApiClient>(),
                    sp.GetRequiredService<UsbCryptoService>(),
                    null,
                    sp.GetRequiredService<LogSyncService>());

                Application.Run(login);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fatal al iniciar la aplicación:\n\n{ex}",
                                "Error crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Flujo de instalación: pide RP_root, desbloquea la unidad ROOT,
        /// descifra .btlk-ip y devuelve (rpRoot, backendIp).
        /// </summary>
        private static (string rpRoot, string backendIp) SetupFirstRunWithUsbRoot()
        {
            while (true)
            {
                /* 1️⃣ Detectar primer USB */
                var list = UsbCryptoService.EnumerateUsbInfos();
                if (list.Count == 0)
                {
                    MessageBox.Show("Conecta el USB ROOT cifrado (con rusbp.sys y pki).",
                                    "USB root requerido", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Thread.Sleep(1200);
                    continue;
                }
                var rootPath = list.First().Roots.First();   // ej.  F:\
                var driveLetter = rootPath[..2];               // “F:”

                /* 2️⃣ Pedir RecoveryPassword bonito */
                if (!Prompt.ForRecoveryPassword(out string rpRoot))
                {
                    MessageBox.Show("El RecoveryPassword es obligatorio.",
                                    "Operación cancelada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }

                /* 3️⃣ Desbloquear BitLocker con RP_root */
                if (!UsbCryptoService.UnlockBitLockerWithRecoveryPass(driveLetter, rpRoot))
                {
                    MessageBox.Show("No se pudo desbloquear la unidad con ese RecoveryPassword.",
                                    "Error BitLocker", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                /* 4️⃣ Esperar montaje */
                string sysDir = Path.Combine(rootPath, "rusbp.sys");
                string pkiDir = Path.Combine(rootPath, "pki");
                int waited = 0;
                while ((!Directory.Exists(sysDir) || !Directory.Exists(pkiDir)) && waited < 18_000)
                {
                    Thread.Sleep(1200);
                    waited += 1200;
                }
                if (!Directory.Exists(sysDir) || !Directory.Exists(pkiDir))
                {
                    MessageBox.Show("Estructura no encontrada tras el unlock.",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                /* 5️⃣ Descifrar .btlk-ip → backendIp */
                string btlkIpPath = Path.Combine(sysDir, ".btlk-ip");
                string backendIp = CryptoHelper.DecryptBtlkIp(btlkIpPath, rpRoot);
                if (string.IsNullOrWhiteSpace(backendIp))
                {
                    MessageBox.Show("No se pudo leer la IP del backend. RP_root incorrecto?",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                MessageBox.Show($"IP del backend guardada: {backendIp}",
                                "Instalación OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return (rpRoot, backendIp.Trim());
            }
        }
    }
}
