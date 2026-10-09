using System.Runtime.InteropServices;

namespace ThreeDGodCreator.App;

/// <summary>Wired DualShock 4 via Windows HID (VID 054C, PID 05C4/09CC).
/// No Bluetooth, DS4Windows, virtual XInput device or network access.</summary>
internal sealed class WiredDualShock4 : IDisposable
{
    private IntPtr _handle;
    private string? _path;
    public bool Connected => _handle != IntPtr.Zero && _handle != InvalidHandleValue;
    public Ds4State Read()
    {
        if (!OperatingSystem.IsWindows()) return default;
        if (!Connected && !Connect()) return default;
        var report = new byte[64];
        if (!HidD_GetInputReport(_handle, report, report.Length))
        {
            Disconnect();
            return default;
        }
        // USB DS4 input report 0x01: LX LY RX RY, buttons at offsets 5..7.
        if (report[0] != 0x01) return default;
        static float Axis(byte value) => MathF.Abs((value - 127.5f) / 127.5f) < 0.16f ? 0 : (value - 127.5f) / 127.5f;
        return new Ds4State(true, Axis(report[1]), Axis(report[2]), Axis(report[3]), Axis(report[4]),
            (report[5] & 0x0F) == 0, (report[6] & 0x20) != 0, (report[6] & 0x40) != 0);
    }
    private bool Connect()
    {
        var info = new HidDAttributes { Size = Marshal.SizeOf<HidDAttributes>() };
        HidD_GetHidGuid(out var guid);
        var set = SetupDiGetClassDevs(ref guid, null, IntPtr.Zero, 0x12);
        if (set == InvalidHandleValue) return false;
        try
        {
            for (uint i = 0; ; i++)
            {
                var data = new DeviceInterfaceData { Size = Marshal.SizeOf<DeviceInterfaceData>() };
                if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref guid, i, ref data)) break;
                SetupDiGetDeviceInterfaceDetail(set, ref data, IntPtr.Zero, 0, out var required, IntPtr.Zero);
                if (required == 0) continue;
                var detail = Marshal.AllocHGlobal((int)required);
                try
                {
                    Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                    if (!SetupDiGetDeviceInterfaceDetail(set, ref data, detail, required, out _, IntPtr.Zero)) continue;
                    var path = Marshal.PtrToStringUni(detail + 4);
                    if (string.IsNullOrEmpty(path) || !path.Contains("vid_054c", StringComparison.OrdinalIgnoreCase)
                        || !(path.Contains("pid_05c4", StringComparison.OrdinalIgnoreCase) || path.Contains("pid_09cc", StringComparison.OrdinalIgnoreCase)))
                        continue;
                    var handle = CreateFile(path, 0x80000000, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
                    if (handle == InvalidHandleValue) continue;
                    if (HidD_GetAttributes(handle, ref info) && info.VendorId == 0x054C
                        && (info.ProductId == 0x05C4 || info.ProductId == 0x09CC))
                    { _handle = handle; _path = path; return true; }
                    CloseHandle(handle);
                }
                finally { Marshal.FreeHGlobal(detail); }
            }
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
        return false;
    }
    private void Disconnect() { if (Connected) CloseHandle(_handle); _handle = IntPtr.Zero; _path = null; }
    public void Dispose() => Disconnect();
    private static readonly IntPtr InvalidHandleValue = new(-1);
    [StructLayout(LayoutKind.Sequential)] private struct HidDAttributes { public int Size; public ushort VendorId; public ushort ProductId; public ushort VersionNumber; }
    [StructLayout(LayoutKind.Sequential)] private struct DeviceInterfaceData { public int Size; public Guid InterfaceClassGuid; public int Flags; public IntPtr Reserved; }
    [DllImport("hid.dll")] private static extern void HidD_GetHidGuid(out Guid guid);
    [DllImport("hid.dll", SetLastError = true)] private static extern bool HidD_GetAttributes(IntPtr handle, ref HidDAttributes attrs);
    [DllImport("hid.dll", SetLastError = true)] private static extern bool HidD_GetInputReport(IntPtr handle, [Out] byte[] report, int length);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr SetupDiGetClassDevs(ref Guid guid, string? enumerator, IntPtr hwnd, int flags);
    [DllImport("setupapi.dll", SetLastError = true)] private static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr devInfo, ref Guid guid, uint index, ref DeviceInterfaceData data);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref DeviceInterfaceData data, IntPtr detail, uint size, out uint required, IntPtr devInfo);
    [DllImport("setupapi.dll")] private static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr CreateFile(string path, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
}
internal readonly record struct Ds4State(bool Connected, float LeftX, float LeftY, float RightX, float RightY, bool DpadUp, bool L1, bool R1);
