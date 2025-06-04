using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;
using RUSBP.Core;
using RUSBP.Helpers;

namespace RUSBP.Forms
{
    /// <summary>Pantalla de bloqueo: valida USB + PIN contra backend.</summary>
    public partial class LoginForm : Form
    {
        private const bool FULLSCREEN = false;

        private readonly ApiClient _api;
        private readonly UsbCryptoService _usb;
        private readonly LogManager? _logManager;
        private readonly LogSyncService? _logSync;
        private readonly UsbWatcher _watcher;

        private string? _challenge;
        private string? _serial;
        private string? _userRut;

        private System.Windows.Forms.Timer? _retryTimer;
        private readonly int _retryIntervalMs = 20000;

        private NotifyIcon? _trayIcon;
        private Button? _btnLogout;
        private PictureBox _picUsbOn = null!;
        private PictureBox _picUsbOff = null!;
        private TextBox _txtPin = null!;
        private Button _btnLogin = null!;
        private Label _lblStatus = null!;

        public LoginForm(ApiClient api, UsbCryptoService usb, LogManager? logMgr = null, LogSyncService? logSync = null)
        {
            _api = api;
            _usb = usb;
            _logManager = logMgr;
            _logSync = logSync;

            BuildUi();
            Resize += (_, __) => CenterControls();

            KeyboardHook.Install();
            CursorGuard.RestrictToControl(this, new Padding(80));

            _watcher = new UsbWatcher();
            _watcher.StateChanged += st => Invoke(() => OnUsbStatusChanged(st));
        }

        /* ========== UI ========= */
        private void BuildUi()
        {
            Text = "RUSBP";
            if (FULLSCREEN)
            {
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
                TopMost = true;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                StartPosition = FormStartPosition.CenterScreen;
                Size = new Size(900, 600);
            }

            BackgroundImage = Properties.Resources.Block_Screen_WALLPAPER;
            BackgroundImageLayout = ImageLayout.Stretch;

            _picUsbOff = new PictureBox
            {
                Image = Properties.Resources.usb_off,
                Size = new Size(140, 140),
                SizeMode = PictureBoxSizeMode.StretchImage
            };
            _picUsbOn = new PictureBox
            {
                Image = Properties.Resources.usb_on,
                Size = _picUsbOff.Size,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Visible = false
            };

            _lblStatus = new Label
            {
                AutoSize = true,
                Text = "Sin USB",
                ForeColor = Color.LightGray,
                Font = new Font(Font.FontFamily, 12, FontStyle.Bold),
                BackColor = Color.Transparent
            };

            _txtPin = new TextBox
            {
                Width = 240,
                PlaceholderText = "PIN",
                PasswordChar = '●',
                Enabled = false,
                Font = new Font(Font.FontFamily, 14),
                TextAlign = HorizontalAlignment.Center
            };
            _txtPin.KeyPress += (_, e) => { if (!char.IsDigit(e.KeyChar) && e.KeyChar != (char)Keys.Back) e.Handled = true; };

            _btnLogin = new Button
            {
                Text = "Entrar",
                Width = 240,
                Height = 45,
                Enabled = false,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = ColorTranslator.FromHtml("#0066A1"),
                Font = new Font(Font.FontFamily, 14, FontStyle.Bold)
            };
            _btnLogin.FlatAppearance.BorderSize = 0;
            _btnLogin.Click += async (_, __) => await OnLoginAsync();

            Controls.AddRange(new Control[] { _picUsbOff, _picUsbOn, _lblStatus, _txtPin, _btnLogin });
            CenterControls();
        }
        private void CenterControls()
        {
            const int VIRTUAL_WIDTH = 380;
            int panelX = (ClientSize.Width / 3) - (VIRTUAL_WIDTH / 2);
            int centerY = ClientSize.Height / 2;

            int iconLeft = panelX + (VIRTUAL_WIDTH - _picUsbOff.Width) / 2;
            int iconTop = centerY - 200;
            _picUsbOff.Location = new Point(iconLeft, iconTop);
            _picUsbOn.Location = _picUsbOff.Location;

            _lblStatus.Location = new Point(iconLeft + (_picUsbOff.Width - _lblStatus.Width) / 2, iconTop - 28);
            _txtPin.Location = new Point(panelX + (VIRTUAL_WIDTH - _txtPin.Width) / 2, _picUsbOff.Bottom + 25);
            _btnLogin.Location = new Point(_txtPin.Left, _txtPin.Bottom + 15);
        }

        /* ========== Watcher ========= */
        private void OnUsbStatusChanged(UsbWatcher.UsbStatus st)
        {
            switch (st)
            {
                case UsbWatcher.UsbStatus.None:
                    ResetUiAfterDisconnect();
                    break;

                case UsbWatcher.UsbStatus.Detected:
                    UpdateVisual(true);
                    _lblStatus.Text = "USB detectado";
                    _lblStatus.ForeColor = Color.DeepSkyBlue;
                    _ = BeginVerificationAsync();
                    break;

                case UsbWatcher.UsbStatus.Verified:
                    _lblStatus.Text = "Verificado";
                    _lblStatus.ForeColor = Color.LimeGreen;
                    break;

                case UsbWatcher.UsbStatus.Error:
                    _lblStatus.Text = "Error verificación";
                    _lblStatus.ForeColor = Color.OrangeRed;
                    break;
            }
        }

        private void ResetUiAfterDisconnect()
        {
            Show(); WindowState = FormWindowState.Normal; ShowInTaskbar = true; BringToFront();
            _txtPin.Visible = true; _btnLogin.Visible = true; _btnLogout?.Hide();
            _lblStatus.Text = "Sin USB"; _lblStatus.ForeColor = Color.LightGray;
            _picUsbOff.Visible = true; _picUsbOn.Visible = false;
            _txtPin.Clear(); _txtPin.Focus();
        }

        private void UpdateVisual(bool usbOk)
        {
            _picUsbOn.Visible = usbOk;
            _picUsbOff.Visible = !usbOk;
            _txtPin.Enabled = usbOk;
            _btnLogin.Enabled = usbOk;
            if (!usbOk) _txtPin.Clear();
            if (_btnLogout != null) _btnLogout.Visible = false;
        }

        /* ========== Verificación + Recover + Unlock ========= */
        private async Task BeginVerificationAsync()
        {
            /* 1️⃣  Esperar a que al menos un USB aparezca (montado o bloqueado) */
            while (UsbCryptoService.EnumerateUsbInfos().Count == 0)
                await Task.Delay(800);

            /* 2️⃣  Tomar el PRIMER dispositivo USB visto (puedes refinar si usas varios) */
            var info = UsbCryptoService.EnumerateUsbInfos().First();
            _serial = info.Serial.ToUpperInvariant();
            string root = info.Roots.First();      // p. ej. “F:\”
            string rootVol = root[..2];               // “F:”

            /* 3️⃣  Si el volumen está BLOQUEADO → recover + unlock con BitLocker */
            if (BitLockerStatus.IsLocked(rootVol))
            {
                // 3.a ── pedir al backend cipher+tag usando /api/usb/recover
                var resp = await _api.RecoverUsbAsync(_serial, 2 /*AgentType = Employee*/);

                if (!resp.Ok)
                {
                    MessageBox.Show(resp.Err ?? "Error en recover-usb", "Backend",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 3.b ── descifrar RecoveryPassword   TAG(16) || CIPHER(n)
                byte[] tagCipher = Convert.FromBase64String(resp.TagB64)
                                 .Concat(Convert.FromBase64String(resp.CipherB64)).ToArray();

                string recPass = CryptoHelper.DecryptToString(
                                     tagCipher,
                                     UsbCryptoService.RpRootGlobal!); // RP_root almacenada

                // 3.c ── desbloquear la unidad con manage-bde (silencioso)
                if (!UsbCryptoService.UnlockBitLockerWithRecoveryPass(rootVol, recPass))
                {
                    MessageBox.Show("No se pudo desbloquear la unidad BitLocker.",
                                    "BitLocker", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // pequeña espera hasta que Windows monte el volumen
                await Task.Delay(2500);
            }

            /* 4️⃣  Ya desbloqueada → localizar estructura /pki y leer el certificado */
            if (!_usb.TryLocateUsb())
            {
                MessageBox.Show("Estructura /pki no encontrada tras el unlock.",
                                "USB inválido", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string certPem = File.ReadAllText(Path.Combine(_usb.MountedRoot!, "pki", "cert.crt"));

            /* 5️⃣  verify-usb → obtengo challenge */
            _challenge = await _api.VerifyUsbAsync(_serial, certPem);
            if (_challenge is null)
            {
                _lblStatus.Text = "Sin conexión… reintento";
                _lblStatus.ForeColor = Color.OrangeRed;
                ProgramarReintentoVerificacion();
                return;
            }

            /* 6️⃣  Todo OK → habilitar PIN */
            _watcher.SetVerified(true);
            _lblStatus.Text = "Unidad lista – ingrese PIN";
            _lblStatus.ForeColor = Color.LimeGreen;
            _txtPin.Enabled = true;
            _btnLogin.Enabled = true;
            _txtPin.Focus();
        }




        private void ProgramarReintentoVerificacion()
        {
            if (_retryTimer == null)
            {
                _retryTimer = new System.Windows.Forms.Timer { Interval = _retryIntervalMs };
                _retryTimer.Tick += async (_, __) =>
                {
                    _retryTimer.Stop();
                    await BeginVerificationAsync();
                };
            }
            if (!_retryTimer.Enabled) _retryTimer.Start();
        }
        private void DetenerReintentoVerificacion()
        {
            if (_retryTimer != null && _retryTimer.Enabled) _retryTimer.Stop();
        }

        /* ========== Login ========= */
        private async Task OnLoginAsync()
        {
            if (_serial == null || _challenge == null) return;
            _btnLogin.Enabled = false;
            try
            {
                string keyPath = Path.Combine(_usb.MountedRoot!, "pki", "priv.key");
                string privPem = File.ReadAllText(keyPath);
                string sig = UsbCryptoService.SignWithKey(privPem, _challenge);

                string mac = NetworkInterface.GetAllNetworkInterfaces()
                                  .FirstOrDefault(i => i.OperationalStatus == OperationalStatus.Up)?
                                  .GetPhysicalAddress().ToString() ?? "";

                var (ok, err) = await _api.LoginAsync(_serial, sig, _txtPin.Text.Trim(), mac);
                if (ok)
                {
                    CursorGuard.Release(); KeyboardHook.Uninstall();
                    CambiarAModoLogoutUI(mac); DialogResult = DialogResult.OK;
                    return;
                }
                string msg = string.IsNullOrWhiteSpace(err) ? "PIN incorrecto o credenciales inválidas." : err;
                MessageBox.Show(msg, "Error de autenticación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                await BeginVerificationAsync();

            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
            finally { _btnLogin.Enabled = true; _txtPin.SelectAll(); _txtPin.Focus(); }
        }

        /* ========== Logout UI ========= */
        private void CambiarAModoLogoutUI(string mac)
        {
            _txtPin.Visible = false; _btnLogin.Visible = false;
            _lblStatus.Text = $"Sesión activa ({mac})"; _lblStatus.ForeColor = Color.LimeGreen;
            _picUsbOn.Visible = true; _picUsbOff.Visible = false;

            if (_btnLogout == null)
            {
                _btnLogout = new Button
                {
                    Text = "Cerrar sesión",
                    Width = 240,
                    Height = 45,
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = Color.White,
                    BackColor = ColorTranslator.FromHtml("#6b0000"),
                    Font = new Font(Font.FontFamily, 14, FontStyle.Bold)
                };
                _btnLogout.Location = new Point(_btnLogin.Left, _btnLogin.Bottom + 15);
                _btnLogout.Click += (_, __) => RealizarLogout();
                Controls.Add(_btnLogout);
            }
            _btnLogout.Visible = true;

            if (_trayIcon == null)
            {
                _trayIcon = new NotifyIcon { Icon = SystemIcons.Shield, Visible = true, Text = "RUSBP - Sesión activa" };
                var menu = new ContextMenuStrip();
                menu.Items.Add("Cerrar sesión", null, (_, __) => Invoke(RealizarLogout));
                menu.Items.Add("Salir (forzado)", null, (_, __) => Application.Exit());
                _trayIcon.ContextMenuStrip = menu;
                _trayIcon.DoubleClick += (_, __) => { Show(); WindowState = FormWindowState.Normal; ShowInTaskbar = true; };
            }

            Hide(); ShowInTaskbar = false;
        }
        private void RealizarLogout() { LockWorkStation(); Application.Exit(); }

        private string ObtenerIpLocal() =>
            System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName())
                    .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?
                    .ToString() ?? "";

        [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool LockWorkStation();

        protected override void OnFormClosing(FormClosingEventArgs e)
        { if (e.CloseReason != CloseReason.UserClosing) LockWorkStation(); base.OnFormClosing(e); }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            return (keyData is (Keys.Alt | Keys.F4) or Keys.Alt or Keys.Tab or Keys.LWin or Keys.RWin) ? true
                          : base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
