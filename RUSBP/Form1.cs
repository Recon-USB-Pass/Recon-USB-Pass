/*---------------------------------------------------------------
 Form1.cs  –  Bloqueo + llave USB (v7 con sólo validación de key.txt)
----------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bloqueo_USB
{
    public partial class Form1 : Form
    {
        /* ---------------- CONFIG ---------------- */
        private static readonly bool DEBUG_MODE = true;      // ← false en producción
        private const int    MAX_INTENTOS = 3;
        private const string PIN_VALIDO   = "1234";

        /* ---------------- ESTADO ---------------- */
        private int  _intentosFallidos  = 0;
        private bool _pinValidado       = false;
        private HashSet<string> _serialesPrevios = new();   // Para registrar conexiones y desconexiones

        /* ---------------- UI -------------------- */
        private PictureBox _picUsb   = null!;
        private TextBox    _txtPin   = null!;
        private Button     _btnLogin = null!;

        /* ---------------- GANCHO TECLAS --------- */
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN     = 0x0100;
        private static readonly LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        /* ---------------- WMI WATCHERS ---------- */
        private ManagementEventWatcher _insertWatcher = null!;
        private ManagementEventWatcher _removeWatcher = null!;
        private static readonly string LOG_DIR = "logs";

        /* ========================================================== */
        public Form1()
        {
            InitializeComponent();

#if DEBUG
            FormBorderStyle = FormBorderStyle.Sizable;
            ControlBox = MaximizeBox = MinimizeBox = true;
#else
            FormBorderStyle = FormBorderStyle.None;
            ControlBox = MaximizeBox = MinimizeBox = false;
#endif
            WindowState   = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost       = true;

            Load        += OnLoad;
            FormClosing += OnFormClosing;
            _hookID      = SetHook(_proc);          // bloquea Win Keys
        }

        /* ---------------- LOAD ------------------ */
        private void OnLoad(object? sender, EventArgs e)
        {
            _picUsb = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.StretchImage,
                Size     = new Size(120, 120),
                Image    = Image.FromFile(@"images\usb_icon_off.png"),
                Location = new Point(Width / 2 - 60, Height / 2 - 260)
            };

            _txtPin = new TextBox
            {
                PasswordChar = '*',
                MaxLength    = 4,
                Width        = 200,
                Enabled      = false,
                TextAlign    = HorizontalAlignment.Center,
                Location     = new Point(Width / 2 - 100, Height / 2 - 50)
            };
            _txtPin.KeyPress += (_, ev) =>
            {
                if (!char.IsDigit(ev.KeyChar) && ev.KeyChar != (char)Keys.Back)
                    ev.Handled = true;
            };

            _btnLogin = new Button
            {
                Text     = "Entrar",
                Width    = 200,
                Enabled  = false,
                Location = new Point(Width / 2 - 100, Height / 2)
            };
            _btnLogin.Click += OnValidarPin;

            Controls.AddRange(new Control[] { _picUsb, _txtPin, _btnLogin });

#if !DEBUG
            RestrictCursor();
#endif
            InicializarDeteccionUsb();
        }

        /* ---------------- PIN ------------------- */
        private void OnValidarPin(object? s, EventArgs e)
        {
            if (_txtPin.Text == PIN_VALIDO)
            {
                _pinValidado = true;
                Application.Exit();
            }
            else
            {
                _intentosFallidos++;
                MessageBox.Show($"PIN incorrecto. Intentos restantes: {MAX_INTENTOS - _intentosFallidos}");
                if (_intentosFallidos >= MAX_INTENTOS)
                {
                    MessageBox.Show("Máximo de intentos alcanzado. Apagando.");
                    System.Diagnostics.Process.Start("shutdown", "/s /f /t 0");
                }
            }
        }

        /* ------------- DETECCIÓN USB ------------ */
        private void InicializarDeteccionUsb()
        {
            _insertWatcher = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2"));
            _removeWatcher = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 3"));

            EventArrivedEventHandler h = async (_, __) => await VerificarUsbAsync();

            _insertWatcher.EventArrived += h;
            _removeWatcher.EventArrived += h;

            _insertWatcher.Start();
            _removeWatcher.Start();

            _ = VerificarUsbAsync();      // primer chequeo
        }

        /* ------ núcleo de verificación ------ */
        private async Task VerificarUsbAsync()
        {
            await Task.Delay(500);   // esperar montaje de letra

            var infos = EnumerarUsbInfos();
            var actuales = infos.Select(i => i.Serial).ToHashSet();

            /* -- Logs de Conexión -- */
            foreach (var nuevo in actuales.Except(_serialesPrevios))
                LogEvento(nuevo, "Conectado");

            /* -- Logs de Desconexión -- */
            foreach (var gone in _serialesPrevios.Except(actuales))
                LogEvento(gone, "Desconectado");

            _serialesPrevios = actuales;

            bool llaveOk = await Task.Run(async () =>
            {
                foreach (var info in infos)
                {
                    if (ExisteKeyTxt(info.Letras))
                    {
                        LogDebug($"Archivo key.txt encontrado en {string.Join(", ", info.Letras)}");
                        return true;
                    }
                }
                return false;
            });

            Invoke(new Action(() => ActualizarUiUsb(llaveOk)));
        }

        private void ActualizarUiUsb(bool ok)
        {
            _picUsb.Image = Image.FromFile(ok
                ? @"images\usb_icon_on.png"
                : @"images\usb_icon_off.png");

            _txtPin.Enabled  = ok;
            _btnLogin.Enabled = ok;
        }

        /* ---------  Helpers USB --------- */
        private record UsbInfo(string Serial, List<string> Letras);

        private static string SerialFromPnp(string pnp) =>
            pnp.Split('\\').LastOrDefault()?.Split('&').FirstOrDefault() ?? "";

        private List<UsbInfo> EnumerarUsbInfos()
        {
            var list = new List<UsbInfo>();

            var ddr = new ManagementObjectSearcher(
                "SELECT DeviceID, PNPDeviceID FROM Win32_DiskDrive WHERE InterfaceType='USB'");

            foreach (ManagementObject d in ddr.Get())
            {
                string serial = SerialFromPnp(d["PNPDeviceID"]?.ToString() ?? "");
                if (serial == "") continue;

                var letras = new List<string>();
                foreach (ManagementObject part in d.GetRelated("Win32_DiskPartition"))
                foreach (ManagementObject log  in part.GetRelated("Win32_LogicalDisk"))
                    letras.Add(log["DeviceID"].ToString() + @"\");

                list.Add(new UsbInfo(serial, letras));
            }
            return list;
        }

        private bool ExisteKeyTxt(IEnumerable<string> letras)
        {
            foreach (var letra in letras)
            {
                try
                {
                    // Verificar si tenemos permisos para acceder a la unidad
                    if (Directory.Exists(letra))
                    {
                        string keyFile = Path.Combine(letra, "key.txt");
                        if (File.Exists(keyFile))
                        {
                            LogDebug($"Archivo key.txt encontrado en {letra}");
                            return true;
                        }
                        else
                        {
                            LogDebug($"Archivo key.txt no encontrado en {letra}");
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    LogDebug($"Acceso denegado a {letra}");
                }
            }
            return false;
        }

        /* ------------- LOG ------------- */
        private static void LogEvento(string serial, string evento)
        {
            if (serial == "") return;
            if (!Directory.Exists(LOG_DIR)) Directory.CreateDirectory(LOG_DIR);

            string shortId = serial.Length > 8 ? serial[^8..] : serial;
            string path    = Path.Combine(LOG_DIR, $"usb_{shortId}.txt");
            string ts      = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.AppendAllText(path, $"{ts} - {evento}{Environment.NewLine}");
        }

        /* ------------- LOG DEBUG ------------- */
        private static void LogDebug(string message)
        {
            string logPath = Path.Combine(LOG_DIR, "log.txt");
            string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.AppendAllText(logPath, $"{ts} - {message}{Environment.NewLine}");
        }

        /* ---------- CURSOR y TECLAS ---------- */
        private static void RestrictCursor()
        {
            Rectangle a = new(100, 100, 800, 600);
            RECT r = new() { Left = a.Left, Top = a.Top, Right = a.Right, Bottom = a.Bottom };
            ClipCursor(ref r);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
#if DEBUG
            return base.ProcessCmdKey(ref msg, keyData);
#else
            Keys[] block = { Keys.Alt | Keys.F4, Keys.Alt, Keys.Tab, Keys.LWin, Keys.RWin };
            if (block.Any(k => k == keyData)) return true;
            return base.ProcessCmdKey(ref msg, keyData);
#endif
        }

        private void OnFormClosing(object? s, FormClosingEventArgs e)
        {
#if !DEBUG
            if (!_pinValidado)
            {
                e.Cancel = true;
                MessageBox.Show("No puedes cerrar hasta autenticar.");
            }
#endif
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
#if !DEBUG
            ClipCursor(ref new RECT());
#endif
            UnhookWindowsHookEx(_hookID);
            _insertWatcher.Stop();
            _removeWatcher.Stop();
        }

        /* ---------- GANCHO & P/Invoke ---------- */
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using var p = System.Diagnostics.Process.GetCurrentProcess();
            using var m = p.MainModule!;
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(m.ModuleName), 0);
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vk = Marshal.ReadInt32(lParam);
                if (vk == (int)Keys.LWin || vk == (int)Keys.RWin) return (IntPtr)1;
            }
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")]                       private static extern bool   ClipCursor(ref RECT r);
        [DllImport("user32.dll", SetLastError = true)]  private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", SetLastError = true)]  private static extern bool   UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll", SetLastError = true)]  private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)] private static extern IntPtr GetModuleHandle(string name);
    }
}
