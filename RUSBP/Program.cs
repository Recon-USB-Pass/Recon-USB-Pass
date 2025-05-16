using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using RUSBP.Core;
using RUSBP.Forms;

namespace RUSBP
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Mutex para evitar doble instancia
            bool createdNew;
            using (var mutex = new Mutex(true, "RUSBP_USB_LOCK_AGENT", out createdNew))
            {
                if (!createdNew)
                {
                    // Ya existe una instancia, termina el proceso
                    return;
                }

                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Dependency Injection
                var services = new ServiceCollection();
                services.AddSingleton(new ApiClient("https://10.145.0.45:8443"));
                services.AddSingleton<UsbCryptoService>();
                services.AddSingleton<LogSyncService>();

                var sp = services.BuildServiceProvider();

                var login = new LoginForm(
                    sp.GetRequiredService<ApiClient>(),
                    sp.GetRequiredService<UsbCryptoService>(),
                    null, // LogManager: será creado dinámicamente en el LoginForm
                    sp.GetRequiredService<LogSyncService>());

                Application.Run(login);
            }
        }
    }
}
