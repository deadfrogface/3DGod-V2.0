using ThreeDGod.AI;
using ThreeDGod.Core.Domain;
using ThreeDGod.Core.Editing;
using ThreeDGod.Infrastructure;
using ThreeDGod.Mesh;

namespace ThreeDGodCreator.Core.Tests;

public class GarmentTemplateTests
{
    [Fact]
    public void BuiltIns_IncludeCoreTemplates()
    {
        var names = GarmentTemplates.BuiltIns.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("T-Shirt", names);
        Assert.Contains("Jacket", names);
        Assert.Contains("Pants", names);
        Assert.Contains("Coat", names);
        Assert.All(GarmentTemplates.BuiltIns, t =>
        {
            Assert.Contains(t.ParameterCatalog, p => p.Key == "length");
            Assert.Contains(t.ParameterCatalog, p => p.Key == "sleeveLength");
        });
    }

    [Fact]
    public async Task LaengereAermel_ChangesSleeveGeometry_NotAiRegen()
    {
        var plan = DeterministicAiParser.Parse("längere Ärmel");
        Assert.Equal("valid", plan.Status);
        Assert.Equal("garment.parameter.delta", plan.Operation);
        Assert.Equal("sleeveLength", plan.Args["key"]);

        var svc = new GarmentService();
        var def = svc.GetTemplate("Jacket");
        var instance = svc.Instantiate(def, Guid.NewGuid());
        var shortPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + "-short.glb");
        var longPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + "-long.glb");
        try
        {
            svc.BuildMesh(def, instance, shortPath);
            var before = instance.ParameterValues["sleeveLength"];
            var stack = new CommandStack();
            await svc.ApplyTextAsync(def, instance, "längere Ärmel", stack);
            Assert.True(instance.ParameterValues["sleeveLength"] > before);
            svc.BuildMesh(def, instance, longPath);
            var shortPos = MeshCompare.ReadPositions(shortPath);
            var longPos = MeshCompare.ReadPositions(longPath);
            var shortSpan = shortPos.Max(p => MathF.Abs(p.X));
            var longSpan = longPos.Max(p => MathF.Abs(p.X));
            Assert.True(longSpan > shortSpan + 0.05f, $"Sleeve span did not grow: {shortSpan} -> {longSpan}");
            await stack.UndoAsync();
            Assert.Equal(before, instance.ParameterValues["sleeveLength"]);
        }
        finally
        {
            if (File.Exists(shortPath)) File.Delete(shortPath);
            if (File.Exists(longPath)) File.Delete(longPath);
        }
    }
}
