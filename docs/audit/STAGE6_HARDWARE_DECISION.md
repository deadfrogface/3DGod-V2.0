# Stage 6 — Hardware profiler

Decision: **KEEP** `HardwareProfiler` + nvidia-smi secondary probe. **REJECT** LibreHardwareMonitor. **DEFER** Hardware.Info.

Added fields: GpuVendor, CpuArchitecture, NvidiaDriverVersion.
CUDA remains true only when nvidia-smi succeeds or `3DGOD_CUDA=1` — never from GPU name alone.
Unknown stays unknown.
