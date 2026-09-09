using ThreeDGod.AI;
using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Core.Editing;
using ThreeDGod.Mesh;

namespace ThreeDGod.Infrastructure;

public static class GarmentTemplates
{
    public static IReadOnlyList<GarmentDefinition> BuiltIns { get; } =
    [
        Template("T-Shirt", GarmentType.Shirt, sleeve: 0.35f, length: 0.45f),
        Template("Jacket", GarmentType.Jacket, sleeve: 0.5f, length: 0.55f),
        Template("Pants", GarmentType.Pants, sleeve: 0f, length: 0.7f),
        Template("Coat", GarmentType.Coat, sleeve: 0.55f, length: 0.85f),
        Template("Skirt", GarmentType.Skirt, sleeve: 0f, length: 0.4f),
        Template("Dress", GarmentType.Dress, sleeve: 0.3f, length: 0.75f)
    ];

    private static GarmentDefinition Template(string name, GarmentType type, float sleeve, float length) =>
        new()
        {
            Name = name,
            GarmentType = type,
            GeneratorBackend = "parametric-catalog",
            ParameterCatalog =
            [
                new() { Key = "length", DisplayNameResourceKey = "garment.length", Min = 0.2f, Max = 1.2f, Default = length },
                new() { Key = "sleeveLength", DisplayNameResourceKey = "garment.sleeveLength", Min = 0f, Max = 1.2f, Default = sleeve },
                new() { Key = "width", DisplayNameResourceKey = "garment.width", Min = 0.1f, Max = 0.6f, Default = 0.28f },
                new() { Key = "collar", DisplayNameResourceKey = "garment.collar", Min = 0f, Max = 0.2f, Default = 0.08f }
            ]
        };
}

public sealed class GarmentService : IGarmentService
{
    public GarmentDefinition GetTemplate(string name) =>
        GarmentTemplates.BuiltIns.First(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public GarmentInstance Instantiate(GarmentDefinition definition, Guid characterId)
    {
        var instance = new GarmentInstance
        {
            DefinitionId = definition.GarmentDefinitionId,
            CharacterId = characterId,
            FitState = "unfitted"
        };
        foreach (var entry in definition.ParameterCatalog)
            instance.ParameterValues[entry.Key] = entry.Default;
        return instance;
    }

    public string BuildMesh(GarmentDefinition definition, GarmentInstance instance, string destinationGlb)
    {
        var (positions, indices) = GarmentMeshBuilder.Build(definition.GarmentType, instance.ParameterValues);
        TriangleMeshExport.WriteGlb(destinationGlb, positions, indices);
        return destinationGlb;
    }

    public async Task ApplyTextAsync(GarmentDefinition definition, GarmentInstance instance, string prompt, CommandStack stack, CancellationToken cancellationToken = default)
    {
        var plan = DeterministicAiParser.Parse(prompt);
        if (plan.Status != "valid" || plan.Operation != "garment.parameter.delta")
            throw new InvalidOperationException("Unsupported – prompt is not a garment parameter edit.");
        var key = plan.Args.GetValueOrDefault("key", "sleeveLength");
        if (!instance.ParameterValues.ContainsKey(key))
            throw new InvalidOperationException($"Unsupported – garment has no parameter '{key}'.");
        var old = instance.ParameterValues[key];
        var delta = float.Parse(plan.Args.GetValueOrDefault("delta", "0.2"), System.Globalization.CultureInfo.InvariantCulture);
        var next = Math.Clamp(old + delta, 0f, 1.5f);
        await stack.ExecuteAsync(new PropertyChangeCommand(instance.GarmentInstanceId, "garment:" + key, old, next, value =>
        {
            instance.ParameterValues[key] = Convert.ToSingle(value);
        }, "garment.parameter"), cancellationToken);
    }
}
