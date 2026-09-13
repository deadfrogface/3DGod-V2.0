using System.Diagnostics;

namespace ThreeDGod.Infrastructure;

public sealed class HardwareProfile
{
    public int CpuCount { get; init; }
    public long RamBytes { get; init; }
    public string GpuName { get; init; } = "unknown";
    public string GpuVendor { get; init; } = "unknown";
    public string CpuArchitecture { get; init; } = "unknown";
    public string? NvidiaDriverVersion { get; init; }
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
        var (cuda, gpu, vram, driver) = ProbeNvidia();
        var gpuName = Environment.GetEnvironmentVariable("3DGOD_GPU_NAME") ?? gpu;
        return new HardwareProfile
        {
            CpuCount = Environment.ProcessorCount,
            RamBytes = ram,
            GpuName = gpuName,
            GpuVendor = InferGpuVendor(gpuName, cuda),
            CpuArchitecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
            NvidiaDriverVersion = driver,
            VramMb = int.TryParse(Environment.GetEnvironmentVariable("3DGOD_VRAM_MB"), out var envVram) ? envVram : vram,
            // CUDA evidence = nvidia-smi success or explicit override. Never infer from GPU name alone.
            Cuda = string.Equals(Environment.GetEnvironmentVariable("3DGOD_CUDA"), "1", StringComparison.Ordinal) || cuda,
            Vulkan = string.Equals(Environment.GetEnvironmentVariable("3DGOD_VULKAN"), "1", StringComparison.Ordinal),
            DiskFreeBytes = drive?.AvailableFreeSpace ?? 0
        };
    }

    private static (bool Cuda, string GpuName, int VramMb, string? DriverVersion) ProbeNvidia()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = "--query-gpu=name,memory.total,driver_version --format=csv,noheader,nounits",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process is null)
                return (false, "unknown", 0, null);
            if (!process.WaitForExit(3000))
            {
                try { process.Kill(true); } catch (InvalidOperationException) { }
                return (false, "unknown", 0, null);
            }
            var line = process.StandardOutput.ReadLine();
            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(line))
                return (false, "unknown", 0, null);
            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            var name = parts.Length > 0 ? parts[0] : "nvidia";
            var vram = parts.Length > 1 && int.TryParse(parts[1], out var mb) ? mb : 0;
            var driver = parts.Length > 2 ? parts[2] : null;
            // nvidia-smi responding is CUDA-toolkit-adjacent evidence; still not a CUDA runtime guarantee.
            return (true, name, vram, driver);
        }
        catch (Exception)
        {
            return (false, "unknown", 0, null);
        }
    }

    private static string InferGpuVendor(string gpuName, bool nvidiaSmiOk)
    {
        if (nvidiaSmiOk)
            return "NVIDIA";
        if (string.IsNullOrWhiteSpace(gpuName) || gpuName == "unknown")
            return "unknown";
        if (gpuName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)
            || gpuName.Contains("GeForce", StringComparison.OrdinalIgnoreCase)
            || gpuName.Contains("Quadro", StringComparison.OrdinalIgnoreCase)
            || gpuName.Contains("RTX", StringComparison.OrdinalIgnoreCase)
            || gpuName.Contains("GTX", StringComparison.OrdinalIgnoreCase))
            return "NVIDIA";
        if (gpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase)
            || gpuName.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
            return "AMD";
        if (gpuName.Contains("Intel", StringComparison.OrdinalIgnoreCase)
            || gpuName.Contains("Arc", StringComparison.OrdinalIgnoreCase))
            return "Intel";
        return "unknown";
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
