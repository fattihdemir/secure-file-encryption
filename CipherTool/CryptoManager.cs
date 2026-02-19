using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CipherTool
{
    /// <summary>
    /// AES-256 + PBKDF2 tabanlı dosya şifreleme/çözme motoru.
    /// UI katmanından tamamen bağımsızdır.
    /// </summary>
    public static class CryptoManager
    {
        // Sabitler
        private const int SaltSize = 32;   // 256 bit
        private const int IvSize = 16;   // 128 bit (AES blok boyutu)
        private const int KeySize = 32;   // 256 bit
        private const int Iterations = 200_000; // PBKDF2 iterasyon sayısı
        private const string EncryptedExtension = ".cipher";

        // Şifrelenmiş dosya başlık imzası (Magic Bytes) — bütünlük kontrolü için
        private static readonly byte[] MagicBytes = Encoding.ASCII.GetBytes("CIPHERTOOL_V1");

        // ------------------------------------------------------------------ //
        //  PUBLIC API
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Verilen dosyayı AES-256 ile şifreler.
        /// </summary>
        /// <param name="sourceFilePath">Şifrelenecek kaynak dosya yolu</param>
        /// <param name="password">Kullanıcı parolası</param>
        /// <returns>Oluşturulan .cipher dosyasının tam yolu</returns>
        public static string EncryptFile(string sourceFilePath, string password)
        {
            ValidateInputs(sourceFilePath, password);

            string destFilePath = sourceFilePath + EncryptedExtension;

            // Salt ve IV rastgele üret
            byte[] salt = GenerateRandomBytes(SaltSize);
            byte[] iv = GenerateRandomBytes(IvSize);

            // Paroladan anahtar türet (PBKDF2 / SHA-256)
            byte[] key = DeriveKey(password, salt);

            using FileStream fsOut = new(destFilePath, FileMode.Create, FileAccess.Write);
            using FileStream fsIn = new(sourceFilePath, FileMode.Open, FileAccess.Read);

            // Başlık yaz: [MagicBytes][Salt][IV]
            fsOut.Write(MagicBytes, 0, MagicBytes.Length);
            fsOut.Write(salt, 0, salt.Length);
            fsOut.Write(iv, 0, iv.Length);

            // AES-256-CBC ile şifrele
            using Aes aes = CreateAes(key, iv);
            using ICryptoTransform encryptor = aes.CreateEncryptor();
            using CryptoStream cs = new(fsOut, encryptor, CryptoStreamMode.Write);

            fsIn.CopyTo(cs);
            cs.FlushFinalBlock();

            return destFilePath;
        }

        /// <summary>
        /// Verilen .cipher dosyasını çözer.
        /// </summary>
        /// <param name="sourceFilePath">Çözülecek .cipher dosyası</param>
        /// <param name="password">Kullanıcı parolası</param>
        /// <returns>Oluşturulan çözülmüş dosyanın tam yolu</returns>
        public static string DecryptFile(string sourceFilePath, string password)
        {
            ValidateInputs(sourceFilePath, password);

            if (!sourceFilePath.EndsWith(EncryptedExtension, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Seçilen dosya geçerli bir .cipher dosyası değil.");

            // Orijinal dosya adını geri al (uzantıyı çıkar)
            string destFilePath = sourceFilePath[..^EncryptedExtension.Length];

            // Aynı isimde dosya varsa çakışmayı önle
            destFilePath = GetUniqueFilePath(destFilePath);

            using FileStream fsIn = new(sourceFilePath, FileMode.Open, FileAccess.Read);

            // MagicBytes doğrula
            byte[] magicRead = new byte[MagicBytes.Length];
            int bytesRead = fsIn.Read(magicRead, 0, magicRead.Length);
            if (bytesRead != MagicBytes.Length || !CompareBytes(magicRead, MagicBytes))
                throw new InvalidDataException(
                    "Dosya imzası geçersiz. Bu bir CipherTool dosyası olmayabilir.");

            // Salt ve IV oku
            byte[] salt = new byte[SaltSize];
            byte[] iv = new byte[IvSize];
            fsIn.Read(salt, 0, SaltSize);
            fsIn.Read(iv, 0, IvSize);

            // Paroladan anahtar türet
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
                // Yanlış parola veya bozuk dosya — oluşturulan geçici dosyayı temizle
                fsOut.Close();
                if (File.Exists(destFilePath)) File.Delete(destFilePath);
                throw new CryptographicException(
                    "Şifre çözme başarısız. Parola yanlış veya dosya bozuk olabilir.");
            }

            return destFilePath;
        }

        // ------------------------------------------------------------------ //
        //  PRIVATE HELPERS
        // ------------------------------------------------------------------ //

        private static byte[] DeriveKey(string password, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256);

            return pbkdf2.GetBytes(KeySize);
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
            // Sabit zamanlı karşılaştırma (timing attack önlemi)
            return CryptographicOperations.FixedTimeEquals(a, b);
        }

        private static void ValidateInputs(string filePath, string password)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Dosya yolu boş olamaz.");
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Dosya bulunamadı.", filePath);
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Parola boş olamaz.");
            if (password.Length < 8)
                throw new ArgumentException("Parola en az 8 karakter olmalıdır.");
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
    }
}