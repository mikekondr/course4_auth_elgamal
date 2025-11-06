using System.Numerics;
using System.Security.Cryptography;

namespace auth_elgamal.Services
{
    /// <summary>
    /// Містить низькорівневі, статичні криптографічні функції 
    /// для алгоритму Ель-Гамаля.
    /// </summary>
    internal static class ElGamalCrypto
    {
        // кількість ітерацій для тесту Миллера-Рабіна
        // (перевірка на простоту з високою ймовірністю)
        private const int MillerRabinIterations = 50;

        // Генерація безпечного простого числа p заданої довжини в десяткових знаках
        // та відповідного простого числа q, де p = 2q + 1
        public static (BigInteger p, BigInteger q) GenerateSafePrime(int numDigits)
        {
            BigInteger p_min = BigInteger.Parse("1" + new string('0', numDigits - 1));
            BigInteger p_max = BigInteger.Parse("1" + new string('0', numDigits));

            while (true)
            {
                BigInteger q_min = p_min / 2;
                BigInteger q_max = p_max / 2;
                BigInteger q_candidate = GenerateRandomBigInteger(q_min, q_max);
                // q_candidate має бути непарним, інакше не буде простим
                if (q_candidate.IsEven) q_candidate++;

                while (q_candidate < q_max)
                {
                    if (IsProbablyPrime(q_candidate, MillerRabinIterations))
                    {
                        // обчислюємо p_candidate з формули p = 2q + 1
                        BigInteger p_candidate = 2 * q_candidate + 1;
                        // якщо перевищили межу p_max, припиняємо пошук та перевіряємо нове q_candidate
                        if (p_candidate >= p_max) break;
                        // чи просте p_candidate?
                        if (IsProbablyPrime(p_candidate, MillerRabinIterations))
                        {
                            if (p_candidate >= p_min)
                            {
                                // умови виконані, повертаємо пару (p, q)
                                return (p_candidate, q_candidate);
                            }
                        }
                    }
                    // Перевіряємо наступне непарне число
                    q_candidate += 2;
                }
            }
        }

        // Пошук генератора g для групи за модулем p
        public static BigInteger GenerateG(BigInteger p, BigInteger q)
        {
            // оскільки p = 2q + 1, то g можна знайти як g = h^2 mod p,
            // де h - будь-яке число від 2 до p-2
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

        // Генерація випадкового великого цілого числа в діапазоні [min, max)
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

        // Тест Миллера-Рабіна для перевірки простоти числа n
        // k - кількість ітерацій для підвищення точності
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