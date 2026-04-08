using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

UTF8Encoding encoding = new UTF8Encoding();
Console.OutputEncoding = encoding;

int start = 1;
int end = 100_000_000;
Console.WriteLine($"Діапазон: {start} - {end}\n");

// 🔸 Послідовний
var sw = Stopwatch.StartNew();
var primes = PrimeCalculator.GetPrimes(start, end);
sw.Stop();

Console.WriteLine("Послідовний:");
Console.WriteLine($"Час: {sw.ElapsedMilliseconds} ms");
Console.WriteLine($"Знайдено простих чисел: {primes.Count}");

// 🔸 Паралельний (різна кількість потоків)
int maxThreads = Environment.ProcessorCount;

for (int threads = 2; threads <= maxThreads; threads++)
{
    sw = Stopwatch.StartNew();
    primes = ParallelPrimeCalculator.GetPrimes(start, end, threads);
    sw.Stop();
    Console.WriteLine($"Паралельний ({threads} потоків): {sw.ElapsedMilliseconds} ms");
}    

Console.WriteLine($"Знайдено простих чисел: {primes.Count}");
// LogHelpers.LogListInOneLine(primes);

public static class PrimeCalculator
{
    public static List<int> GetPrimes(int start, int end)
    {
        var result = new List<int>();

        for (int i = Math.Max(2, start); i <= end; i++)
        {
            if (IsPrime(i))
                result.Add(i);
        }

        return result;
    }

    private static bool IsPrime(int number)
    {
        if (number < 2) return false;
        if (number == 2) return true;
        if (number % 2 == 0) return false;

        int limit = (int)Math.Sqrt(number);

        for (int i = 3; i <= limit; i += 2)
        {
            if (number % i == 0)
                return false;
        }

        return true;
    }
}

public static class ParallelPrimeCalculator
{
    public static List<int> GetPrimes(int start, int end, int degreeOfParallelism)
    {
        var result = new ConcurrentBag<int>();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = degreeOfParallelism
        };

        Parallel.For(Math.Max(2, start), end + 1, options, i =>
        {
            if (IsPrime(i))
                result.Add(i);
        });

        return result.OrderBy(x => x).ToList();
    }

    private static bool IsPrime(int number)
    {
        if (number < 2) return false;
        if (number == 2) return true;
        if (number % 2 == 0) return false;

        int limit = (int)Math.Sqrt(number);

        for (int i = 3; i <= limit; i += 2)
        {
            if (number % i == 0)
                return false;
        }

        return true;
    }
}