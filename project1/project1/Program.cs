using System.Diagnostics;

namespace BrownianMotionSimulation
{
    class Program
    {
        const int Width = 20;
        const int Height = 20;
        const int ParticleCount = 50000;
        const int SimulationSteps = 50; // testing purposes
        const int SnapshotIntervalMilliseconds = 100;
        static TimeSpan SimulationDuration = TimeSpan.FromSeconds(10);

        static int[,] grid;
        static List<int[,]> snapshots = new List<int[,]>();
        
        // locks objects
        static object[,] cellLocks;

        static ThreadLocal<Random> rng = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));

        struct Particle
        {
            public int X;
            public int Y;
        }

        static Particle[] particles; 

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            //Console.WriteLine("[ЗАПУСК 1] Небезпечна симуляція (Race Conditions)...");
            InitializeSimulation();
            RunSimulation(safeMode: false);
            
            Console.WriteLine("\n[ЗАПУСК 2] Безпечна симуляція...");
            InitializeSimulation();
            PrintCrystalSnapshot();
            
            var step = 1;
            var timerElapsedMs = SnapshotIntervalMilliseconds;
            void Callback(object? _)
            {
                Console.Write($"\n[Крок {step}] Знімок зроблено {timerElapsedMs}ms. Поточна сума в матриці: {GetTotalParticles()}");
                // PrintCrystalSnapshot();
                step++;
                timerElapsedMs += SnapshotIntervalMilliseconds;
            }
            
            await using var timer = new Timer(Callback, null, SnapshotIntervalMilliseconds, SnapshotIntervalMilliseconds);
            RunSimulation(safeMode: true, SimulationDuration, SnapshotIntervalMilliseconds, timer);
        }
        
        static void InitializeSimulation()
        {
            grid = new int[Width, Height];
            cellLocks = new object[Width, Height];
            particles = new Particle[ParticleCount];

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    cellLocks[x, y] = new object();

            // Розподіл частинок (всі стартують в центрі)
            for (int i = 0; i < ParticleCount; i++)
            {
                particles[i] = new Particle { X = Width / 2, Y = Height / 2 };
                grid[Width / 2, Height / 2]++;
            }
        }

        // Fixed attempts (50 SimulationSteps), testing purposes
        static void RunSimulation(bool safeMode)
        {
            int initialCount = GetTotalParticles();
            Console.WriteLine($"Початкова кількість частинок: {initialCount}");
        
            Stopwatch sw = Stopwatch.StartNew();
            for (int step = 1; step <= SimulationSteps; step++)
            {
                // Barrier + thread pool
                Parallel.For(0, ParticleCount, i =>
                {
                    MoveParticle(ref particles[i].X, ref particles[i].Y, safeMode);
                });
                
                // if (step % 10 == 0)
                // {
                //     Console.WriteLine($"[Крок {step}] Знімок зроблено. Поточна сума в матриці: {GetTotalParticles()}");
                // }
                
                // Console.Write($"\n[Крок {step}] Знімок зроблено. Поточна сума в матриці: {GetTotalParticles()}");
                
                //PrintCrystalSnapshot();
            }
        
            sw.Stop();
            int finalCount = GetTotalParticles();
            Console.WriteLine($"Кінцева кількість частинок у матриці: {finalCount}");
        
            if (initialCount != finalCount)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(
                    $"[ERROR] Втрачено частинок: {initialCount - finalCount}");
                Console.ResetColor();
            }
        
            Console.WriteLine($"Час виконання: {sw.ElapsedMilliseconds} мс");
            PrintCrystalSnapshot();
        }

        static void RunSimulation(bool safeMode, TimeSpan simulationDuration, int snapshotIntervalMilliseconds, Timer timer)
        {
            int initialCount = GetTotalParticles();
            Console.WriteLine($"Початкова кількість частинок: {initialCount}");
            
            var sw = Stopwatch.StartNew();
            long nextSnapshotTime = snapshotIntervalMilliseconds;
            while (sw.Elapsed < simulationDuration)
            {
                // Barrier + thread pool
                Parallel.For(0, ParticleCount, i =>
                {
                    MoveParticle(ref particles[i].X, ref particles[i].Y, safeMode);
                });

                if (sw.ElapsedMilliseconds >= nextSnapshotTime)
                {
                    snapshots.Add((int[,])grid.Clone()); 
                    nextSnapshotTime += snapshotIntervalMilliseconds; 
                }
                
                // ensure to add snapshot at the end of simulation
                if (sw.Elapsed > simulationDuration)
                    snapshots.Add((int[,])grid.Clone()); 
            }

            sw.Stop();
            timer.Change(Timeout.Infinite, Timeout.Infinite); // disable timer
            
            int finalCount = GetTotalParticles();
            Console.WriteLine($"\nКінцева кількість частинок у матриці: {finalCount}");
        
            Console.WriteLine($"Час виконання: {sw.ElapsedMilliseconds} мс");
            PrintCrystalSnapshot();
        }
        
        static void MoveParticle(ref int x, ref int y, bool safeMode)
        {
            int oldX = x;
            int oldY = y;
            int newX = oldX;
            int newY = oldY;
        
            // Вибір напрямку
            int dir = rng.Value.Next(4);
            switch (dir)
            {
                case 0: newY--; break; // Вгору
                case 1: newY++; break; // Вниз
                case 2: newX--; break; // Вліво
                case 3: newX++; break; // Вправо
            }
        
            // Перевірка меж (відбивання від стін)
            if (newX < 0 || newX >= Width || newY < 0 || newY >= Height)
                return; // Залишається на місці
        
            if (safeMode)
            {
                int id1 = oldY * Width + oldX;
                int id2 = newY * Width + newX;
            
                object lock1 = id1 < id2 ? cellLocks[oldX, oldY] : cellLocks[newX, newY];
                object lock2 = id1 < id2 ? cellLocks[newX, newY] : cellLocks[oldX, oldY];
            
                lock (lock1)
                {
                    lock (lock2)
                    {
                        grid[oldX, oldY]--;
                        grid[newX, newY]++;
                        x = newX;
                        y = newY;
                    }
                }
            }
            else
            {
                grid[oldX, oldY]--;
                grid[newX, newY]++;
                x = newX;
                y = newY;
            }
        }

        static int GetTotalParticles()
        {
            int sum = 0;
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    sum += grid[x, y];
            return sum;
        }

        static void PrintCrystalSnapshot()
        {
            Console.WriteLine("\nВізуалізація зліпку (фрагмент 20x20):");
            for (int y = 0; y < Math.Min(Height, 20); y++)
            {
                for (int x = 0; x < Math.Min(Width, 20); x++)
                {
                    Console.Write($"{grid[x, y],6}");
                }
                Console.WriteLine();
            }
        }
    }
}