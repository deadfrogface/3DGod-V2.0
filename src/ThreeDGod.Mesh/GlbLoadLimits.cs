namespace ThreeDGod.Mesh;

public sealed class GlbLoadLimits
{
    public static GlbLoadLimits Default { get; } = new();

    public long MaxFileBytes { get; init; } = 512L * 1024 * 1024;
}

public sealed class GlbLoadException : Exception
{
    public string Code { get; }

    public GlbLoadException(string code, string message) : base(message) => Code = code;
}
