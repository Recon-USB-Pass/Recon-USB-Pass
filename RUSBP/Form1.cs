using System.Runtime.InteropServices;
using System.Management;

namespace BloqueoYUsbDetectionApp
{
    public partial class Form1 : Form
    {
        private const int MaxIntentos = 3;
        private int intentosFallidos = 0;
        private const string PinValido = "1234"; // PIN predefinido
        private bool pinValidado = false; // Variable de estado para indicar si el PIN fue validado

        // Para la detección de USB
        private const string LogDir = "logs";
        private const string LogFile = "logs/usb_log.txt";
        private ManagementEventWatcher insertWatcher = null!;
        private ManagementEventWatcher removeWatcher = null!;
        private Dictionary<string, Tuple<PictureBox, Label>> connectedUsbDevices = new();

        private readonly HttpClient _httpClient;

        public Form1()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;

            // Configuración para el bloqueo de pantalla
            _httpClient = new HttpClient();

            // Iniciar detección de USB
            InitializeUsbDetection();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Inicializar controles de PIN
            TextBox txtPin = new TextBox
            {
                Name = "txtPin",
                PasswordChar = '*',
                Location = new Point(this.Width / 2 - 100, this.Height / 2 - 20),
                Width = 200,
                MaxLength = 4,
                TextAlign = HorizontalAlignment.Center
            };

            txtPin.KeyPress += (s, ev) =>
            {
                if (!char.IsDigit(ev.KeyChar) && ev.KeyChar != (char)Keys.Back)
                {
                    ev.Handled = true; // Bloquea la entrada si no es un número
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
                    Application.Exit(); // Cierra la aplicación después de la validación del PIN
                }
                else
                {
                    intentosFallidos++;
                    MessageBox.Show($"PIN incorrecto. Intentos restantes: {MaxIntentos - intentosFallidos}");
                    if (intentosFallidos >= MaxIntentos)
                    {
                        MessageBox.Show("Número máximo de intentos alcanzado. Apagando el sistema.");
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
            Rectangle areaPermitida = new Rectangle(100, 100, 800, 600); 
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
            if (!pinValidado)
            {
                e.Cancel = true;
                MessageBox.Show("Esta ventana no puede cerrarse hasta que se complete la validación.");
            }
        }

        // Para detectar la inserción y extracción de dispositivos USB
        private void InitializeUsbDetection()
        {
            if (!Directory.Exists(LogDir)) Directory.CreateDirectory(LogDir);

            // Watcher para conexión (EventType=2)
            insertWatcher = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2")
            );
            insertWatcher.EventArrived += (s, e) => Invoke(new Action(RefreshUsbDevices));
            insertWatcher.Start();

            // Watcher para desconexión (EventType=3)
            removeWatcher = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 3")
            );
            removeWatcher.EventArrived += (s, e) => Invoke(new Action(RefreshUsbDevices));
            removeWatcher.Start();

            RefreshUsbDevices();
        }

        private void RefreshUsbDevices()
        {
            Dictionary<string, string> currentUsb = GetConnectedUsbDevices();

            foreach (var device in currentUsb)
            {
                if (!connectedUsbDevices.ContainsKey(device.Key))
                {
                    LogEvent($"USB conectado: {device.Key}");
                    AddUsbDevice(device.Key, device.Value);
                }
            }

            List<string> toRemove = new();
            foreach (var existing in connectedUsbDevices.Keys)
                if (!currentUsb.ContainsKey(existing))
                    toRemove.Add(existing);

            foreach (var devId in toRemove)
            {
                LogEvent($"USB desconectado: {devId}");
                RemoveUsbDevice(devId);
            }
        }

        private Dictionary<string, string> GetConnectedUsbDevices()
        {
            var results = new Dictionary<string, string>();
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'");
            foreach (ManagementObject disk in searcher.Get())
            {
                string pnpId = disk["PNPDeviceID"]?.ToString() ?? "";
                string model = disk["Model"]?.ToString() ?? "USB";
                if (!string.IsNullOrEmpty(pnpId)) results[pnpId] = model;
            }
            return results;
        }

        private void AddUsbDevice(string deviceId, string deviceName)
        {
            var pic = new PictureBox
            {
                Size = new Size(50, 50),
                Image = Image.FromFile("images\\usb_icon.png"),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Transparent
            };
            var lbl = new Label
            {
                AutoSize = true,
                Text = $"Dispositivo: {deviceName}",
                ForeColor = Color.Black
            };
            Controls.Add(pic);
            Controls.Add(lbl);

            connectedUsbDevices[deviceId] = Tuple.Create(pic, lbl);
            RepositionUsbDevices();
        }

        private void RemoveUsbDevice(string deviceId)
        {
            if (connectedUsbDevices.ContainsKey(deviceId))
            {
                var (pic, lbl) = connectedUsbDevices[deviceId];
                Controls.Remove(pic);
                Controls.Remove(lbl);
                connectedUsbDevices.Remove(deviceId);
                RepositionUsbDevices();
            }
        }

        private void RepositionUsbDevices()
        {
            int i = 0;
            foreach (var kvp in connectedUsbDevices)
            {
                var pic = kvp.Value.Item1;
                var lbl = kvp.Value.Item2;
                pic.Location = new Point(10, i * 60 + 10);
                lbl.Location = new Point(70, i * 60 + 25);
                i++;
            }
        }

        private void LogEvent(string text)
        {
            string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            using var writer = new StreamWriter(LogFile, true);
            writer.WriteLine($"{ts} - {text}");
        }

        // Declaración de la estructura RECT para interop con la API de Windows
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // Importación de la función ClipCursor desde user32.dll para controlar el cursor
        [DllImport("user32.dll")]
        public static extern bool ClipCursor(ref RECT lpRect);
    }
}
