using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using auth_elgamal.Models.Keys;

namespace auth_elgamal.Services
{
    public class ElGamalService
    {
        // Розмір блоку в байтах (з вашого коду)
        private const int BLOCK_SIZE = 26;

        // --- 1. Керування ключами (з HandleGeneration) ---

        public bool GenerateKeys(string username)
        {
            try
            {
                string privateKeyFile = $"{username}.key";
                string publicKeyFile = $"{username}.pub";

                // Використовуємо вашу логіку генерації
                (BigInteger p, BigInteger q) = ElGamal.GenerateSafePrime(64);
                BigInteger g = ElGamal.GenerateG(p, q);
                BigInteger x = ElGamal.GenerateRandomBigInteger(2, q);
                BigInteger y = BigInteger.ModPow(g, x, p);

                // Збереження ключів (4 параметри)
                File.WriteAllLines(privateKeyFile, new[] { p.ToString(), q.ToString(), g.ToString(), x.ToString() });
                File.WriteAllLines(publicKeyFile, new[] { p.ToString(), q.ToString(), g.ToString(), y.ToString() });

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Key Gen Error: {ex.Message}");
                return false;
            }
        }

        public PrivateKey LoadPrivateKey(string username)
        {
            string privateKeyPath = $"{username}.key";
            if (!File.Exists(privateKeyPath))
            {
                return null;
            }

            try
            {
                string[] keyLines = File.ReadAllLines(privateKeyPath);
                if (keyLines.Length < 4) return null; // Неправильний формат

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
                System.Diagnostics.Debug.WriteLine($"Failed to load private key: {ex.Message}");
                return null;
            }
        }

        public PublicKey LoadPublicKey(string keyFilename)
        {
            if (!File.Exists(keyFilename))
            {
                return null;
            }

            try
            {
                string[] keyLines = File.ReadAllLines(keyFilename);
                if (keyLines.Length < 4) return null; // Неправильний формат

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
                System.Diagnostics.Debug.WriteLine($"Failed to load public key: {ex.Message}");
                return null;
            }
        }


        // --- 2. Шифрування (з HandleEncryption, адаптовано для string) ---

        public void EncryptFile(string inputFile, PublicKey recipientKey, string outputFile)
        {
            if (recipientKey == null)
                throw new ArgumentNullException(nameof(recipientKey), "Публічний ключ адресата не завантажено.");

            // Беремо параметри з об'єкта ключа
            BigInteger p = recipientKey.P;
            BigInteger q = recipientKey.Q;
            BigInteger g = recipientKey.G;
            BigInteger y = recipientKey.Y;

            long originalLength = new FileInfo(inputFile).Length;

            using (StreamWriter writer = new StreamWriter(outputFile)) // Запис тексту (a, b)
            using (FileStream fs = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs)) // Читання байтів
            {
                writer.WriteLine(originalLength.ToString());

                byte[] buffer = new byte[BLOCK_SIZE];
                int bytesRead;

                while ((bytesRead = reader.Read(buffer, 0, BLOCK_SIZE)) > 0)
                {
                    byte[] blockToEncrypt;
                    if (bytesRead < BLOCK_SIZE)
                    {
                        blockToEncrypt = new byte[BLOCK_SIZE];
                        Array.Copy(buffer, blockToEncrypt, bytesRead);
                    }
                    else
                    {
                        blockToEncrypt = buffer;
                    }

                    BigInteger m = new BigInteger(blockToEncrypt, isUnsigned: true);
                    if (m.IsZero) m = 1;

                    // --- Шифрування (Ваша логіка) ---
                    BigInteger k = ElGamal.GenerateRandomBigInteger(2, q);
                    BigInteger a = BigInteger.ModPow(g, k, p);
                    BigInteger b = (BigInteger.ModPow(y, k, p) * m) % p;

                    writer.WriteLine(a.ToString());
                    writer.WriteLine(b.ToString());
                }
            }
        }

        // --- 3. Розшифрування (з HandleDecryption, адаптовано для string) ---

        public void DecryptFile(string inputFile, PrivateKey userKey, string outputFile)
        {
            if (userKey == null)
                throw new ArgumentNullException(nameof(userKey), "Приватний ключ не завантажено.");

            // Беремо параметри з об'єкта ключа
            BigInteger p = userKey.P;
            BigInteger x = userKey.X;

            string[] cipherLines = File.ReadAllLines(inputFile); // Читання тексту (a, b)

            if (cipherLines.Length < 1 || (cipherLines.Length - 1) % 2 != 0)
            {
                throw new InvalidDataException("Файл шифротексту пошкоджений (неправильна кількість рядків).");
            }

            long originalLength = long.Parse(cipherLines[0]);
            long totalBytesWritten = 0;

            using (FileStream fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(fs)) // Запис байтів
            {
                for (int i = 1; i < cipherLines.Length; i += 2)
                {
                    BigInteger a = BigInteger.Parse(cipherLines[i]);
                    BigInteger b = BigInteger.Parse(cipherLines[i + 1]);

                    // --- Розшифрування (Ваша логіка) ---
                    BigInteger ax_inv = BigInteger.ModPow(a, p - 1 - x, p);
                    BigInteger m = (b * ax_inv) % p;

                    byte[] m_bytes = m.ToByteArray(isUnsigned: true);

                    byte[] decryptedBlock = new byte[BLOCK_SIZE];
                    int bytesToCopy = Math.Min(m_bytes.Length, BLOCK_SIZE);
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

            // (Помилка з 'ms' та 'writer' з минулого разу тут відсутня, 
            // оскільки ми пишемо прямо у FileStream)
        }


        // --- 4. Внутрішній клас ElGamal (ПОВНІСТЮ СКОПІЙОВАНО З ВАШОГО КОДУ) ---
        private static class ElGamal
        {
            private const int MillerRabinIterations = 40;

            public static (BigInteger p, BigInteger q) GenerateSafePrime(int numDigits)
            {
                BigInteger p_min = BigInteger.Parse("1" + new string('0', numDigits - 1));
                BigInteger p_max = BigInteger.Parse("1" + new string('0', numDigits));

                while (true)
                {
                    BigInteger q_min = p_min / 2;
                    BigInteger q_max = p_max / 2;
                    BigInteger q_candidate = GenerateRandomBigInteger(q_min, q_max);
                    if (q_candidate.IsEven) q_candidate++;

                    while (q_candidate < q_max)
                    {
                        if (IsProbablyPrime(q_candidate, MillerRabinIterations))
                        {
                            BigInteger p_candidate = 2 * q_candidate + 1;
                            if (p_candidate >= p_max) break;
                            if (IsProbablyPrime(p_candidate, MillerRabinIterations))
                            {
                                if (p_candidate >= p_min)
                                {
                                    return (p_candidate, q_candidate);
                                }
                            }
                        }
                        q_candidate += 2;
                    }
                }
            }

            public static BigInteger GenerateG(BigInteger p, BigInteger q)
            {
                BigInteger h = 2;
                while (true)
                {
                    BigInteger g = BigInteger.ModPow(h, 2, p);
                    if (g == 1)
                    {
                        h++;
                        continue;
                    }
                    return g;
                }
            }

            public static BigInteger GenerateRandomBigInteger(BigInteger min, BigInteger max)
            {
                if (min >= max)
                    throw new ArgumentException("min повинен бути меншим за max");

                BigInteger range = max - min;
                int byteLen = range.ToByteArray().Length;
                BigInteger result;
                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                {
                    do
                    {
                        byte[] bytes = new byte[byteLen];
                        rng.GetBytes(bytes);
                        result = new BigInteger(bytes, isUnsigned: true);
                    }
                    while (result >= range);
                }
                return result + min;
            }

            public static bool IsProbablyPrime(BigInteger n, int k)
            {
                if (n < 2) return false;
                if (n == 2 || n == 3) return true;
                if (n.IsEven) return false;

                BigInteger d = n - 1;
                int s = 0;
                while (d % 2 == 0)
                {
                    d /= 2;
                    s++;
                }

                for (int i = 0; i < k; i++)
                {
                    BigInteger a = GenerateRandomBigInteger(2, n - 1);
                    BigInteger x = BigInteger.ModPow(a, d, n);
                    if (x == 1 || x == n - 1)
                        continue;

                    bool isComposite = true;
                    for (int r = 1; r < s; r++)
                    {
                        x = BigInteger.ModPow(x, 2, n);
                        if (x == 1) return false;
                        if (x == n - 1)
                        {
                            isComposite = false;
                            break;
                        }
                    }
                    if (isComposite)
                        return false;
                }
                return true;
            }
        }
    }
}