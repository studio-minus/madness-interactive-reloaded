using System;

namespace BigGustave.Timing
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    public static class Program
    {
        public static int Main(string[] args)
        {
#if DEBUG
            throw new InvalidOperationException("Cannot run timings in debug mode.");
#endif
            Console.WriteLine("Starting run.");

            var stopwatch = new Stopwatch();

            var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-file.png");

            var bytes = File.ReadAllBytes(file);

            var result = 520;

            Console.WriteLine("Warmup.");
            using (var memoryStream = new MemoryStream(bytes))
            {
                // Warmup
                for (var i = 0; i < 5; i++)
                {
                    GetAllPixels(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    GetPrimeFactors();
                    Console.WriteLine($"Warmup run {i + 1} finished.");
                }

                var pngTimings = new List<long>();
                var referenceTimings = new List<long>();

                for (var i = 0; i < 21; i++)
                {
                    Console.WriteLine($"Starting run {i + 1}.");
                    stopwatch.Start();
                    GetAllPixels(memoryStream);
                    stopwatch.Stop();
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    pngTimings.Add(stopwatch.ElapsedTicks);

                    stopwatch.Reset();
                    stopwatch.Start();
                    GetPrimeFactors();
                    stopwatch.Stop();
                    referenceTimings.Add(stopwatch.ElapsedTicks);
                    Console.WriteLine($"Finished run {i + 1}.");
                }

                var pngAverage = pngTimings.Average();
                var referenceAverage = referenceTimings.Average();
                var average = Math.Round(pngAverage / referenceAverage, 2);

                Console.WriteLine($"PNG average: {pngAverage} ticks");
                Console.WriteLine($"Ref average: {referenceAverage} ticks");
                Console.WriteLine($"Multiple: {average}");

                result = (int)Math.Round(average * 100);
            }

            if (args.Length>0)
            {
                Console.WriteLine("Complete, press any key to exit.");
                Console.ReadKey();
            }

            return result;
        }

        private static List<long> GetPrimeFactors()
        {
            // intentionally slow and wrong
            const long value = 600851475143;

            var result = new List<long>();

            var primes = new List<long> { 2 };

            for (long i = 3; i <= Math.Sqrt(value); i+=2)
            {
                if (value % i == 0)
                {
                    result.Add(i);
                }

                if (primes.All(x => value % x != 0))
                {
                    primes.Add(i);
                }
            }

            return result;
        }

        private static int GetAllPixels(Stream file)
        {
            var val = 0;
            var png = Png.Open(file);

            for (int i = 0; i < png.Width; i++)
            {
                for (int j = 0; j < png.Height; j++)
                {
                    var pixel = png.GetPixel(i, j);
                    val += pixel.R;
                }
            }

            return val;
        }
    }
}
