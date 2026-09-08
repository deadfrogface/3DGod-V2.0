namespace ThreeDGodCreator.Core.Tests;

internal static class RepoPaths
{
    public static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "3DGodCreator.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Repository root with 3DGodCreator.sln was not found from " + AppContext.BaseDirectory);
    }

    public static string AssetsDir => Path.Combine(FindRepoRoot(), "assets");
}
