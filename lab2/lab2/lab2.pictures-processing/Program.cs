using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ImageProcessingPatterns
{
    // Модель "зображення"
    public record ImageFrame(int Id, string FileName, int DataSize, string Status = "Raw");

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            const int totalFrames = 100;
            Console.WriteLine($"=== Запуск обробки {totalFrames} кадрів ===\n");

            // 1. Послідовна обробка
            Measure("Sequential", () => RunSequential(totalFrames));

            // 2. Producer-Consumer
            Measure("Producer-Consumer (1 Prod, 4 Cons)", () => RunProducerConsumer(totalFrames, 4));
            Measure("Producer-Consumer (1 Prod, 8 Cons)", () => RunProducerConsumer(totalFrames, 8));
            Measure("Producer-Consumer (1 Prod, 16 Cons)", () => RunProducerConsumer(totalFrames, 16));
            Measure("Producer-Consumer (1 Prod, 32 Cons)", () => RunProducerConsumer(totalFrames, 32));

            // 3. Pipeline
            Measure("Pipeline (Stage-based parallelism)", () => RunPipeline(totalFrames));
            Measure("Pipeline (Stage-based parallelism) optimized", () => RunPipelineOptimized(totalFrames));
        }

        static void Measure(string name, Action action)
        {
            GC.Collect();
            var sw = Stopwatch.StartNew();
            action();
            sw.Stop();
            Console.WriteLine($"{name,-40} | Час: {sw.ElapsedMilliseconds} мс");
        }

        // --- СИМУЛЯЦІЯ ЕТАПІВ ---
        static ImageFrame Decode(ImageFrame img) { Thread.Sleep(20); return img with { Status = "Decoded" }; }
        static ImageFrame ApplyFilter(ImageFrame img) { Thread.Sleep(50); return img with { Status = "Filtered" }; }
        static ImageFrame AddWatermark(ImageFrame img) { Thread.Sleep(15); return img with { Status = "Watermarked" }; }
        static ImageFrame Encode(ImageFrame img) { Thread.Sleep(30); return img with { Status = "Encoded" }; }

        // ==========================================
        // 1. ПОСЛІДОВНА ОБРОБКА
        // ==========================================
        static void RunSequential(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var img = new ImageFrame(i, $"img_{i}.jpg", 1024);
                img = Decode(img);
                img = ApplyFilter(img);
                img = AddWatermark(img);
                img = Encode(img);
            }
        }

        // ==========================================
        // 2. PRODUCER-CONSUMER
        // ==========================================
        static void RunProducerConsumer(int count, int consumerCount)
        {
            var queue = new BlockingCollection<ImageFrame>(20);

            // Продюсер (читає файли)
            var producer = Task.Run(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    queue.Add(new ImageFrame(i, $"img_{i}.jpg", 1024));
                }
                queue.CompleteAdding();
            });

            // Кожен бере картинку і проганяє її через фільтри
            var consumers = Enumerable.Range(0, consumerCount).Select(_ => Task.Run(() =>
            {
                foreach (var img in queue.GetConsumingEnumerable())
                {
                    var processed = Encode(AddWatermark(ApplyFilter(Decode(img))));
                }
            })).ToArray();

            Task.WaitAll(producer);
            Task.WaitAll(consumers);
        }

        // ==========================================
        // 3. PIPELINE 
        // ==========================================
        static void RunPipeline(int count)
        {
            // пайпи
            var decodeToFilter = new BlockingCollection<ImageFrame>(10);
            var filterToWatermark = new BlockingCollection<ImageFrame>(10);
            var watermarkToEncode = new BlockingCollection<ImageFrame>(10);
            
            var stage1 = Task.Run(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    decodeToFilter.Add(Decode(new ImageFrame(i, $"img_{i}.jpg", 1024)));
                }
                decodeToFilter.CompleteAdding();
            });

            // Етап 2: Фільтрація
            var stage2 = Task.Run(() =>
            {
                foreach (var img in decodeToFilter.GetConsumingEnumerable())
                    filterToWatermark.Add(ApplyFilter(img));
                filterToWatermark.CompleteAdding();
            });

            // Етап 3: Водяний знак
            var stage3 = Task.Run(() =>
            {
                foreach (var img in filterToWatermark.GetConsumingEnumerable())
                    watermarkToEncode.Add(AddWatermark(img));
                watermarkToEncode.CompleteAdding();
            });

            // Етап 4: Кодування
            var stage4 = Task.Run(() =>
            {
                foreach (var img in watermarkToEncode.GetConsumingEnumerable())
                    Encode(img);
            });

            Task.WaitAll(stage1, stage2, stage3, stage4);
        }
        
        // ==========================================
        // 3. PIPELINE optimized
        // ==========================================
        static void RunPipelineOptimized(int count)
        {
            var filterOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 8 };
            var defaultOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 2 };

            var decodeBlock = new TransformBlock<ImageFrame, ImageFrame>(img => Decode(img), defaultOptions);
            var filterBlock = new TransformBlock<ImageFrame, ImageFrame>(img => ApplyFilter(img), filterOptions);
            var watermarkBlock = new TransformBlock<ImageFrame, ImageFrame>(img => AddWatermark(img), defaultOptions);
            var encodeBlock = new ActionBlock<ImageFrame>(img => Encode(img), defaultOptions);

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            decodeBlock.LinkTo(filterBlock, linkOptions);
            filterBlock.LinkTo(watermarkBlock, linkOptions);
            watermarkBlock.LinkTo(encodeBlock, linkOptions);

            for (int i = 0; i < count; i++)
            {
                decodeBlock.Post(new ImageFrame(i, $"img_{i}.jpg", 1024));
            }

            decodeBlock.Complete();
            encodeBlock.Completion.GetAwaiter().GetResult();
        }
    }
}