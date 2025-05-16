using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace RUSBP.Core
{
    /// <summary>
    /// Administra los eventos locales de un USB, genera, almacena, detecta anomalías y prepara sync PKI.
    /// </summary>
    public class LogManager
    {
        private readonly string _serial;
        private readonly string _userRut;
        private readonly LogEventStore _store;
        private readonly List<LogEvent> _logCache = new();

        public LogManager(string usbSerial, string userRut, X509Certificate2 backendCert, X509Certificate2? usbCert = null)
        {
            _serial = usbSerial.ToUpperInvariant();
            _userRut = userRut;
            _store = new LogEventStore(_serial, backendCert, usbCert);

            // Intenta cargar los logs descifrados si es posible
            // (En agente, esto será casi siempre solo para append; sólo el backend puede descifrar)
            // _logCache = _store.LoadLogs();
            // Para agente, mantenemos una copia local en claro (por ejemplo, _serial + ".json")
            var jsonPath = GetPlainJsonPath();
            if (File.Exists(jsonPath))
            {
                try
                {
                    var json = File.ReadAllText(jsonPath);
                    _logCache = JsonSerializer.Deserialize<List<LogEvent>>(json) ?? new List<LogEvent>();
                }
                catch { _logCache = new List<LogEvent>(); }
            }
        }

        private string GetPlainJsonPath()
            => Path.Combine(AppContext.BaseDirectory, "logs", $"{_serial}.json");

        /// <summary>
        /// Agrega un evento y detecta desconexiones anómalas automáticamente.
        /// </summary>
        public void AddEvent(LogEvent ev)
        {
            // 1. Reglas para detectar anormalidades:
            var last = _logCache.LastOrDefault();
            if (ev.EventType == "conexión")
            {
                if (last != null && last.EventType == "conexión")
                {
                    // Inserta desconexión anómala antes
                    _logCache.Add(new LogEvent
                    {
                        EventId = Guid.NewGuid().ToString(),
                        UserRut = _userRut,
                        UsbSerial = _serial,
                        EventType = "desconexión_no_registrada",
                        Ip = last.Ip,
                        Mac = last.Mac,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
            else if (ev.EventType == "desconexión")
            {
                if (last != null && last.EventType == "desconexión")
                {
                    _logCache.Add(new LogEvent
                    {
                        EventId = Guid.NewGuid().ToString(),
                        UserRut = _userRut,
                        UsbSerial = _serial,
                        EventType = "evento_desconocido",
                        Ip = last.Ip,
                        Mac = last.Mac,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
            // 2. Añade el evento real
            _logCache.Add(ev);

            // 3. Guarda versión cifrada y versión local en claro para revisión/sync rápida
            _store.SaveEncrypted(_logCache);
            File.WriteAllText(GetPlainJsonPath(), JsonSerializer.Serialize(_logCache, new JsonSerializerOptions { WriteIndented = true }));
        }

        /// <summary>
        /// Obtiene todos los eventos locales en claro (solo para sync).
        /// </summary>
        public List<LogEvent> GetLocalLogs() => new List<LogEvent>(_logCache);

        /// <summary>
        /// Prepara el archivo cifrado para subirlo al backend (retorna ruta del .enc)
        /// </summary>
        public string GetEncryptedLogPath() =>
            Path.Combine(AppContext.BaseDirectory, "logs", $"{_serial}.enc");

        /// <summary>
        /// Marca eventos como sincronizados tras respuesta OK del backend.
        /// (Opcional: puedes implementar una marca bool o llevar solo por EventId).
        /// </summary>
        public void MarkSynced(IEnumerable<string> syncedEventIds)
        {
            // Ejemplo: solo para demostración, no cambia nada porque backend ignora duplicados por EventId
            // Si quieres, puedes implementar un campo bool 'Synced' y reescribir los archivos
        }

        /// <summary>
        /// Borra logs locales, si quieres limpiar tras sync completa
        /// </summary>
        public void ClearLogs()
        {
            _logCache.Clear();
            _store.SaveEncrypted(_logCache);
            File.WriteAllText(GetPlainJsonPath(), "[]");
        }
    }
}
