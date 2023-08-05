using System;
using System.Diagnostics;
using System.Threading.Tasks;

public class Performance
{
    public static async Task<double> GetCpuUsageForProcess()
    {
        var startTime = DateTime.UtcNow;
        var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
        await Task.Delay(500);

        var endTime = DateTime.UtcNow;
        var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
        var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
        var totalMsPassed = (endTime - startTime).TotalMilliseconds;
        var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

        return Math.Round(cpuUsageTotal * 100f, 2);
    }

    public static double GetRamUsageForProcess()
    {
        var ramUsage = Process.GetCurrentProcess().WorkingSet64;

        // RAM of RPI in Bytes = 4294967296
        return Math.Round((ramUsage / 4294967296f) * 100f, 2);
    }

    public static string FormatPerformance(double cpu, double ram)
    {
        return $"CPU: {cpu}% | RAM: {ram}%";
    }
}