using BloqueoYUsbDetectionApp;  // Asegúrate de agregar esto si no está presente
namespace BloqueoYUsbDetectionApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Configurar visualización de la aplicación
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Ejecutar el formulario principal
            Application.Run(new Form1());
        }
    }
}
