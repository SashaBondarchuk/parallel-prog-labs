using System.Diagnostics;

namespace lab3.task2;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        const int iterations = 1000;
        var timings = new List<double>();

        var startInfo = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = "C:\\Users\\sasha\\source\\repos\\parallel-prog-labs\\lab3.task2\\lab3.task2\\lab3.py",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,

            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardInputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        startInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";

        using var process = Process.Start(startInfo);
        Random rnd = new Random();

        try
        {
            Console.WriteLine($"[C#] Starting {iterations} iterations of Anonymous Pipes...");
            var totalStopwatch = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                int numberToSend = rnd.Next(1000, 9999);
                var sw = Stopwatch.StartNew();

                process.StandardInput.WriteLine(numberToSend.ToString());
                process.StandardInput.Flush();

                string response = process.StandardOutput.ReadLine();

                sw.Stop();
                timings.Add(sw.Elapsed.TotalMilliseconds);

                if ((i + 1) % 100 == 0)
                    Console.WriteLine($"  Completed {i + 1}/{iterations} iterations");
            }

            totalStopwatch.Stop();
            process.StandardInput.WriteLine("exit");

            // Statistics
            double avgTime = timings.Average();
            double minTime = timings.Min();
            double maxTime = timings.Max();
            double medianTime = timings.OrderBy(x => x).ElementAt(timings.Count / 2);

            Console.WriteLine("\n=== Anonymous Pipes Performance Results ===");
            Console.WriteLine($"Total iterations: {iterations}");
            Console.WriteLine($"Total time: {totalStopwatch.Elapsed.TotalMilliseconds:F2} ms");
            Console.WriteLine($"Average round-trip: {avgTime:F4} ms");
            Console.WriteLine($"Min round-trip: {minTime:F4} ms");
            Console.WriteLine($"Max round-trip: {maxTime:F4} ms");
        }
        catch (IOException)
        {
            string pythonError = process.StandardError.ReadToEnd();
            Console.WriteLine("\n[Помилка в Python скрипті]:\n" + pythonError);
        }

        process.WaitForExit();
    }
}