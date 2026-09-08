using System.Xml.Linq;

namespace ThreeDGodCreator.Core.Tests;

public class LayerBoundaryTests
{
    [Fact]
    public void ExistingCoreProject_DoesNotReferenceWpfOrHelixOrBlenderServiceFile()
    {
        var csproj = Path.Combine(RepoPaths.FindRepoRoot(), "3DGodCreator.Core", "3DGodCreator.Core.csproj");
        var xml = File.ReadAllText(csproj);
        Assert.DoesNotContain("HelixToolkit", xml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UseWPF", xml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PresentationCore", xml, StringComparison.OrdinalIgnoreCase);

        var blenderFile = Path.Combine(RepoPaths.FindRepoRoot(), "3DGodCreator.Core", "Services", "BlenderService.cs");
        Assert.False(File.Exists(blenderFile), "BlenderService must not live in Core.");
    }

    [Fact]
    public void ExistingCoreProject_HasNoWpfPackageReferences()
    {
        var csproj = Path.Combine(RepoPaths.FindRepoRoot(), "3DGodCreator.Core", "3DGodCreator.Core.csproj");
        var doc = XDocument.Load(csproj);
        var packages = doc.Descendants("PackageReference")
            .Select(x => (string?)x.Attribute("Include"))
            .Where(x => x != null)
            .ToList();
        Assert.DoesNotContain(packages, p => p!.Contains("Helix", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(packages, p => p!.Contains("Wpf", StringComparison.OrdinalIgnoreCase));
    }
}
