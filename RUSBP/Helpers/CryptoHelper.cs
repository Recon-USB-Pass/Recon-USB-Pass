using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace RUSBP.Core
{
    /// <summary>
    /// Ayuda para operaciones criptográficas: firma, cifrado y descifrado con PKI.
    /// </summary>
    public static class CryptoHelper
    {
        // --- CIFRADO Y DESCIFRADO ASIMÉTRICO (RSA) ---

        /// <summary>
        /// Cifra los datos usando la clave pública de un certificado X.509 (RSA).
        /// </summary>
        public static byte[] EncryptWithCert(byte[] data, X509Certificate2 publicCert)
        {
            using var rsa = publicCert.GetRSAPublicKey();
            return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
        }

        /// <summary>
        /// Descifra los datos usando la clave privada del certificado (RSA).
        /// </summary>
        public static byte[] DecryptWithCert(byte[] encrypted, X509Certificate2 privateCert)
        {
            using var rsa = privateCert.GetRSAPrivateKey();
            return rsa.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA256);
        }

        // --- FIRMA Y VERIFICACIÓN DIGITAL (RSA) ---

        /// <summary>
        /// Firma datos usando el certificado y clave privada del agente/USB.
        /// </summary>
        public static byte[] SignData(byte[] data, X509Certificate2 signerCert)
        {
            using var rsa = signerCert.GetRSAPrivateKey();
            return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        /// <summary>
        /// Verifica la firma usando el certificado público.
        /// </summary>
        public static bool VerifySignature(byte[] data, byte[] signature, X509Certificate2 cert)
        {
            using var rsa = cert.GetRSAPublicKey();
            return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        // --- CIFRADO/DECIFRADO SIMÉTRICO (AES) ---

        /// <summary>
        /// Cifra datos usando AES-256 en CBC y un IV aleatorio. Devuelve IV + ciphertext.
        /// </summary>
        public static byte[] EncryptAES(byte[] data, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length);
            using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }

        /// <summary>
        /// Descifra datos cifrados por EncryptAES.
        /// </summary>
        public static byte[] DecryptAES(byte[] encrypted, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            byte[] iv = new byte[aes.BlockSize / 8];
            Array.Copy(encrypted, 0, iv, 0, iv.Length);
            aes.IV = iv;
            using var ms = new MemoryStream(encrypted, iv.Length, encrypted.Length - iv.Length);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var reader = new MemoryStream();
            cs.CopyTo(reader);
            return reader.ToArray();
        }

        // --- CARGA DE CERTIFICADOS ---

        /// <summary>
        /// Carga un certificado X.509 desde archivo PEM o PFX.
        /// </summary>
        public static X509Certificate2 LoadCertificate(string path, string? password = null)
        {
            if (path.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase))
                return new X509Certificate2(path, password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);

            if (path.EndsWith(".pem", StringComparison.OrdinalIgnoreCase))
                return X509Certificate2.CreateFromPemFile(path);

            throw new NotSupportedException("Solo .pfx y .pem soportados");
        }

        /// <summary>
        /// Convierte un string PEM a X509Certificate2.
        /// </summary>
        public static X509Certificate2 FromPemString(string pem)
        {
            return X509Certificate2.CreateFromPem(pem);
        }

        // --- ÚTILES DE TEXTO ---

        /// <summary>
        /// Codifica un array de bytes como base64 string.
        /// </summary>
        public static string ToBase64(byte[] data) => Convert.ToBase64String(data);

        /// <summary>
        /// Decodifica base64 a array de bytes.
        /// </summary>
        public static byte[] FromBase64(string b64) => Convert.FromBase64String(b64);
    }
}
