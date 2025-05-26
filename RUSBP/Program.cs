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
            bool createdNew;
            using (var mutex = new Mutex(true, "RUSBP_USB_LOCK_AGENT", out createdNew))
            {
                if (!createdNew)
                    return;

                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                try
                {
                    string rpRoot, backendIp;

                    // === SETUP INICIAL SOLO UNA VEZ ===
                    var settings = SettingsStore.Load();
                    if (settings == null)
                    {
                        (rpRoot, backendIp) = SetupFirstRunWithUsbRoot();
                        SettingsStore.Save(rpRoot, backendIp);
                    }
                    else
                    {
                        rpRoot = settings.Value.rpRoot;
                        backendIp = settings.Value.backendIp;
                    }

                    // --- Dependency Injection ---
                    var services = new ServiceCollection();
                    services.AddSingleton(new ApiClient(backendIp));
                    services.AddSingleton<UsbCryptoService>();
                    services.AddSingleton<LogSyncService>();

                    var sp = services.BuildServiceProvider();

                    var login = new LoginForm(
                        sp.GetRequiredService<ApiClient>(),
                        sp.GetRequiredService<UsbCryptoService>(),
                        null,
                        sp.GetRequiredService<LogSyncService>());

                    Application.Run(login);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Error fatal al iniciar la aplicación:\n\n" + ex.ToString(),
                        "Error crítico",
                        MessageBoxButtons.OK, MessageBoxIcon.Error
                    );
                }
            }
        }

        /// <summary>
        /// Primer uso: pide RecoveryPassword root y extrae la IP del backend.
        /// </summary>
        private static (string rpRoot, string backendIp) SetupFirstRunWithUsbRoot()
        {
            while (true)
            {
                // 1️⃣ Detectar primer USB conectado (puede estar cifrado)
                var usbList = UsbCryptoService.EnumerateUsbInfos();
                if (usbList.Count == 0)
                {
                    MessageBox.Show(
                        "Conecta el USB root cifrado (con rusbp.sys y pki).",
                        "USB root requerido",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    Thread.Sleep(1200);
                    continue;
                }
                var root = usbList.First().Roots.First();
                var driveLetter = root.Substring(0, 2);

                // 2️⃣ Pedir RecoveryPassword con UI bonita y validada
                if (!Prompt.ForRecoveryPassword(out string recoveryPassword))
                {
                    MessageBox.Show("El RecoveryPassword es obligatorio para instalar.", "Operación cancelada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }

                // 3️⃣ Intentar desbloquear BitLocker usando recoveryPass
                if (!UsbCryptoService.UnlockBitLockerWithRecoveryPass(driveLetter, recoveryPassword))
                {
                    MessageBox.Show("No se pudo desbloquear el USB root con ese RecoveryPassword. Intenta nuevamente.", "Error BitLocker", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                // 4️⃣ Esperar a que la unidad esté montada/accesible
                string sysDir = Path.Combine(root, "rusbp.sys");
                string pkiDir = Path.Combine(root, "pki");
                int maxWaitMs = 18000, waited = 0, sleepMs = 1200;
                while ((!Directory.Exists(sysDir) || !Directory.Exists(pkiDir)) && waited < maxWaitMs)
                {
                    Thread.Sleep(sleepMs);
                    waited += sleepMs;
                }
                if (!Directory.Exists(sysDir) || !Directory.Exists(pkiDir))
                {
                    MessageBox.Show("No se detecta la estructura después de desbloquear. Revisa que el USB esté montado y accesible.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                // 5️⃣ Extraer y validar la IP del backend desde el archivo cifrado
                string btlkIpPath = Path.Combine(sysDir, ".btlk-ip");
                string backendIp = CryptoHelper.DecryptBtlkIp(btlkIpPath, recoveryPassword);

                if (string.IsNullOrWhiteSpace(backendIp))
                {
                    MessageBox.Show("No se pudo leer la IP del backend desde el USB root. Asegúrate de ingresar la clave correcta.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                MessageBox.Show("IP del backend guardada exitosamente: " + backendIp, "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return (recoveryPassword, backendIp.Trim());
            }
        }
    }
}
