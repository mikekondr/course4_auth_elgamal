using auth_elgamal.Models.Keys;
using System.IO;
using System.Numerics;

namespace auth_elgamal.Services
{
    /// <summary>
    /// Сервіс для роботи з алгоритмом Ель-Гамаля
    /// надає інтерфейс для генерації ключів, шифрування та розшифрування файлів.
    /// </summary>
    public class ElGamalService
    {
        // розмір блоку в байтах
        // розмір ключа 64 десяткових знаків ~ 216 біт ~ 27 байт
        // тому беремо блок розміром 26 байт, щоб бути впевненими, що повідомлення менше ключа
        private const int BLOCK_SIZE = 26;

        // Генерація пари ключів
        // ім'я користувача використовується для імен файлів ключів
        public bool GenerateKeys(string username)
        {
            bool result = false;
            try
            {
                string privateKeyFile = $"{username}.key";
                string publicKeyFile = $"{username}.pub";

                // прості великі числа p і q
                (BigInteger p, BigInteger q) = ElGamalCrypto.GenerateSafePrime(64);
                // генератор g
                BigInteger g = ElGamalCrypto.GenerateG(p, q);
                // приватний ключ x
                BigInteger x = ElGamalCrypto.GenerateRandomBigInteger(2, q);
                // публічний ключ y
                BigInteger y = BigInteger.ModPow(g, x, p);

                // в обидва набори входять p, q, g, але приватний містить x, а публічний - y
                File.WriteAllLines(privateKeyFile, [p.ToString(), q.ToString(), g.ToString(), x.ToString()]);
                File.WriteAllLines(publicKeyFile, [p.ToString(), q.ToString(), g.ToString(), y.ToString()]);

                result = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Key Gen Error: {ex.Message}");
            }

            return result;
        }

        // оскільки формат файлів ключів однаковий, читання файлу також однакове
        // помилка - повертає null
        private string[]? LoadKeyFile(string filePath)
        {
            string[]? result = null;
            if (File.Exists(filePath))
            {
                try
                {
                    result = File.ReadAllLines(filePath);
                    if (result.Length < 4) result = null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load key file '{filePath}': {ex.Message}");
                }
            }
            return result;
        }

        public PrivateKey? LoadPrivateKey(string username)
        {
            string[]? keyLines = LoadKeyFile($"{username}.key");
            if (keyLines == null) return null;

            try
            {
                return new PrivateKey
                {
                    P = BigInteger.Parse(keyLines[0]),
                    Q = BigInteger.Parse(keyLines[1]),
                    G = BigInteger.Parse(keyLines[2]),
                    X = BigInteger.Parse(keyLines[3])
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to parse private key: {ex.Message}");
            }
            return null;
        }

        public PublicKey? LoadPublicKey(string keyFilename)
        {
            string[]? keyLines = LoadKeyFile(keyFilename);
            if (keyLines == null) return null;

            try
            {
                return new PublicKey
                {
                    P = BigInteger.Parse(keyLines[0]),
                    Q = BigInteger.Parse(keyLines[1]),
                    G = BigInteger.Parse(keyLines[2]),
                    Y = BigInteger.Parse(keyLines[3])
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to parse public key: {ex.Message}");
            }
            return null;
        }

        // Шифрування файлу для адресата з використанням його публічного ключа
        // та збереження результату в outputFile
        public void EncryptFile(string inputFile, PublicKey recipientKey, string outputFile)
        {
            if (recipientKey == null)
                throw new ArgumentNullException(nameof(recipientKey), "Публічний ключ адресата не завантажено.");

            BigInteger p = recipientKey.P;
            BigInteger q = recipientKey.Q;
            BigInteger g = recipientKey.G;
            BigInteger y = recipientKey.Y;

            // оригінальний розмір файлу зберігаємо в першому рядку файлу шифротексту
            // для спрощення подальшого відновлення (розшифрування)
            long originalLength = new FileInfo(inputFile).Length;

            using (StreamWriter writer = new StreamWriter(outputFile))
            using (FileStream fs = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                // розмір повідомлення
                writer.WriteLine(originalLength.ToString());
                // буфер для читання блоків
                byte[] buffer = new byte[BLOCK_SIZE];
                // кількість прочитаних байт
                int bytesRead;

                while ((bytesRead = reader.Read(buffer, 0, BLOCK_SIZE)) > 0)
                {
                    byte[] blockToEncrypt;
                    if (bytesRead < BLOCK_SIZE)
                    {
                        //якщо прочитано менше, ніж розмір блоку, створюємо новий масив потрібного розміру
                        //та копіюємо туди прочитані байти
                        //решта байтів за замовчуванням буде нулями
                        blockToEncrypt = new byte[BLOCK_SIZE];
                        Array.Copy(buffer, blockToEncrypt, bytesRead);
                    }
                    else
                    {
                        //інакше використовуємо повний блок
                        blockToEncrypt = buffer;
                    }

                    //беззнакове велике ціле з блоку байтів
                    BigInteger m = new BigInteger(blockToEncrypt, isUnsigned: true);
                    // переконуємося, що m не дорівнює нулю
                    // оскільки в алгоритмі Ель-Гамаля m повинно бути в діапазоні [1, p-1]
                    if (m.IsZero) m = 1;

                    // сеансовий ключ k - випадкове ціле в діапазоні [2, q-1]
                    BigInteger k = ElGamalCrypto.GenerateRandomBigInteger(2, q);
                    // обчислення компонентів шифротексту
                    // a = (g^k) mod p
                    BigInteger a = BigInteger.ModPow(g, k, p);
                    // b = (y^k * m) mod p
                    BigInteger b = (BigInteger.ModPow(y, k, p) * m) % p;

                    writer.WriteLine(a.ToString());
                    writer.WriteLine(b.ToString());
                }
            }
        }

        // Розшифрування вхідного файлу з використанням приватного ключа користувача
        // та збереження результату в outputFile
        public void DecryptFile(string inputFile, PrivateKey userKey, string outputFile)
        {
            if (userKey == null)
                throw new ArgumentNullException(nameof(userKey), "Приватний ключ не завантажено.");

            BigInteger p = userKey.P;
            BigInteger x = userKey.X;

            // всі рядки файлу шифротексту
            string[] cipherLines = File.ReadAllLines(inputFile);

            // перевірка цілісності файлу шифротексту
            // перший рядок - оригінальна довжина файлу
            // далі йдуть пари рядків (a, b)
            if (cipherLines.Length < 1 || (cipherLines.Length - 1) % 2 != 0)
            {
                throw new InvalidDataException("Файл шифротексту пошкоджений (неправильна кількість рядків).");
            }

            long originalLength = long.Parse(cipherLines[0]);
            long totalBytesWritten = 0;

            using (FileStream fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                for (int i = 1; i < cipherLines.Length; i += 2)
                {
                    BigInteger a = BigInteger.Parse(cipherLines[i]);
                    BigInteger b = BigInteger.Parse(cipherLines[i + 1]);

                    // розшифрування блоку
                    // m = (b * a^(p-1-x)) mod p
                    BigInteger ax_inv = BigInteger.ModPow(a, p - 1 - x, p);
                    BigInteger m = (b * ax_inv) % p;

                    byte[] m_bytes = m.ToByteArray(isUnsigned: true);

                    // оскільки останній блок може бути меншим за BLOCK_SIZE,
                    // враховуємо це при записі розшифрованих даних
                    byte[] decryptedBlock = new byte[BLOCK_SIZE];
                    int bytesToCopy = Math.Min(m_bytes.Length, BLOCK_SIZE);
                    // зважаючи на обернений порядок байтів у BigInteger.ToByteArray,
                    // копіюємо з кінця масиву
                    int sourceOffset = Math.Max(0, m_bytes.Length - BLOCK_SIZE);
                    Array.Copy(m_bytes, sourceOffset, decryptedBlock, 0, bytesToCopy);

                    long bytesRemaining = originalLength - totalBytesWritten;
                    int bytesToWrite = (int)Math.Min(BLOCK_SIZE, bytesRemaining);

                    if (bytesToWrite > 0)
                    {
                        writer.Write(decryptedBlock, 0, bytesToWrite);
                        totalBytesWritten += bytesToWrite;
                    }
                }
            }
        }
    }
}