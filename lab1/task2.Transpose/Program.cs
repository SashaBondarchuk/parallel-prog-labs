using System.Diagnostics;
using System.Text;

UTF8Encoding encoding = new UTF8Encoding();
Console.OutputEncoding = encoding;

const int size = 10_000;

Console.WriteLine($"Транспонування матриці {size} x {size}");

var matrix = GenerateMatrix(size);

Console.WriteLine("Послідновний");

RunAndMeasure(() => TransposeSequential(matrix));


Console.WriteLine("\nПаралельний");

int maxThreads = Environment.ProcessorCount;

for (int threads = 2; threads <= maxThreads; threads++)
{
    Console.WriteLine($"Потоки: {threads}");

    RunAndMeasure(() => TransposeParallel(matrix, threads));
}


static int[,] GenerateMatrix(int size)
{
    var matrix = new int[size, size];
    var rand = new Random(42);

    for (int i = 0; i < size; i++)
    {
        for (int j = 0; j < size; j++)
        {
            matrix[i, j] = rand.Next(0, 100);
        }
    }

    return matrix;
}

static int[,] TransposeSequential(int[,] matrix)
{
    int n = matrix.GetLength(0);
    int[,] result = new int[n, n];

    for (int i = 0; i < n; i++)
    {
        for (int j = 0; j < n; j++)
        {
            result[j, i] = matrix[i, j];
        }
    }

    return result;
}

static int[,] TransposeParallel(int[,] matrix, int threads)
{
    int n = matrix.GetLength(0);
    int[,] result = new int[n, n];

    Parallel.For(
        0,
        n,
        new ParallelOptions { MaxDegreeOfParallelism = threads },
        i =>
        {
            for (int j = 0; j < n; j++)
            {
                result[j, i] = matrix[i, j];
            }
        });

    return result;
}

static void RunAndMeasure(Func<int[,]> action)
{
    Stopwatch sw = Stopwatch.StartNew();
    var result = action();
    sw.Stop();

    Console.WriteLine("Elapsed milliseconds: {0}", sw.ElapsedMilliseconds);
}



























class Factory
{
    private int _count;

    public void AddItem()
    {
        _count++;
    }

    public void RemoveItem()
    {
        _count--;
    }
}











