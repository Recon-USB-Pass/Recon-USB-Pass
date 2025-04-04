using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Bloqueo_C
{
    public partial class Form1 : Form
    {
        private const int MaxIntentos = 3;
        private int intentosFallidos = 0;
        private const string PinValido = "1234"; // PIN predefinido
        private bool pinValidado = false; // Variable de estado para indicar si el PIN fue validado

        // Constantes para el gancho de teclado
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        public Form1()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;

            // Configurar el gancho de teclado al iniciar el formulario
            _hookID = SetHook(_proc);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Inicializar controles
            TextBox txtPin = new TextBox
            {
                Name = "txtPin",
                PasswordChar = '*',
                Location = new Point(this.Width / 2 - 100, this.Height / 2 - 20),
                Width = 200,
                MaxLength = 4, // Aseguramos que el PIN tenga solo 4 dÃ­gitos
                TextAlign = HorizontalAlignment.Center // Centrado del texto
            };

            // Limitar a solo nÃºmeros
            txtPin.KeyPress += (s, ev) =>
            {
                if (!char.IsDigit(ev.KeyChar) && ev.KeyChar != (char)Keys.Back)
                {
                    ev.Handled = true; // Bloquea la entrada si no es un nÃºmero
                }
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
                if (pinIngresado == PinValido)
                {
                    MessageBox.Show("Acceso concedido.");
                    pinValidado = true; // Marcar como validado
                    Application.Exit(); // Cierra la aplicaciÃ³n completamente
                }
                else
                {
                    intentosFallidos++;
                    MessageBox.Show($"PIN incorrecto. Intentos restantes: {MaxIntentos - intentosFallidos}");
                    if (intentosFallidos >= MaxIntentos)
                    {
                        MessageBox.Show("NÃºmero mÃ¡ximo de intentos alcanzado. Apagando el sistema.");
                        ApagarSistema();
                    }
                }
            }
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
            RECT clipRect = new RECT
            {
                Left = areaPermitida.Left,
                Top = areaPermitida.Top,
                Right = areaPermitida.Right,
                Bottom = areaPermitida.Bottom
            };
            ClipCursor(ref clipRect);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Cancela el intento de cierre solo si el PIN no ha sido validado
            if (!pinValidado)
            {
                e.Cancel = true;
                MessageBox.Show("Esta ventana no puede cerrarse hasta que se complete la validaciÃ³n.");
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Lista de combinaciones de teclas a bloquear
            Keys[] teclasBloqueadas = new Keys[]
            {
                Keys.Alt | Keys.F4, // Alt + F4
                Keys.LWin | Keys.Down, // Win + Down
                Keys.Alt, // Alt
                Keys.Tab, // Tab
                Keys.R,   // R
                Keys.LWin, // Tecla Windows
                Keys.Down  // Flecha abajo
            };

            if (Array.Exists(teclasBloqueadas, key => key == keyData))
            {
                return true; // Ignora la tecla
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        [DllImport("user32.dll")]
        private static extern bool ClipCursor(ref RECT lpRect);

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            // Liberar el cursor
            RECT unrestrictedArea = new RECT
            {
                Left = 0,
                Top = 0,
                Right = 0,
                Bottom = 0
            };
            ClipCursor(ref unrestrictedArea);

            // Eliminar el gancho de teclado al cerrar el formulario
            UnhookWindowsHookEx(_hookID);
        }

        // DefiniciÃ³n de la estructura RECT para coincidir con la API de Windows
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // MÃ©todos para configurar el gancho de teclado
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (vkCode == (int)Keys.LWin || vkCode == (int)Keys.RWin)
                {
                    // Ignorar la tecla Windows
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}