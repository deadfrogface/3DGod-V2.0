using ThreeDGod.Core.Domain;

namespace ThreeDGodCreator.Core.Tests;

public class DomainModelTests
{
    [Fact]
    public void ProjectDocument_JsonRoundtrip_PreservesIdentityAndCollections()
    {
        var original = new ProjectDocument
        {
            Name = "Demo",
            Description = "Roundtrip",
            FormatVersion = 1,
            AppVersionCreated = "2.0.0",
            AppVersionLastSaved = "2.0.0",
            ActiveSceneId = Guid.NewGuid(),
            SceneIds = [Guid.NewGuid()],
            CharacterIds = [Guid.NewGuid()],
            AssetIds = [Guid.NewGuid()],
            ExportProfiles = [new ExportProfile { Name = "glb", Target = "glb" }],
            Settings = new ProjectSettings { Locale = "de", Theme = "dark", Values = { ["aa"] = "bb" } },
            ProvenanceSummary = "none"
        };

        var copy = DomainJson.Roundtrip(original);
        Assert.Equal(original.ProjectId, copy.ProjectId);
        Assert.Equal(original.Name, copy.Name);
        Assert.Equal(original.CharacterIds, copy.CharacterIds);
        Assert.Equal("glb", copy.ExportProfiles[0].Name);
        Assert.Equal("bb", copy.Settings.Values["aa"]);
        Assert.Equal(original.SchemaVersion, copy.SchemaVersion);
    }

    [Fact]
    public void CharacterDocument_JsonRoundtrip_IncludesOptionalStates()
    {
        var original = new CharacterDocument
        {
            Name = "Hero",
            CharacterKind = CharacterKind.HumanoidCreature,
            SourceRepresentation = SourceRepresentation.ModularCreature,
            RootTransform = new SpatialTransform { Ty = 1.2 },
            RigId = Guid.NewGuid(),
            MorphState = new MorphState { Weights = { ["jaw"] = 0.4f } },
            ParametricHumanState = new ParametricHumanState
            {
                BackendId = "anny",
                PhenotypeParameters = { ["height"] = 1.8f }
            },
            CreatureState = new CreatureState
            {
                BaseFamily = "orc",
                BodyPlan = new BodyPlan { TailCount = 1, ExtraLimbCount = 0, IsBiped = true }
            },
            GeneratedAssetMetadata = new GeneratedAssetMetadata { BackendId = "test", Prompt = "orc" },
            Tags = ["demo"]
        };

        var copy = DomainJson.Roundtrip(original);
        Assert.Equal(original.CharacterId, copy.CharacterId);
        Assert.Equal(CharacterKind.HumanoidCreature, copy.CharacterKind);
        Assert.Equal(1.2, copy.RootTransform.Ty);
        Assert.Equal(0.4f, copy.MorphState.Weights["jaw"]);
        Assert.Equal("anny", copy.ParametricHumanState!.BackendId);
        Assert.Equal(1, copy.CreatureState!.BodyPlan.TailCount);
        Assert.Equal("test", copy.GeneratedAssetMetadata!.BackendId);
    }

    [Theory]
    [InlineData(typeof(MeshAsset))]
    [InlineData(typeof(MaterialDefinition))]
    [InlineData(typeof(RigDefinition))]
    [InlineData(typeof(GarmentDefinition))]
    [InlineData(typeof(GarmentInstance))]
    [InlineData(typeof(AttachmentInstance))]
    [InlineData(typeof(GeneratedAssetMetadata))]
    [InlineData(typeof(CreatureState))]
    [InlineData(typeof(ParametricHumanState))]
    public void DomainTypes_JsonRoundtrip_DoesNotThrow(Type type)
    {
        var value = Activator.CreateInstance(type)!;
        var json = DomainJson.Serialize(value);
        var copy = DomainJsonTypeHelper.Deserialize(json, type);
        Assert.NotNull(copy);
        Assert.Contains("schemaVersion", json, StringComparison.Ordinal);
    }

    [Fact]
    public void NewGuids_AreUnique()
    {
        var ids = new HashSet<Guid>();
        for (var i = 0; i < 200; i++)
        {
            Assert.True(ids.Add(new ProjectDocument().ProjectId));
            Assert.True(ids.Add(new CharacterDocument().CharacterId));
            Assert.True(ids.Add(new MeshAsset().MeshAssetId));
            Assert.True(ids.Add(new MaterialDefinition().MaterialId));
            Assert.True(ids.Add(new RigDefinition().RigId));
            Assert.True(ids.Add(new GarmentDefinition().GarmentDefinitionId));
            Assert.True(ids.Add(new GarmentInstance().GarmentInstanceId));
            Assert.True(ids.Add(new AttachmentInstance().AttachmentId));
            Assert.True(ids.Add(new BodyPartSlot().SlotId));
        }
    }

    [Fact]
    public void ThreeDGodCore_HasNoWpfOrHelixTypes()
    {
        var coreDir = Path.Combine(RepoPaths.FindRepoRoot(), "src", "ThreeDGod.Core");
        var files = Directory.GetFiles(coreDir, "*.cs", SearchOption.AllDirectories);
        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("System.Windows", text, StringComparison.Ordinal);
            Assert.DoesNotContain("HelixToolkit", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Media3D", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void BodyPlan_DoesNotAssumeTwoArmsOrLegs()
    {
        var plan = new BodyPlan
        {
            SemanticLimbDescriptors = ["leftArm", "rightArm", "extraArm"],
            ExtraLimbCount = 1,
            TailCount = 2,
            IsBiped = false,
            IsQuadruped = false
        };
        var copy = DomainJson.Roundtrip(new CreatureState { BodyPlan = plan });
        Assert.Equal(3, copy.BodyPlan.SemanticLimbDescriptors.Count);
        Assert.Equal(1, copy.BodyPlan.ExtraLimbCount);
        Assert.False(copy.BodyPlan.IsBiped);
    }
}

file static class DomainJsonTypeHelper
{
    public static object Deserialize(string json, Type type)
    {
        var method = typeof(DomainJson).GetMethod(nameof(DomainJson.Deserialize))!;
        return method.MakeGenericMethod(type).Invoke(null, [json])!;
    }
}
