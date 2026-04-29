using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

UTF8Encoding encoding = new UTF8Encoding();
Console.OutputEncoding = encoding;

long[] iterations = { 100_000_000 };

Console.WriteLine("Послідновний");
foreach (long n in iterations)
{
    Stopwatch sw = Stopwatch.StartNew();
    CalculatePiMonteCarlo(n);
    sw.Stop();
    Console.WriteLine("Elapsed milliseconds: {0}", sw.ElapsedMilliseconds);
}

Console.WriteLine("\nПаралельний");
int maxThreads = Environment.ProcessorCount;
for (int threads = 2; threads <= maxThreads; threads++)
{
    foreach (long n in iterations)
    {
        Stopwatch sw = Stopwatch.StartNew();
        ParallelMonteCarlo(n, threads);
        sw.Stop();
        Console.WriteLine($"({threads} потоків): {sw.ElapsedMilliseconds} ms");
    }
}


static void CalculatePiMonteCarlo(long totalPoints)
{
    var rand = new Random(334213);
    long insideCircle = 0;

    for (long i = 0; i < totalPoints; i++)
    {
        var x = rand.NextDouble();
        var y = rand.NextDouble();

        if (x * x + y * y <= 1.0)
            insideCircle++;
    }

    PrintResult(totalPoints, insideCircle);
}

static void ParallelMonteCarlo(long totalPoints, int threads)
{
    long insideCircle = 0;

    Parallel.For<long>(
        0,
        totalPoints,
        new ParallelOptions { MaxDegreeOfParallelism = threads },

        () => 0, // local init

        (i, state, localCount) =>
        {
            var x = Random.Shared.NextDouble();
            var y = Random.Shared.NextDouble();

            if (x * x + y * y <= 1.0)
                localCount++;

            return localCount;
        },

        localCount => Interlocked.Add(ref insideCircle, localCount)
    );

    PrintResult(totalPoints, insideCircle);
}

static void FastMonteCarlo(long totalPoints, int threads)
{
    const int vectorSize = 4;
    long insideCircle = 0;

    Parallel.For<long>(
        0,
        totalPoints / vectorSize,
        new ParallelOptions { MaxDegreeOfParallelism = threads },

        () => 0,

        (i, state, localCount) =>
        {
            ulong seed = (ulong)(i + 1) * 6364136223846793005UL;

            Span<double> xs = stackalloc double[vectorSize];
            Span<double> ys = stackalloc double[vectorSize];

            for (int j = 0; j < vectorSize; j++)
            {
                // XorShift RNG
                seed ^= seed << 13;
                seed ^= seed >> 7;
                seed ^= seed << 17;
                xs[j] = (seed & 0xFFFFFFFF) / (double)uint.MaxValue;

                seed ^= seed << 13;
                seed ^= seed >> 7;
                seed ^= seed << 17;
                ys[j] = (seed & 0xFFFFFFFF) / (double)uint.MaxValue;
            }

            // SIMD
            var vx = new Vector<double>(xs);
            var vy = new Vector<double>(ys);

            var dist = vx * vx + vy * vy;

            for (int k = 0; k < vectorSize; k++)
            {
                if (dist[k] <= 1.0)
                    localCount++;
            }

            return localCount;
        },

        localCount => Interlocked.Add(ref insideCircle, localCount)
    );

    // залишок
    long remainder = totalPoints % vectorSize;
    ulong tailSeed = 123456789;

    for (int i = 0; i < remainder; i++)
    {
        tailSeed ^= tailSeed << 13;
        tailSeed ^= tailSeed >> 7;
        tailSeed ^= tailSeed << 17;
        double x = (tailSeed & 0xFFFFFFFF) / (double)uint.MaxValue;

        tailSeed ^= tailSeed << 13;
        tailSeed ^= tailSeed >> 7;
        tailSeed ^= tailSeed << 17;
        double y = (tailSeed & 0xFFFFFFFF) / (double)uint.MaxValue;

        if (x * x + y * y <= 1.0)
            insideCircle++;
    }

    PrintResult(totalPoints, insideCircle);
}

static void PrintResult(long totalPoints, long insideCircle)
{
    var piEstimate = 4.0 * insideCircle / totalPoints;

    Console.WriteLine(
        $"Ітерацій: {totalPoints,12} | Pi: {piEstimate:F6} | Помилка: {Math.Abs(Math.PI - piEstimate):F6}");
}