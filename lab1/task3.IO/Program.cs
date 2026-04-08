using System.Diagnostics;
using System.Text;

UTF8Encoding encoding = new UTF8Encoding();
Console.OutputEncoding = encoding;

string baseDir = Path.Combine(Path.GetTempPath(), "RandomTextFiles");
int numFiles = 1000;
int wordsPerFile = 1000;

if (Directory.Exists(baseDir))
    Directory.Delete(baseDir, true);

Directory.CreateDirectory(baseDir);

Console.WriteLine($"Генеруємо {numFiles} файлів по {wordsPerFile} слів у {baseDir}...");

GenerateRandomTextFiles(baseDir, numFiles, wordsPerFile);

Console.WriteLine("Генерація завершена.\n");

Console.WriteLine("Послідновний");
RunAndMeasure(() =>
{
    long totalWords = CountWordsSequential(baseDir);
    Console.WriteLine($"Загальна кількість слів: {totalWords}");
});

Console.WriteLine("\nПаралельний");

int maxThreads = Environment.ProcessorCount;

for (int threads = 2; threads <= maxThreads; threads++)
{
    Console.WriteLine($"\nПотоки: {threads}");
    RunAndMeasure(() =>
    {
        long totalWords = CountWordsParallel(baseDir, threads);
        Console.WriteLine($"Загальна кількість слів: {totalWords}");
    });
}


static void GenerateRandomTextFiles(string directory, int numFiles, int wordsPerFile)
{
    var rand = new Random(42);
    var words = Enumerable.Range(0, 10000).Select(i => $"word{i}").ToArray();

    for (int i = 0; i < numFiles; i++)
    {
        string filePath = Path.Combine(directory, $"file_{i}.txt");
        using var writer = new StreamWriter(filePath);
        for (int w = 0; w < wordsPerFile; w++)
        {
            string word = words[rand.Next(words.Length)];
            writer.Write(word + " ");
        }
    }
}

static long CountWordsSequential(string directory)
{
    long count = 0;
    foreach (var file in Directory.GetFiles(directory, "*.txt", SearchOption.AllDirectories))
    {
        string text = File.ReadAllText(file);
        count += text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
    return count;
}

static long CountWordsParallel(string directory, int maxDegreeOfParallelism)
{
    var files = Directory.GetFiles(directory, "*.txt", SearchOption.AllDirectories);
    long total = 0;

    Parallel.ForEach(
        files,
        new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
        () => 0L,
        (file, state, localCount) =>
        {
            string text = File.ReadAllText(file);
            localCount += text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            return localCount;
        },
        localCount => Interlocked.Add(ref total, localCount)
    );

    return total;
}

static void RunAndMeasure(Action action)
{
    Stopwatch sw = Stopwatch.StartNew();
    action();
    sw.Stop();
    Console.WriteLine("Elapsed milliseconds: {0}", sw.ElapsedMilliseconds);
}