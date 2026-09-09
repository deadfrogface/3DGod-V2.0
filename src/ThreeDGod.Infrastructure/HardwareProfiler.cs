using System.Diagnostics;

namespace ThreeDGod.Infrastructure;

public sealed class HardwareProfile
{
    public int CpuCount { get; init; }
    public long RamBytes { get; init; }
    public string GpuName { get; init; } = "unknown";
    public int VramMb { get; init; }
    public bool Cuda { get; init; }
    public bool Vulkan { get; init; }
    public long DiskFreeBytes { get; init; }
}

public static class HardwareProfiler
{
    private static readonly Lazy<HardwareProfile> Cached = new(ProbeCore);

    public static HardwareProfile Probe() => Cached.Value;

    private static HardwareProfile ProbeCore()
    {
        var ram = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady);
        var (cuda, gpu, vram) = ProbeNvidia();
        return new HardwareProfile
        {
            CpuCount = Environment.ProcessorCount,
            RamBytes = ram,
            GpuName = Environment.GetEnvironmentVariable("3DGOD_GPU_NAME") ?? gpu,
            VramMb = int.TryParse(Environment.GetEnvironmentVariable("3DGOD_VRAM_MB"), out var envVram) ? envVram : vram,
            Cuda = string.Equals(Environment.GetEnvironmentVariable("3DGOD_CUDA"), "1", StringComparison.Ordinal) || cuda,
            Vulkan = string.Equals(Environment.GetEnvironmentVariable("3DGOD_VULKAN"), "1", StringComparison.Ordinal),
            DiskFreeBytes = drive?.AvailableFreeSpace ?? 0
        };
    }

    private static (bool Cuda, string GpuName, int VramMb) ProbeNvidia()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = "--query-gpu=name,memory.total --format=csv,noheader,nounits",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process is null)
                return (false, "unknown", 0);
            if (!process.WaitForExit(3000))
            {
                try { process.Kill(true); } catch (InvalidOperationException) { }
                return (false, "unknown", 0);
            }
            var line = process.StandardOutput.ReadLine();
            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(line))
                return (false, "unknown", 0);
            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            var name = parts.Length > 0 ? parts[0] : "nvidia";
            var vram = parts.Length > 1 && int.TryParse(parts[1], out var mb) ? mb : 0;
            return (true, name, vram);
        }
        catch (Exception)
        {
            return (false, "unknown", 0);
        }
    }
}

public interface IGpuJobScheduler
{
    Task<T> RunHeavyAsync<T>(Func<CancellationToken, Task<T>> job, CancellationToken cancellationToken = default);
}

public sealed class GpuJobScheduler : IGpuJobScheduler
{
    private readonly SemaphoreSlim _heavy = new(1, 1);

    public async Task<T> RunHeavyAsync<T>(Func<CancellationToken, Task<T>> job, CancellationToken cancellationToken = default)
    {
        await _heavy.WaitAsync(cancellationToken);
        try
        {
            return await job(cancellationToken);
        }
        finally
        {
            _heavy.Release();
        }
    }
}
