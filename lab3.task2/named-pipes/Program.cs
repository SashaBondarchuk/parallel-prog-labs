using System.Diagnostics;
using System.IO.Pipes;

namespace named_pipes;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        const int iterations = 1000;
        var timings = new List<double>();
        Random rnd = new Random();

        // Create named pipe server once
        using var pipeServer = new NamedPipeServerStream("LabPipe", PipeDirection.InOut);

        // Start Python process once
        var pythonProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "python",
            Arguments = "C:\\Users\\sasha\\source\\repos\\parallel-prog-labs\\lab3.task2\\named-pipes\\lab3-named-pipes.py",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true
        });

        Console.WriteLine($"[C#] Waiting for Python to connect...");
        pipeServer.WaitForConnection();
        Console.WriteLine($"[C#] Python connected. Starting {iterations} iterations of Named Pipes...");

        // Send iteration count first
        byte[] iterBuffer = BitConverter.GetBytes(iterations);
        pipeServer.Write(iterBuffer, 0, iterBuffer.Length);
        pipeServer.Flush();

        var totalStopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            int number = rnd.Next(1000, 9999);
            byte[] buffer = BitConverter.GetBytes(number);

            var sw = Stopwatch.StartNew();

            pipeServer.Write(buffer, 0, buffer.Length);
            pipeServer.Flush();

            byte[] response = new byte[4];
            int bytesRead = pipeServer.Read(response, 0, response.Length);

            sw.Stop();
            timings.Add(sw.Elapsed.TotalMilliseconds);

            if ((i + 1) % 100 == 0)
                Console.WriteLine($"  Completed {i + 1}/{iterations} iterations");
        }

        totalStopwatch.Stop();

        pipeServer.Close();
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

        Console.WriteLine("\n=== Named Pipes Performance Results (Optimized) ===");
        Console.WriteLine($"Total iterations: {iterations}");
        Console.WriteLine($"Total time: {totalStopwatch.Elapsed.TotalMilliseconds:F2} ms");
        Console.WriteLine($"Average round-trip: {avgTime:F4} ms");
        Console.WriteLine($"Min round-trip: {minTime:F4} ms");
        Console.WriteLine($"Max round-trip: {maxTime:F4} ms");
    }
}