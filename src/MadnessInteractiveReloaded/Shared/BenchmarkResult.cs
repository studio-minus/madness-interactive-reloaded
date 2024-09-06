using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MIR;

/// <summary>
/// The result of the performance benchmark.
/// </summary>
public class BenchmarkResult
{
    /// <summary>
    /// The minimum update time.
    /// </summary>
    public float MinUpdateTime => UpdateTimeListMs.Min();

    /// <summary>
    /// The maximum update time.
    /// </summary>
    public float MaxUpdateTime => UpdateTimeListMs.Max();

    /// <summary>
    /// The mean time.
    /// </summary>
    public float MeanUpdateTime => UpdateTimeListMs.Average();

    public List<float> UpdateTimeListMs = new();

    /// <summary>
    /// Write the benchmark results to a file.
    /// </summary>
    public void WriteToFile()
    {
        var path = "benchmark-" + DateTime.Now.Ticks + ".md";
        var dataPointsPath = "benchmark-" + DateTime.Now.Ticks + ".txt";
        using var stream = new StreamWriter(path);

        stream.WriteLine("# Madness Interactive Reloaded - Benchmark at {0}", DateTime.Now.ToString());
#if DEBUG
        stream.WriteLine();
        stream.WriteLine("## GAME IS IN DEBUG MODE. RESULTS ARE NOT RELIABLE.");
#endif

        stream.WriteLine("## Update times");
        stream.WriteLine("```\nMin\t\t{0}ms / {1}fps\n", MinUpdateTime * 1000, 1 / MinUpdateTime);
        stream.WriteLine("Max\t\t{0}ms / {1}fps\n", MaxUpdateTime * 1000, 1 / MaxUpdateTime);
        stream.WriteLine("Mean\t{0}ms / {1}fps\n```", MeanUpdateTime * 1000, 1 / MeanUpdateTime);
        stream.WriteLine();

        stream.WriteLine("Data points file: {0}", dataPointsPath);

        stream.Dispose();

        using var dataPointsFile = new StreamWriter(dataPointsPath);
        foreach (var item in UpdateTimeListMs)
            dataPointsFile.WriteLine(item);

        dataPointsFile.Dispose();

        File.Copy(path, "latest-benchmark.md", true);
    }
}