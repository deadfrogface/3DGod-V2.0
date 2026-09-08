using System.Collections.Concurrent;

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
    public static HardwareProfile Probe()
    {
        var ram = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady);
        return new HardwareProfile
        {
            CpuCount = Environment.ProcessorCount,
            RamBytes = ram,
            GpuName = Environment.GetEnvironmentVariable("3DGOD_GPU_NAME") ?? "unknown",
            VramMb = int.TryParse(Environment.GetEnvironmentVariable("3DGOD_VRAM_MB"), out var vram) ? vram : 0,
            Cuda = string.Equals(Environment.GetEnvironmentVariable("3DGOD_CUDA"), "1", StringComparison.Ordinal),
            Vulkan = string.Equals(Environment.GetEnvironmentVariable("3DGOD_VULKAN"), "1", StringComparison.Ordinal),
            DiskFreeBytes = drive?.AvailableFreeSpace ?? 0
        };
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
