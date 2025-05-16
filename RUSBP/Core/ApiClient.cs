using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;

namespace RUSBP.Core
{
    public class ApiClient
    {
        private readonly HttpClient _http;

        public ApiClient(string baseUrl)
        {
            var handler = new HttpClientHandler
            {
                // ⚠️ Aceptar certificados autofirmados solo para desarrollo
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            _http = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        /* ───────────── Auth ───────────── */

        public async Task<string?> VerifyUsbAsync(string serial, string certPem, CancellationToken ct = default)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/auth/verify-usb", new { Serial = serial, CertPem = certPem }, ct);

                if (!res.IsSuccessStatusCode)
                {
                    LogDebug($"[VerifyUsb] Respuesta inválida: {(int)res.StatusCode}");
                    return null;
                }

                return await res.Content.ReadAsStringAsync(ct);
            }
            catch (Exception ex)
            {
                // Identificar si es error de conexión o de SSL
                if (ex is HttpRequestException || ex.Message.Contains("expresamente dicha conexión") || ex.Message.Contains("actively refused"))
                {
                    MessageBox.Show("No hay conexión con el servidor. Por favor revise su red o que el backend esté activo.",
                        "Sin conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show($"Error de conexión con el servidor:\n{ex.Message}",
                        "Error SSL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                LogDebug($"[VerifyUsb] ERROR CONEXION: {ex.Message}");
                return null;
            }
        }


        public async Task<(bool ok, string? msg)> LoginAsync(
                string serial, string sig, string pin, string mac)
        {
            var json = new
            {
                serial,
                signatureBase64 = sig,
                pin,
                macAddress = mac
            };
            var resp = await _http.PostAsJsonAsync("api/auth/login", json);

            string body = await resp.Content.ReadAsStringAsync();
            if (resp.IsSuccessStatusCode)
                return (true, null);

            return (false, body.Length > 200 ? resp.StatusCode.ToString() : body);
        }

        /* ───────────── Logs ───────────── */

        /// <summary>
        /// Envía una lista de eventos de log (JSON batch) al backend.
        /// </summary>
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

        /// <summary>
        /// Envía el archivo de log cifrado (.enc) al backend.
        /// Útil si decides enviar todo el archivo PKI para máxima seguridad.
        /// </summary>
        public async Task<bool> SendEncryptedLogAsync(string serial, string encFilePath, CancellationToken ct = default)
        {
            if (!File.Exists(encFilePath)) return false;
            try
            {
                using var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(File.OpenRead(encFilePath));
                content.Add(fileContent, "logfile", Path.GetFileName(encFilePath));
                content.Add(new StringContent(serial), "serial");

                var resp = await _http.PostAsync("api/logs/upload", content, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    LogDebug($"[SendEncryptedLogAsync] Falló envío: {resp.StatusCode}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                LogDebug($"[SendEncryptedLogAsync] ERROR: {ex.Message}");
                return false;
            }
        }

        /* ───────────── Log Local ───────────── */

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
