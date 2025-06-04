using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace RUSBP.Core
{
    /// <summary>
    /// Utilidades criptográficas usadas por RUSBP (agent & bootstrap)
    /// </summary>
    public static class CryptoHelper
    {
        // ────────────────────────────────────────────────
        // 1.  CIFRADO / DESCIFRADO SIMÉTRICO  (AES-256-CBC)
        // ────────────────────────────────────────────────

        /// <summary>
        /// Cifra <paramref name="data"/> con AES-256 (CBC).  
        /// Devuelve IV || ciphertext  en un solo «blob».
        /// </summary>
        public static byte[] EncryptAES(byte[] data, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();                      // IV aleatorio

            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length);    // IV al inicio

            using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();

            return ms.ToArray();
        }

        /// <summary>
        /// Descifra un blob IV || ciphertext generado por <see cref="EncryptAES"/>.
        /// </summary>
        public static byte[] DecryptAES(byte[] encrypted, byte[] key)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = key;

                byte[] iv = new byte[aes.BlockSize / 8];
                Array.Copy(encrypted, iv, iv.Length);
                aes.IV = iv;

                using var msIn = new MemoryStream(encrypted, iv.Length, encrypted.Length - iv.Length);
                using var cs = new CryptoStream(msIn, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using var msOut = new MemoryStream();
                cs.CopyTo(msOut);
                return msOut.ToArray();
            }
            catch (CryptographicException ex)
            {
                MessageBox.Show(
                    "No se pudo descifrar el archivo:\n" +
                    "• Verifica que el RecoveryPassword ingresado sea el correcto.\n" +
                    "• El archivo puede estar corrupto o no fue generado por el sistema original.\n\n" +
                    ex.Message,
                    "Error de descifrado", MessageBoxButtons.OK, MessageBoxIcon.Error
                );
                throw; // Opcional: puedes lanzar o devolver null/array vacío según tu flujo.
            }
        }

        /// <summary>
        /// Deriva una clave AES-256 desde la password (PBKDF2-SHA256, 100 000 iter.).  
        /// *La misma función la usan el bootstrap y el agente para los .btlk *.
        /// </summary>
        public static byte[] DeriveAesKey(string password)
        {
            // Sal fija: suficientemente buena para ficheros locales (no expuestos).
            byte[] salt = Encoding.UTF8.GetBytes("RUSBP_SALT");
            using var kdf = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            return kdf.GetBytes(32);   // 32 bytes → AES-256
        }

        // ────────────────────────────────────────────────
        // 2.  CIFRADO / FIRMA ASIMÉTRICOS (RSA)
        // ────────────────────────────────────────────────

        /// <summary>Cifra <paramref name="data"/> con la clave pública de <paramref name="publicCert"/> (RSA-OAEP-SHA256).</summary>
        public static byte[] EncryptWithCert(byte[] data, X509Certificate2 publicCert)
        {
            using var rsa = publicCert.GetRSAPublicKey();
            return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
        }

        /// <summary>Descifra <paramref name="encrypted"/> con la clave privada de <paramref name="privateCert"/>.</summary>
        public static byte[] DecryptWithCert(byte[] encrypted, X509Certificate2 privateCert)
        {
            using var rsa = privateCert.GetRSAPrivateKey();
            return rsa.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA256);
        }

        /// <summary>Firma datos con la clave privada de <paramref name="signerCert"/> (SHA-256/PKCS#1).</summary>
        public static byte[] SignData(byte[] data, X509Certificate2 signerCert)
        {
            using var rsa = signerCert.GetRSAPrivateKey();
            return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        /// <summary>Verifica la firma con el certificado público.</summary>
        public static bool VerifySignature(byte[] data, byte[] signature, X509Certificate2 cert)
        {
            using var rsa = cert.GetRSAPublicKey();
            return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        // ────────────────────────────────────────────────
        // 3.  Carga rápida de certificados PEM / PFX
        // ────────────────────────────────────────────────

        public static X509Certificate2 LoadCertificate(string path, string? password = null)
        {
            if (path.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase))
                return new X509Certificate2(path, password,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);

            if (path.EndsWith(".pem", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".crt", StringComparison.OrdinalIgnoreCase))
                return X509Certificate2.CreateFromPemFile(path);

            throw new NotSupportedException("Solo .pfx, .pem o .crt soportados");
        }

        public static X509Certificate2 FromPemString(string pem) => X509Certificate2.CreateFromPem(pem);

        // ────────────────────────────────────────────────
        // 4.  Base64 helpers sencillos
        // ────────────────────────────────────────────────
        public static string ToBase64(byte[] data) => Convert.ToBase64String(data);
        public static byte[] FromBase64(string b64) => Convert.FromBase64String(b64);


        /// <summary>
        /// Descifra un buffer AES-GCM: [tag | cipher], con recoverypass (48 dígitos c/guiones).
        /// </summary>
        public static string DecryptBtlkIp(string filePath, string recoveryPass)
        {
            byte[] tagCipher = File.ReadAllBytes(filePath);

            byte[] tag = new byte[16];
            Array.Copy(tagCipher, 0, tag, 0, 16);

            byte[] cipher = new byte[tagCipher.Length - 16];
            Array.Copy(tagCipher, 16, cipher, 0, cipher.Length);

            byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes(recoveryPass));
            byte[] iv = new byte[12]; // 12 bytes a cero

            byte[] plain = new byte[cipher.Length];
            using (var aes = new AesGcm(key))
            {
                aes.Decrypt(iv, cipher, tag, plain);
            }
            return Encoding.UTF8.GetString(plain);
        }

        // Descifra TAG||CIPHER usando AES-GCM con IV = 0 × 12 y key = SHA-256(pass)
        public static string DecryptToString(byte[] tagCipher, string pass)
        {
            const int TAG_LEN = 16;
            byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes(pass));
            byte[] tag = tagCipher[..TAG_LEN];
            byte[] cipher = tagCipher[TAG_LEN..];
            byte[] plain = new byte[cipher.Length];

            using var gcm = new AesGcm(key);
            gcm.Decrypt(new byte[12], cipher, tag, plain);
            return Encoding.UTF8.GetString(plain);
        }






    }
}
