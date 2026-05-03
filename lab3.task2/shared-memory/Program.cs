using System.Diagnostics;
using System.IO.MemoryMappedFiles;

namespace shared_memory;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        const int iterations = 1000;
        var timings = new List<double>();
        Random rnd = new Random();

        // Create shared memory - only need space for int and iteration count
        using var mmf = MemoryMappedFile.CreateOrOpen("LabSharedMem", 8); // 4 bytes int + 4 bytes iteration count
        using var accessor = mmf.CreateViewAccessor();

        // Create named semaphores for synchronization
        using var semDataReady = new Semaphore(0, 1, "LabSemDataReady");      // C# signals when data is written
        using var semDataRead = new Semaphore(0, 1, "LabSemDataRead");        // Python signals when data is read
        using var semPythonReady = new Semaphore(0, 1, "LabSemPythonReady");  // Python signals when initialized

        // Write iteration count
        accessor.Write(4, iterations);

        // Start Python process once
        Console.WriteLine("[C#] Starting Python process...");
        var pythonProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "python",
            Arguments = "C:\\Users\\sasha\\source\\repos\\parallel-prog-labs\\lab3.task2\\shared-memory\\lab3-shared-memory.py",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        });

        if (pythonProcess == null)
        {
            Console.WriteLine("[C#] Failed to start Python process!");
            return;
        }

        // Wait for Python to signal it's ready
        Console.WriteLine("[C#] Waiting for Python to be ready...");
        if (!semPythonReady.WaitOne(10000))
        {
            Console.WriteLine($"[C#] Timeout waiting for Python to initialize!");
            if (pythonProcess.HasExited)
            {
                Console.WriteLine($"Python exit code: {pythonProcess.ExitCode}");
                Console.WriteLine($"Python stderr: {pythonProcess.StandardError.ReadToEnd()}");
            }
            pythonProcess?.Kill();
            return;
        }

        Console.WriteLine($"[C#] Python ready. Starting {iterations} iterations...");
        var totalStopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            int number = rnd.Next(1000, 9999);

            var sw = Stopwatch.StartNew();

            // Write number to shared memory
            accessor.Write(0, number);

            // Signal Python that data is ready
            semDataReady.Release();

            // Wait for Python to signal it has read the data
            if (!semDataRead.WaitOne(5000))
            {
                Console.WriteLine($"Timeout at iteration {i}");
                pythonProcess?.Kill();
                return;
            }

            sw.Stop();
            timings.Add(sw.Elapsed.TotalMilliseconds);

            if ((i + 1) % 100 == 0)
                Console.WriteLine($"  Completed {i + 1}/{iterations} iterations");
        }

        totalStopwatch.Stop();

        pythonProcess?.WaitForExit();

        if (pythonProcess?.ExitCode != 0)
        {
            Console.WriteLine($"Python error: {pythonProcess?.StandardError.ReadToEnd()}");
        }

        // Statistics
        double avgTime = timings.Average();
        double minTime = timings.Min();
        double maxTime = timings.Max();
        double medianTime = timings.OrderBy(x => x).ElementAt(timings.Count / 2);

        Console.WriteLine("\n=== Shared Memory Performance Results (Semaphores) ===");
        Console.WriteLine($"Total iterations: {iterations}");
        Console.WriteLine($"Total time: {totalStopwatch.Elapsed.TotalMilliseconds:F2} ms");
        Console.WriteLine($"Average round-trip: {avgTime:F4} ms");
        Console.WriteLine($"Min round-trip: {minTime:F4} ms");
        Console.WriteLine($"Max round-trip: {maxTime:F4} ms");
    }
}