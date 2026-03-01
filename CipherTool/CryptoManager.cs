using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace CipherTool
{
    /// <summary>
    /// AES-256 + PBKDF2 tabanlı dosya şifreleme/çözme motoru.
    /// UI katmanından tamamen bağımsızdır.
    /// </summary>
    public static class CryptoManager
    {
        // Sabitler
        private const int SaltSize = 32;
        private const int IvSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 200_000;
        private const string EncryptedExtension = ".cipher";

        private static readonly byte[] MagicBytes = Encoding.ASCII.GetBytes("CIPHERTOOL_V1");

        private static readonly string SpecialChars = "!@#$%^&*()_+-=[]{}|;':\",./<>?";

        // ------------------------------------------------------------------ //
        //  PUBLIC API
        // ------------------------------------------------------------------ //

        public static string EncryptFile(string sourceFilePath, string password)
        {
            ValidateInputs(sourceFilePath, password);

            string destFilePath = sourceFilePath + EncryptedExtension;

            byte[] salt = GenerateRandomBytes(SaltSize);
            byte[] iv = GenerateRandomBytes(IvSize);
            byte[] key = DeriveKey(password, salt);

            using FileStream fsOut = new(destFilePath, FileMode.Create, FileAccess.Write);
            using FileStream fsIn = new(sourceFilePath, FileMode.Open, FileAccess.Read);

            fsOut.Write(MagicBytes, 0, MagicBytes.Length);
            // ipucu olarak dosya adını yaz
            string hint = Path.GetFileNameWithoutExtension(sourceFilePath);
            byte[] hintBytes = Encoding.UTF8.GetBytes(hint);
            byte[] hintLength = BitConverter.GetBytes(hintBytes.Length);

            fsOut.Write(hintLength, 0, hintLength.Length);
            fsOut.Write(hintBytes, 0, hintBytes.Length);
            fsOut.Write(salt, 0, salt.Length);
            fsOut.Write(iv, 0, iv.Length);

            using Aes aes = CreateAes(key, iv);
            using ICryptoTransform encryptor = aes.CreateEncryptor();
            using CryptoStream cs = new(fsOut, encryptor, CryptoStreamMode.Write);

            fsIn.CopyTo(cs);
            cs.FlushFinalBlock();

            return destFilePath;
        }

        public static string DecryptFile(string sourceFilePath, string password)
        {
            ValidateInputs(sourceFilePath, password);

            if (!sourceFilePath.EndsWith(EncryptedExtension, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Seçilen dosya geçerli bir .cipher dosyası değil.");

            string destFilePath = sourceFilePath[..^EncryptedExtension.Length];
            destFilePath = GetUniqueFilePath(destFilePath);

            using FileStream fsIn = new(sourceFilePath, FileMode.Open, FileAccess.Read);

            byte[] bytes = new byte[MagicBytes.Length];
            byte[] magicRead = bytes;
            int bytesRead = fsIn.Read(magicRead, 0, magicRead.Length);
            if (bytesRead != MagicBytes.Length || !CompareBytes(magicRead, MagicBytes))
                throw new InvalidDataException("Dosya imzası geçersiz. Bu bir CipherTool dosyası olmayabilir.");

            byte[] salt = new byte[SaltSize];
            byte[] iv = new byte[IvSize];

            // CA2022 UYARISI ÇÖZÜMÜ: Read yerine ReadExactly kullanıldı.
            // ipucu kısmını atla
            byte[] lenBytes = new byte[4];
            fsIn.Read(lenBytes, 0, 4);
            int hintLength = BitConverter.ToInt32(lenBytes, 0);
            fsIn.Position += hintLength;
            fsIn.ReadExactly(salt, 0, SaltSize);
            fsIn.ReadExactly(iv, 0, IvSize);

            byte[] key = DeriveKey(password, salt);

            using FileStream fsOut = new(destFilePath, FileMode.Create, FileAccess.Write);

            try
            {
                using Aes aes = CreateAes(key, iv);
                using ICryptoTransform decryptor = aes.CreateDecryptor();
                using CryptoStream cs = new(fsIn, decryptor, CryptoStreamMode.Read);
                cs.CopyTo(fsOut);
            }
            catch (CryptographicException)
            {
                fsOut.Close();
                if (File.Exists(destFilePath)) File.Delete(destFilePath);
                throw new CryptographicException("Şifre çözme başarısız. Parola yanlış veya dosya bozuk olabilir.");
            }

            return destFilePath;
        }

        /// <summary>
        /// Parola gücünü 0-5 arası puan olarak döner.
        /// UI'da canlı gösterim için kullanılır.
        /// </summary>
        public static int GetPasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password)) return 0;

            int score = 0;
            if (password.Length >= 8) score++;
            if (password.Any(char.IsUpper)) score++;
            if (password.Any(char.IsLower)) score++;
            if (password.Any(char.IsDigit)) score++;
            if (password.Any(c => SpecialChars.Contains(c))) score++;
            return score;
        }

        // ------------------------------------------------------------------ //
        //  PRIVATE HELPERS
        // ------------------------------------------------------------------ //

        private static void ValidateInputs(string filePath, string password)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Dosya yolu boş olamaz.");
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Dosya bulunamadı.", filePath);
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Parola boş olamaz.");

            var errors = new List<string>();

            if (password.Length < 8)
                errors.Add("• En az 8 karakter olmalıdır");
            if (!password.Any(char.IsUpper))
                errors.Add("• En az 1 büyük harf içermelidir (A-Z)");
            if (!password.Any(char.IsLower))
                errors.Add("• En az 1 küçük harf içermelidir (a-z)");
            if (!password.Any(char.IsDigit))
                errors.Add("• En az 1 rakam içermelidir (0-9)");
            if (!password.Any(c => SpecialChars.Contains(c)))
                errors.Add("• En az 1 özel karakter içermelidir (!@#$%^&* vb.)");

            if (errors.Count > 0)
                throw new ArgumentException("Parola yeterince güçlü değil:\n" + string.Join("\n", errors));
        }

        private static byte[] DeriveKey(string password, byte[] salt)
        {
            // SYSLIB0060 UYARISI ÇÖZÜMÜ: Obsolete metot yerine modern statik Pbkdf2 kullanıldı.
            return Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize);
        }

        private static Aes CreateAes(byte[] key, byte[] iv)
        {
            Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;
            return aes;
        }

        private static byte[] GenerateRandomBytes(int size)
        {
            byte[] bytes = new byte[size];
            RandomNumberGenerator.Fill(bytes);
            return bytes;
        }

        private static bool CompareBytes(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            return CryptographicOperations.FixedTimeEquals(a, b);
        }

        private static string GetUniqueFilePath(string path)
        {
            if (!File.Exists(path)) return path;

            string dir = Path.GetDirectoryName(path)!;
            string name = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            int counter = 1;

            string newPath;
            do
            {
                newPath = Path.Combine(dir, $"{name}_decrypted_{counter}{ext}");
                counter++;
            } while (File.Exists(newPath));

            return newPath;
        }

        // ===== ŞİFRE İPUCU OKUMA METODU =====
        public static string ReadHint(string encryptedFilePath)
        {
            if (string.IsNullOrWhiteSpace(encryptedFilePath))
                throw new ArgumentException("Dosya yolu boş olamaz.", nameof(encryptedFilePath));

            if (!File.Exists(encryptedFilePath))
                throw new FileNotFoundException("Dosya bulunamadı.", encryptedFilePath);

            using FileStream fs = new(encryptedFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            // Dosya yapısı: [MagicBytes][Salt(32)][IV(16)][HintLen(4)][HintBytes][CipherData...]
            // Magic + Salt + IV kısmını atla
            fs.Position = MagicBytes.Length + SaltSize + IvSize;

            // Hint uzunluğunu oku (4 byte)
            byte[] lenBytes = new byte[4];
            fs.ReadExactly(lenBytes, 0, 4);
            int hintLength = BitConverter.ToInt32(lenBytes, 0);

            // Güvenlik kontrolü: saçma değerleri engelle
            if (hintLength <= 0 || hintLength > 4096)

                return "İpucu yok";

            // Hint verisini oku
            byte[] hintBytes = new byte[hintLength];
            fs.ReadExactly(hintBytes, 0, hintLength);

            string hint = Encoding.UTF8.GetString(hintBytes).Trim();
            return string.IsNullOrWhiteSpace(hint) ? "İpucu yok" : hint;
        }
    }
}