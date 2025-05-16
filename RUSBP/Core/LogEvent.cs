using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace RUSBP.Core
{
    /// <summary>
    /// Representa un evento individual de conexión/desconexión USB.
    /// </summary>
    public class LogEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        public string UserRut { get; set; } = default!;
        public string UsbSerial { get; set; } = default!;
        public string EventType { get; set; } = default!; // "conexión", "desconexión", etc.
        public string Ip { get; set; } = default!;
        public string Mac { get; set; } = default!;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        // Opcional: puedes agregar más campos aquí si necesitas
    }

    /// <summary>
    /// Maneja la persistencia y cifrado PKI de logs para un USB.
    /// </summary>
    public class LogEventStore
    {
        private readonly string _filePath; // Ruta del log cifrado: logs/SERIAL.enc
        private readonly X509Certificate2 _backendCert; // Certificado público del backend para cifrar
        private readonly X509Certificate2? _usbCert;    // Certificado propio del USB para firma (opcional)

        public LogEventStore(string serial, X509Certificate2 backendCert, X509Certificate2? usbCert = null)
        {
            var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logsDir);
            _filePath = Path.Combine(logsDir, $"{serial.ToUpperInvariant()}.enc");
            _backendCert = backendCert;
            _usbCert = usbCert;
        }

        /// <summary>
        /// Guarda la lista de eventos cifrada con la clave pública del backend.
        /// Firma el JSON con el certificado del USB si está disponible.
        /// </summary>
        public void SaveEncrypted(List<LogEvent> logs)
        {
            // Serializa los eventos a JSON
            var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = false });
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

            // Firma el JSON si hay certificado de USB (opcional)
            byte[] signature = Array.Empty<byte>();
            if (_usbCert != null && _usbCert.HasPrivateKey)
            {
                using var rsa = _usbCert.GetRSAPrivateKey();
                if (rsa != null)
                {
                    signature = rsa.SignData(jsonBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
            }

            // Cifra el JSON usando la clave pública del backend
            using var rsaBackend = _backendCert.GetRSAPublicKey();
            byte[] encryptedJson = rsaBackend.Encrypt(jsonBytes, RSAEncryptionPadding.OaepSHA256);

            // Estructura de archivo: [4 bytes: lenFirma][firma][cifrado]
            using var fs = File.Create(_filePath);
            var lenFirma = BitConverter.GetBytes(signature.Length);
            fs.Write(lenFirma, 0, 4);
            if (signature.Length > 0) fs.Write(signature, 0, signature.Length);
            fs.Write(encryptedJson, 0, encryptedJson.Length);
        }

        /// <summary>
        /// Lee y descifra la lista de eventos (requiere clave privada del backend).
        /// </summary>
        public static List<LogEvent> LoadDecrypted(string filePath, X509Certificate2 backendCertWithPrivateKey, out byte[]? signature)
        {
            signature = null;
            if (!File.Exists(filePath)) return new List<LogEvent>();

            using var fs = File.OpenRead(filePath);
            var lenBuf = new byte[4];
            if (fs.Read(lenBuf, 0, 4) != 4) return new List<LogEvent>();
            int lenSig = BitConverter.ToInt32(lenBuf, 0);

            byte[] sigBuf = Array.Empty<byte>();
            if (lenSig > 0)
            {
                sigBuf = new byte[lenSig];
                if (fs.Read(sigBuf, 0, lenSig) != lenSig) return new List<LogEvent>();
                signature = sigBuf;
            }

            using var ms = new MemoryStream();
            fs.CopyTo(ms);
            byte[] encryptedJson = ms.ToArray();

            using var rsa = backendCertWithPrivateKey.GetRSAPrivateKey();
            byte[] decrypted = rsa.Decrypt(encryptedJson, RSAEncryptionPadding.OaepSHA256);

            var json = System.Text.Encoding.UTF8.GetString(decrypted);
            return JsonSerializer.Deserialize<List<LogEvent>>(json) ?? new List<LogEvent>();
        }

        /// <summary>
        /// Agrega un evento al archivo cifrado. Carga, agrega, guarda.
        /// </summary>
        public void AddEvent(LogEvent log)
        {
            var logs = LoadLogs();
            logs.Add(log);
            SaveEncrypted(logs);
        }

        /// <summary>
        /// Carga los eventos descifrando con el certificado del backend.
        /// No requiere la clave privada si sólo necesitas ver el archivo.
        /// </summary>
        public List<LogEvent> LoadLogs()
        {
            // Sólo posible desencriptar si tienes la clave privada (normalmente en el backend)
            // Aquí puedes solo devolver vacío o implementar desencriptado con clave privada local si tienes una copia
            return new List<LogEvent>(); // Para agentes, en general solo agregan eventos nuevos
        }
    }
}
