using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RUSBP.Core
{
    public class ApiClient
    {
        private readonly HttpClient _http;

        public ApiClient(string backendIp)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            string url = backendIp.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? backendIp
                : $"https://{backendIp}:8443/";
            _http = new HttpClient(handler)
            {
                BaseAddress = new Uri(url),
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        public async Task<string?> VerifyUsbAsync(string serial, string certPem, CancellationToken ct = default)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/auth/verify-usb", new { serial, certPem }, ct);
                if (!res.IsSuccessStatusCode)
                {
                    LogDebug($"[VerifyUsb] Respuesta inválida: {(int)res.StatusCode}");
                    return null;
                }
                return await res.Content.ReadAsStringAsync(ct);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión con el servidor:\n{ex.Message}",
                    "Error SSL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogDebug($"[VerifyUsb] ERROR CONEXION: {ex.Message}");
                return null;
            }
        }

        public async Task<(bool ok, string? msg)> LoginAsync(string serial, string sig, string pin, string mac)
        {
            var json = new
            {
                serial,
                signatureBase64 = sig,
                pin,
                macAddress = mac
            };
            var resp = await _http.PostAsJsonAsync("api/auth/recover", json);
            string body = await resp.Content.ReadAsStringAsync();
            if (resp.IsSuccessStatusCode)
                return (true, null);

            return (false, body.Length > 200 ? resp.StatusCode.ToString() : body);
        }

        // ---- NUEVO ----
        public class RecoverUsbResponse
        {
            public string Cipher { get; set; } = "";
            public string Tag { get; set; } = "";
            public int Rol { get; set; }
        }

        public async Task<RecoverUsbResponse?> RecoverUsbAsync(string serial, int agentType)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/usb/recover", new { serial, agentType });
                if (!res.IsSuccessStatusCode)
                {
                    LogDebug($"[RecoverUsb] Respuesta inválida: {(int)res.StatusCode}");
                    return null;
                }
                var json = await res.Content.ReadFromJsonAsync<RecoverUsbResponse>();
                return json;
            }
            catch (Exception ex)
            {
                LogDebug($"[RecoverUsb] ERROR CONEXION: {ex.Message}");
                return null;
            }
        }

        // --- LOGS Y OTROS MÉTODOS SE MANTIENEN IGUAL ---
        public async Task<bool> SendLogsAsync(List<LogEvent> events, CancellationToken ct = default)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync("api/logs", events, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    LogDebug($"[SendLogsAsync] Falló envío: {resp.StatusCode}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                LogDebug($"[SendLogsAsync] ERROR: {ex.Message}");
                return false;
            }
        }

        private static void LogDebug(string msg)
        {
            try
            {
                string dir = Path.Combine(Path.GetTempPath(), "RUSBP", "logs");
                Directory.CreateDirectory(dir);
                string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                File.AppendAllText(Path.Combine(dir, "debug.txt"),
                                   $"{ts} - {msg}{Environment.NewLine}");
            }
            catch { /* swallow */ }
        }
    }
}
