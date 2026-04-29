using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelPatterns
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== Підготовка даних ===");
            
            int numHtmlDocs = 2000;
            var htmlDocs = GenerateHtmlDocuments(numHtmlDocs);
            Console.WriteLine($"Згенеровано {numHtmlDocs} HTML документів.");
            
            int arraySize = 5_000_000;
            var numbers = GenerateRandomArray(arraySize);
            Console.WriteLine($"Згенеровано масив із {arraySize} чисел.");
            
            int matrixSize = 500;
            var matrixA = GenerateMatrix(matrixSize);
            var matrixB = GenerateMatrix(matrixSize);
            Console.WriteLine($"Згенеровано дві матриці {matrixSize}x{matrixSize}.\n");

            int coreCount = Environment.ProcessorCount;
            
            Console.WriteLine("--- ЗАДАЧА 1: Підрахунок тегів у HTML ---");
            
            Measure("Sequential HTML", () => SequentialHtmlTags(htmlDocs));
            
            Measure($"Map-Reduce HTML (PLINQ, Threads: 4)", 
                () => MapReduceHtmlTags(htmlDocs, 4));
            Measure($"Map-Reduce HTML (PLINQ, Threads: 8)", 
                () => MapReduceHtmlTags(htmlDocs, 8));
            Measure($"Map-Reduce HTML (PLINQ, Threads: {coreCount})", 
                () => MapReduceHtmlTags(htmlDocs, coreCount));
                
            Measure($"Worker Pool HTML (Threads: 4)", 
                () => WorkerPoolHtmlTags(htmlDocs, 4));
            Measure($"Worker Pool HTML (Threads: 8)", 
                () => WorkerPoolHtmlTags(htmlDocs, 8));
            Measure($"Worker Pool HTML (Threads: 16)",
                () => WorkerPoolHtmlTags(htmlDocs, 16));

            Measure($"Fork-Join HTML (Threshold: {numHtmlDocs/10})",
                () => ForkJoinHtmlTags(htmlDocs, 0, htmlDocs.Count, numHtmlDocs / 10));

            Console.WriteLine();
            
            Console.WriteLine("--- ЗАДАЧА 2: Статистика великого масиву ---");
            // Медіана потребує сортування, ми розпаралелюємо знаходження Min, Max, Sum
            
            Measure("Sequential Stats", () => SequentialStats(numbers));
            
            Measure($"Fork-Join Stats (Threshold: {arraySize/10})", 
                () => ForkJoinStats(numbers, 0, numbers.Length, numbers.Length / 10));
                
            Measure($"Map-Reduce Stats (PLINQ, (Threads: 4)", 
                () => MapReduceStats(numbers, 4));
            Measure($"Map-Reduce Stats (PLINQ, (Threads: 8)", 
                () => MapReduceStats(numbers, 8));
            Measure($"Map-Reduce Stats (PLINQ, Threads: {coreCount})",
                () => MapReduceStats(numbers, coreCount));

            Measure($"Worker Pool Stats (Threads: 4)",
                () => WorkerPoolStats(numbers, 4));
            Measure($"Worker Pool Stats (Threads: 8)",
                () => WorkerPoolStats(numbers, 8));
            Measure($"Worker Pool Stats (Threads: {coreCount})",
                () => WorkerPoolStats(numbers, coreCount));

            Console.WriteLine();
            
            Console.WriteLine("--- ЗАДАЧА 3: Множення матриць ---");
            
            Measure("Sequential Matrix Mult", () => SequentialMatrixMult(matrixA, matrixB, matrixSize));
            
            Measure($"Worker Pool Matrix (Threads: 4)", 
                () => WorkerPoolMatrixMult(matrixA, matrixB, matrixSize, 4));
            
            Measure($"Worker Pool Matrix (Threads: 8)", 
                () => WorkerPoolMatrixMult(matrixA, matrixB, matrixSize, 8));
            
            Measure($"Worker Pool Matrix (Threads: {coreCount})", 
                () => WorkerPoolMatrixMult(matrixA, matrixB, matrixSize, coreCount));
                
            Measure($"Worker Pool Matrix (Overprovisioned Threads: {coreCount * 4})",
                () => WorkerPoolMatrixMult(matrixA, matrixB, matrixSize, coreCount * 4));

            Measure($"Fork-Join Matrix (Threshold: {matrixSize/10})",
                () => ForkJoinMatrixMult(matrixA, matrixB, matrixSize, matrixSize / 10));

            Measure($"Map-Reduce Matrix (PLINQ, Threads: 4)",
                () => MapReduceMatrixMult(matrixA, matrixB, matrixSize, 4));
            Measure($"Map-Reduce Matrix (PLINQ, Threads: 8)",
                () => MapReduceMatrixMult(matrixA, matrixB, matrixSize, 8));
            Measure($"Map-Reduce Matrix (PLINQ, Threads: {coreCount})",
                () => MapReduceMatrixMult(matrixA, matrixB, matrixSize, coreCount));
        }
        
        static void Measure(string name, Action action)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var sw = Stopwatch.StartNew();
            action();
            sw.Stop();
            Console.WriteLine($"{name,-50} | Час: {sw.ElapsedMilliseconds} мс");
        }

        // ==========================================
        // ЗАДАЧА 1: підрахунок тегів у HTML 
        // ==========================================
        static Dictionary<string, int> SequentialHtmlTags(List<string> docs)
        {
            var result = new Dictionary<string, int>();
            Regex regex = new Regex(@"<([a-z1-6]+)(?:\s+[^>]+)?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            
            foreach (var doc in docs)
            {
                var matches = regex.Matches(doc);
                foreach (Match match in matches)
                {
                    string tag = match.Groups[1].Value.ToLower();
                    if (!result.ContainsKey(tag)) result[tag] = 0;
                    result[tag]++;
                }
            }
            return result;
        }

        static Dictionary<string, int> MapReduceHtmlTags(List<string> docs, int dop)
        {
            Regex regex = new Regex(@"<([a-z1-6]+)(?:\s+[^>]+)?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            
            return docs.AsParallel()
                .WithDegreeOfParallelism(dop)
                .Select(doc => 
                {
                    // MAP
                    var localDict = new Dictionary<string, int>();
                    var matches = regex.Matches(doc);
                    foreach (Match match in matches)
                    {
                        string tag = match.Groups[1].Value.ToLower();
                        if (!localDict.ContainsKey(tag)) localDict[tag] = 0;
                        localDict[tag]++;
                    }
                    return localDict;
                })
                // REDUCE
                .Aggregate(
                    new Dictionary<string, int>(),
                    (mainDict, localDict) => 
                    {
                        foreach (var kvp in localDict)
                        {
                            if (!mainDict.ContainsKey(kvp.Key)) mainDict[kvp.Key] = 0;
                            mainDict[kvp.Key] += kvp.Value;
                        }
                        return mainDict;
                    }
                );
        }

        static Dictionary<string, int> WorkerPoolHtmlTags(List<string> docs, int workers)
        {
            var globalDict = new ConcurrentDictionary<string, int>();
            var queue = new BlockingCollection<string>();
            Regex regex = new Regex(@"<([a-z1-6]+)(?:\s+[^>]+)?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            var tasks = new Task[workers];
            for (int i = 0; i < workers; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    foreach (var doc in queue.GetConsumingEnumerable())
                    {
                        var matches = regex.Matches(doc);
                        foreach (Match match in matches)
                        {
                            string tag = match.Groups[1].Value.ToLower();
                            globalDict.AddOrUpdate(tag, 1, (key, oldValue) => oldValue + 1);
                        }
                    }
                });
            }

            foreach (var doc in docs) queue.Add(doc);
            queue.CompleteAdding();
            Task.WaitAll(tasks);

            return new Dictionary<string, int>(globalDict);
        }

        static Dictionary<string, int> ForkJoinHtmlTags(List<string> docs, int start, int end, int threshold)
        {
            Regex regex = new Regex(@"<([a-z1-6]+)(?:\s+[^>]+)?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            if (end - start <= threshold)
            {
                var result = new Dictionary<string, int>();
                for (int idx = start; idx < end; idx++)
                {
                    var matches = regex.Matches(docs[idx]);
                    foreach (Match match in matches)
                    {
                        string tag = match.Groups[1].Value.ToLower();
                        if (!result.ContainsKey(tag)) result[tag] = 0;
                        result[tag]++;
                    }
                }
                return result;
            }

            int mid = start + (end - start) / 2;
            var t1 = Task.Run(() => ForkJoinHtmlTags(docs, start, mid, threshold));
            var t2 = Task.Run(() => ForkJoinHtmlTags(docs, mid, end, threshold));
            Task.WaitAll(t1, t2);

            var r1 = t1.Result;
            var r2 = t2.Result;
            foreach (var kvp in r2)
            {
                if (!r1.ContainsKey(kvp.Key)) r1[kvp.Key] = 0;
                r1[kvp.Key] += kvp.Value;
            }
            return r1;
        }

        // ==========================================
        // ЗАДАЧА 2: Знайти мінімум, максимум, медіану та середнє значення масиву з > 1 000 000 чисел
        // ==========================================
        struct ArrayStats
        {
            public double Min, Max, Sum;
            public int Count;
        }

        static void SequentialStats(double[] arr)
        {
            double min = double.MaxValue, max = double.MinValue, sum = 0;
            for(int i = 0; i < arr.Length; i++)
            {
                if (arr[i] < min) min = arr[i];
                if (arr[i] > max) max = arr[i];
                sum += arr[i];
            }
            double avg = sum / arr.Length;
            
            // Медіана
            var copy = new double[arr.Length];
            Array.Copy(arr, copy, arr.Length);
            Array.Sort(copy);
            double median = copy[copy.Length / 2];
        }

        static ArrayStats ForkJoinStats(double[] arr, int start, int end, int threshold)
        {
            if (end - start <= threshold)
            {
                // Послідовне обчислення для малих шматків
                double min = double.MaxValue, max = double.MinValue, sum = 0;
                for (int i = start; i < end; i++)
                {
                    if (arr[i] < min) min = arr[i];
                    if (arr[i] > max) max = arr[i];
                    sum += arr[i];
                }
                return new ArrayStats { Min = min, Max = max, Sum = sum, Count = end - start };
            }

            int mid = start + (end - start) / 2;
            var t1 = Task.Run(() => ForkJoinStats(arr, start, mid, threshold));
            var t2 = Task.Run(() => ForkJoinStats(arr, mid, end, threshold));
            Task.WaitAll(t1, t2);

            var r1 = t1.Result;
            var r2 = t2.Result;

            return new ArrayStats
            {
                Min = Math.Min(r1.Min, r2.Min),
                Max = Math.Max(r1.Max, r2.Max),
                Sum = r1.Sum + r2.Sum,
                Count = r1.Count + r2.Count
            };
        }

        static ArrayStats MapReduceStats(double[] arr, int dop)
        {
            return arr.AsParallel()
                .WithDegreeOfParallelism(dop)
                .Aggregate(
                    // Локальний акумулятор для кожного потоку
                    () => new ArrayStats { Min = double.MaxValue, Max = double.MinValue, Sum = 0, Count = 0 },
                    (local, val) =>
                    {
                        if (val < local.Min) local.Min = val;
                        if (val > local.Max) local.Max = val;
                        local.Sum += val;
                        local.Count++;
                        return local;
                    },
                    // Злиття (Reduce) локальних акумуляторів
                    (main, local) => new ArrayStats
                    {
                        Min = Math.Min(main.Min, local.Min),
                        Max = Math.Max(main.Max, local.Max),
                        Sum = main.Sum + local.Sum,
                        Count = main.Count + local.Count
                    },
                    final => final
                );
        }

        static ArrayStats WorkerPoolStats(double[] arr, int workers)
        {
            int chunkSize = arr.Length / workers;
            var results = new ArrayStats[workers];
            var tasks = new Task[workers];

            for (int w = 0; w < workers; w++)
            {
                int workerIndex = w;
                int start = w * chunkSize;
                int end = (w == workers - 1) ? arr.Length : (w + 1) * chunkSize;

                tasks[w] = Task.Run(() =>
                {
                    double min = double.MaxValue, max = double.MinValue, sum = 0;
                    int count = 0;

                    for (int i = start; i < end; i++)
                    {
                        if (arr[i] < min) min = arr[i];
                        if (arr[i] > max) max = arr[i];
                        sum += arr[i];
                        count++;
                    }

                    results[workerIndex] = new ArrayStats { Min = min, Max = max, Sum = sum, Count = count };
                });
            }

            Task.WaitAll(tasks);

            // Combine results
            var combined = new ArrayStats { Min = double.MaxValue, Max = double.MinValue, Sum = 0, Count = 0 };
            foreach (var r in results)
            {
                if (r.Min < combined.Min) combined.Min = r.Min;
                if (r.Max > combined.Max) combined.Max = r.Max;
                combined.Sum += r.Sum;
                combined.Count += r.Count;
            }

            return combined;
        }

        // ==========================================
        // ЗАДАЧА 3: Помножити дві матриці великого розміру із > 1 000 чисел
        // ==========================================
        static double[,] SequentialMatrixMult(double[,] A, double[,] B, int size)
        {
            double[,] C = new double[size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < size; k++) sum += A[i, k] * B[k, j];
                    C[i, j] = sum;
                }
            }
            return C;
        }

        static double[,] WorkerPoolMatrixMult(double[,] A, double[,] B, int size, int workers)
        {
            double[,] C = new double[size, size];
            var queue = new BlockingCollection<int>(); // Черга індексів рядків

            var tasks = new Task[workers];
            for(int w = 0; w < workers; w++)
            {
                tasks[w] = Task.Run(() =>
                {
                    foreach(var i in queue.GetConsumingEnumerable())
                    {
                        for (int j = 0; j < size; j++)
                        {
                            double sum = 0;
                            for (int k = 0; k < size; k++) sum += A[i, k] * B[k, j];
                            C[i, j] = sum;
                        }
                    }
                });
            }

            for (int i = 0; i < size; i++) queue.Add(i);
            queue.CompleteAdding();
            Task.WaitAll(tasks);

            return C;
        }

        static double[,] ForkJoinMatrixMult(double[,] A, double[,] B, int size, int threshold)
        {
            double[,] C = new double[size, size];
            ForkJoinMatrixMultHelper(A, B, C, size, 0, size, threshold);
            return C;
        }

        static void ForkJoinMatrixMultHelper(double[,] A, double[,] B, double[,] C, int size, int startRow, int endRow, int threshold)
        {
            if (endRow - startRow <= threshold)
            {
                // computation for small chunks
                for (int i = startRow; i < endRow; i++)
                {
                    for (int j = 0; j < size; j++)
                    {
                        double sum = 0;
                        for (int k = 0; k < size; k++) sum += A[i, k] * B[k, j];
                        C[i, j] = sum;
                    }
                }
                return;
            }

            int mid = startRow + (endRow - startRow) / 2;
            var t1 = Task.Run(() => ForkJoinMatrixMultHelper(A, B, C, size, startRow, mid, threshold));
            var t2 = Task.Run(() => ForkJoinMatrixMultHelper(A, B, C, size, mid, endRow, threshold));
            Task.WaitAll(t1, t2);
        }

        static double[,] MapReduceMatrixMult(double[,] A, double[,] B, int size, int dop)
        {
            double[,] C = new double[size, size];

            // Map: process each row in parallel
            Enumerable.Range(0, size)
                .AsParallel()
                .WithDegreeOfParallelism(dop)
                .ForAll(i =>
                {
                    for (int j = 0; j < size; j++)
                    {
                        double sum = 0;
                        for (int k = 0; k < size; k++) sum += A[i, k] * B[k, j];
                        C[i, j] = sum;
                    }
                });

            return C;
        }

        // ==========================================
        // ГЕНЕРАТОРИ
        // ==========================================
        static List<string> GenerateHtmlDocuments(int count)
        {
            var tags = new[] { "div", "span", "a", "p", "h1", "h2", "ul", "li", "table", "tr", "td", "img" };
            var rnd = new Random(42);
            var docs = new List<string>(count);

            for (int i = 0; i < count; i++)
            {
                int numTags = rnd.Next(50, 500);
                var doc = new System.Text.StringBuilder();
                doc.Append("<html><body>");
                for (int j = 0; j < numTags; j++)
                {
                    string tag = tags[rnd.Next(tags.Length)];
                    doc.Append($"<{tag} class='x'>Text</{tag}>");
                }
                doc.Append("</body></html>");
                docs.Add(doc.ToString());
            }
            return docs;
        }

        static double[] GenerateRandomArray(int size)
        {
            var rnd = new Random(123);
            var arr = new double[size];
            for(int i = 0; i < size; i++) arr[i] = rnd.NextDouble() * 10000.0;
            return arr;
        }

        static double[,] GenerateMatrix(int size)
        {
            var rnd = new Random(777);
            var matrix = new double[size, size];
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    matrix[i, j] = rnd.NextDouble() * 10.0;
            return matrix;
        }
    }
}