using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BloqueoPantalla
{
    public partial class Form1 : Form
    {
        // NÃºmero mÃ¡ximo de intentos fallidos
        private const int MaxIntentos = 3;
        private int intentosFallidos = 0;
        private const string PinValido = "1234"; // PIN predefinido
        private readonly string rutaArchivoLlaveUsb = @"C:\ruta\al\archivo\de\llave.txt"; // Ruta del archivo que indica la presencia de la llave USB

        public Form1()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Inicializar controles
            TextBox txtPin = new TextBox
            {
                Name = "txtPin",
                PasswordChar = '*',
                Location = new Point(this.Width / 2 - 100, this.Height / 2 - 20),
                Width = 200
            };
            Button btnValidar = new Button
            {
                Name = "btnValidar",
                Text = "Validar",
                Location = new Point(this.Width / 2 - 100, this.Height / 2 + 20),
                Width = 200
            };
            btnValidar.Click += BtnValidar_Click;
            this.Controls.Add(txtPin);
            this.Controls.Add(btnValidar);

            // Restringir el cursor
            RestrictCursor();
        }

        private void BtnValidar_Click(object sender, EventArgs e)
        {
            TextBox txtPin = this.Controls["txtPin"] as TextBox;
            if (txtPin != null)
            {
                string pinIngresado = txtPin.Text;
                if (pinIngresado == PinValido && VerificarLlaveUsb())
                {
                    MessageBox.Show("Acceso concedido.");
                    this.Close(); // Cierra la aplicaciÃ³n de bloqueo
                }
                else
                {
                    intentosFallidos++;
                    MessageBox.Show($"PIN o llave USB incorrectos. Intentos restantes: {MaxIntentos - intentosFallidos}");
                    if (intentosFallidos >= MaxIntentos)
                    {
                        MessageBox.Show("NÃºmero mÃ¡ximo de intentos alcanzado. Apagando el sistema.");
                        ApagarSistema();
                    }
                }
            }
        }

        private bool VerificarLlaveUsb()
        {
            // Verifica si el archivo que indica la presencia de la llave USB existe
            return File.Exists(rutaArchivoLlaveUsb);
        }

        private void ApagarSistema()
        {
            // Ejecuta el comando para apagar el sistema
            System.Diagnostics.Process.Start("shutdown", "/s /f /t 0");
        }

        private void RestrictCursor()
        {
            // Define el Ã¡rea permitida para el cursor
            Rectangle areaPermitida = new Rectangle(100, 100, 800, 600); // Ajuste segÃºn sea necesario
            ClipCursor(ref areaPermitida);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Cancela el intento de cierre del formulario
            e.Cancel = true;
            MessageBox.Show("Esta ventana no puede cerrarse hasta que se complete la validaciÃ³n.");
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Lista de combinaciones de teclas a bloquear
            Keys[] teclasBloqueadas = new Keys[]
            {
                Keys.Alt | Keys.F4, // Alt + F4
                Keys.LWin | Keys.Down, // Win + Down
                Keys.Alt, // Alt
                Keys.Tab // Tab
            };

            if (teclasBloqueadas.Contains(keyData))
            {
                return true; // Ignora la tecla
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        [DllImport("user32.dll")]
        private static extern bool ClipCursor(ref Rectangle lpRect);

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            // Liberar el cursor
            ClipCursor(ref new Rectangle(0, 0, 0, 0));
        }
    }
}