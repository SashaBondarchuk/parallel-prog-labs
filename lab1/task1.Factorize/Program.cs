using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using common;

UTF8Encoding encoding = new UTF8Encoding();
Console.OutputEncoding = encoding;

// ulong targetNumber = 6696413145649UL;
ulong targetNumber = ulong.MaxValue;

Console.WriteLine($"Шукаємо дільники для: {targetNumber}");
Console.WriteLine("Послідновний");

RunAndMeasure(() => PrimeFactorizer.Factorize(targetNumber));

Console.WriteLine("\nПаралельний");

int maxThreads = Environment.ProcessorCount;
for (int threads = 2; threads <= maxThreads; threads++)
{
    RunAndMeasure(() => ParallelPrimeFactorizer.Factorize(targetNumber, threads), threads);
}
static void RunAndMeasure(Func<List<ulong>> action, int threads = 1)
{
    Stopwatch sw = Stopwatch.StartNew();
    var divisors = action();
    sw.Stop();

    Console.WriteLine($"Паралельний ({threads} потоків): {sw.ElapsedTicks} ticks");
}

public class PrimeFactorizer
{
    static ulong MulMod(ulong a, ulong b, ulong mod)
    {
        return (ulong)((UInt128)a * b % mod);
    }

    // Швидке піднесення до степеня: (baseValue ^ exponent) % mod
    static ulong PowerMod(ulong baseValue, ulong exponent, ulong mod)
    {
        ulong res = 1;
        baseValue %= mod;
        while (exponent > 0)
        {
            if ((exponent & 1) == 1) res = MulMod(res, baseValue, mod);
            baseValue = MulMod(baseValue, baseValue, mod);
            exponent /= 2;
        }
        return res;
    }

    // Найбільший спільний дільник (НСД) / алгоритм Евкліда
    static ulong GCD(ulong a, ulong b)
    {
        while (b != 0)
        {
            ulong temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }

    // Тест простоти Міллера-Рабіна
    // Формула: n - 1 = (2^s) * d
    static bool IsPrime(ulong n)
    {
        if (n < 2) return false;
        if (n == 2 || n == 3) return true;
        if (n % 2 == 0) return false;

        ulong d = n - 1;
        int s = 0;
        while (d % 2 == 0)
        {
            d /= 2;
            s++;
        }

        // Бази, достатні для детермінованої перевірки всіх 64-бітних чисел
        ulong[] bases = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37 };
        
        foreach (ulong a in bases)
        {
            if (n <= a) break;
            ulong x = PowerMod(a, d, n);
            if (x == 1 || x == n - 1) continue;

            bool composite = true;
            for (int r = 1; r < s; r++)
            {
                x = MulMod(x, x, n); // x = x^2 % n
                if (x == n - 1)
                {
                    composite = false;
                    break;
                }
            }
            if (composite) return false;
        }
        return true;
    }

    // Ро-алгоритм Полларда (знаходить один нетривіальний дільник)
    static ulong PollardsRho(ulong n)
    {
        if (n % 2 == 0) return 2;

        ulong x = 2, y = 2, d = 1, c = 1;
        
        // Псевдовипадкова функція: f(x) = (x^2 + c) % n
        Func<ulong, ulong> f = (val) => (MulMod(val, val, n) + c) % n;

        while (d == 1)
        {
            // Алгоритм Флойда (черепаха і заєць) для пошуку циклу
            x = f(x);         // x_{i}
            y = f(f(y));      // x_{2i}
            
            ulong diff = x > y ? x - y : y - x; // |x - y|
            d = GCD(diff, n);                   // НСД(|x - y|, n)

            if (d == n)
            {
                // Потрапили в тривіальний цикл, змінюємо початкові параметри
                var rand = new Random();
                x = (ulong)rand.NextInt64(2, (long)n - 2);
                y = x;
                c = (ulong)rand.NextInt64(1, (long)n - 1);
                d = 1;
            }
        }
        return d;
    }

    public static List<ulong> Factorize(ulong n)
    {
        List<ulong> factors = new List<ulong>();
        if (n <= 1) return factors;

        // Видаляємо всі двійки для пришвидшення
        while (n % 2 == 0)
        {
            factors.Add(2);
            n /= 2;
        }

        if (n == 1) return factors;

        Stack<ulong> stack = new Stack<ulong>();
        stack.Push(n);

        while (stack.Count > 0)
        {
            ulong current = stack.Pop();
            if (current == 1) continue;

            // Якщо число просте, додаємо його до результату
            if (IsPrime(current))
            {
                factors.Add(current);
            }
            else
            {
                // Якщо складене - знаходимо дільник і розбиваємо далі
                ulong divisor = PollardsRho(current);
                stack.Push(divisor);
                stack.Push(current / divisor);
            }
        }
        
        factors.Sort();
        return factors;
    }
}

public class ParallelPrimeFactorizer
{
    static ulong MulMod(ulong a, ulong b, ulong mod)
    {
        return (ulong)((UInt128)a * b % mod);
    }

    static ulong PowerMod(ulong baseValue, ulong exponent, ulong mod)
    {
        ulong res = 1;
        baseValue %= mod;
        while (exponent > 0)
        {
            if ((exponent & 1) == 1) res = MulMod(res, baseValue, mod);
            baseValue = MulMod(baseValue, baseValue, mod);
            exponent /= 2;
        }
        return res;
    }

    static ulong GCD(ulong a, ulong b)
    {
        while (b != 0)
        {
            ulong temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }

    static bool IsPrime(ulong n)
    {
        if (n < 2) return false;
        if (n == 2 || n == 3) return true;
        if (n % 2 == 0) return false;

        ulong d = n - 1;
        int s = 0;
        while (d % 2 == 0)
        {
            d /= 2;
            s++;
        }

        ulong[] bases = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37 };
        
        foreach (ulong a in bases)
        {
            if (n <= a) break;
            ulong x = PowerMod(a, d, n);
            if (x == 1 || x == n - 1) continue;

            bool composite = true;
            for (int r = 1; r < s; r++)
            {
                x = MulMod(x, x, n);
                if (x == n - 1)
                {
                    composite = false;
                    break;
                }
            }
            if (composite) return false;
        }
        return true;
    }

    // Паралельний Ро-алгоритм Полларда
    static ulong ParallelPollardsRho(ulong n, int maxDegreeOfParallelism)
    {
        if (n % 2 == 0) return 2;

        ulong foundDivisor = 0;
        object lockObj = new object();

        Parallel.For(
            1,
            maxDegreeOfParallelism + 1,
            new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
            (i, loopState) =>
            {
                ulong x = 2, y = 2, d = 1, c = (ulong)i;

                Func<ulong, ulong> f = (val) => (MulMod(val, val, n) + c) % n;

                while (d == 1 && !loopState.IsStopped)
                {
                    x = f(x);
                    y = f(f(y));

                    ulong diff = x > y ? x - y : y - x;
                    d = GCD(diff, n);

                    if (d == n)
                    {
                        c += (ulong)maxDegreeOfParallelism;
                        x = 2; y = 2; d = 1;
                    }
                }

                if (d > 1 && d < n && !loopState.IsStopped)
                {
                    lock (lockObj)
                    {
                        if (foundDivisor == 0)
                            foundDivisor = d;
                    }

                    loopState.Stop();
                }
            });

        return foundDivisor;
    }

    private static void FactorizeRecursive(
        ulong n,
        ConcurrentBag<ulong> factors,
        int maxDegreeOfParallelism)
    {
        if (n <= 1) return;

        if (IsPrime(n))
        {
            factors.Add(n);
            return;
        }

        ulong divisor = ParallelPollardsRho(n, maxDegreeOfParallelism);

        Parallel.Invoke(
            new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
            () => FactorizeRecursive(divisor, factors, maxDegreeOfParallelism),
            () => FactorizeRecursive(n / divisor, factors, maxDegreeOfParallelism)
        );
    }

    public static List<ulong> Factorize(ulong n, int maxDegreeOfParallelism)
    {
        ConcurrentBag<ulong> factors = new ConcurrentBag<ulong>();

        while (n % 2 == 0)
        {
            factors.Add(2);
            n /= 2;
        }

        if (n > 1)
        {
            FactorizeRecursive(n, factors, maxDegreeOfParallelism);
        }

        var result = factors.ToList();
        result.Sort();
        return result;
    }
}