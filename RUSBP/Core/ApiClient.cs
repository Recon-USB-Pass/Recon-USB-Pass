using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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
        public record RecoverUsbResponse(bool Ok, string? Err,
                                         string CipherB64, string TagB64);

        public async Task<RecoverUsbResponse> RecoverUsbAsync(string serial, int agentType)
        {
            var body = new { serial, agentType };
            var resp = await _http.PostAsJsonAsync("api/usb/recover", body);

            if (!resp.IsSuccessStatusCode)
            {
                string err = await resp.Content.ReadAsStringAsync();
                return new RecoverUsbResponse(false, err, "", "");
            }

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            string cipherB64 = json!.GetProperty("cipher").GetString()!;
            string tagB64 = json.GetProperty("tag").GetString()!;
            return new RecoverUsbResponse(true, null, cipherB64, tagB64);
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
        public async Task<(bool ok, string? err,
                   string cipherB64, string tagB64)>
        RecoverUsbAsync(string serial, int agentType = 2,
                        CancellationToken ct = default)
        {
            var json = new { serial, agentType };
            var resp = await _http.PostAsJsonAsync("api/usb/recover", json, ct);

            var body = await resp.Content.ReadAsStringAsync(ct);
            if (resp.IsSuccessStatusCode)
            {
                var j = JsonSerializer.Deserialize<JsonElement>(body);
                return (true, null,
                        j.GetProperty("cipher").GetString()!,
                        j.GetProperty("tag").GetString()!);
            }
            return (false,
                    body.Length > 200 ? resp.StatusCode.ToString() : body,
                    "", "");
        }


    }
}
