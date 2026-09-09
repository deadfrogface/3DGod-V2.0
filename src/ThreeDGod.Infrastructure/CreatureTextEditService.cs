using System.Numerics;
using ThreeDGod.AI;
using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Core.Editing;
using ThreeDGod.Mesh;

namespace ThreeDGod.Infrastructure;

public static class CreaturePartCatalog
{
    public static IReadOnlyList<BodyPartSlot> Create(ProjectBundle bundle, string meshRoot, string slot, string family)
    {
        Directory.CreateDirectory(meshRoot);
        if (slot == "head" && family == "rat")
        {
            var muzzle = CreatureAssembly.WritePart(bundle, meshRoot, "rat-muzzle", SemanticBodyPartType.Head, "head", "boundary:head.face.muzzle",
                b => b.AddCone(new Vector3(0, 1.62f, 0.08f), new Vector3(0, 1.58f, 0.28f), 0.045f, 14));
            var earL = CreatureAssembly.WritePart(bundle, meshRoot, "rat-ear.L", SemanticBodyPartType.Ear, "head", "boundary:head.ear.L",
                b => b.AddSphere(new Vector3(-0.14f, 1.78f, 0), 0.055f, 10, 8));
            var earR = CreatureAssembly.WritePart(bundle, meshRoot, "rat-ear.R", SemanticBodyPartType.Ear, "head", "boundary:head.ear.R",
                b => b.AddSphere(new Vector3(0.14f, 1.78f, 0), 0.055f, 10, 8));
            return [muzzle.Slot, earL.Slot, earR.Slot];
        }
        if (slot == "horn")
        {
            var hornL = CreatureAssembly.WritePart(bundle, meshRoot, "horn.L", SemanticBodyPartType.Horn, "head", "boundary:head.temple.L",
                b => b.AddCone(new Vector3(-0.08f, 1.72f, 0.02f), new Vector3(-0.16f, 1.95f, -0.02f), 0.03f, 12));
            var hornR = CreatureAssembly.WritePart(bundle, meshRoot, "horn.R", SemanticBodyPartType.Horn, "head", "boundary:head.temple.R",
                b => b.AddCone(new Vector3(0.08f, 1.72f, 0.02f), new Vector3(0.16f, 1.95f, -0.02f), 0.03f, 12));
            return [hornL.Slot, hornR.Slot];
        }
        if (slot == "rightHand" && family is "prosthetic" or "mechanical")
        {
            var hand = CreatureAssembly.WritePart(bundle, meshRoot, "hand.R.mechanical", SemanticBodyPartType.RightHand, "hand.R", "boundary:wrist.R.prosthetic",
                b =>
                {
                    b.AddSphere(new Vector3(0.7f, 1.4f, 0), 0.04f, 8, 6);
                    b.AddCone(new Vector3(0.7f, 1.4f, 0), new Vector3(0.92f, 1.32f, 0.04f), 0.03f, 8);
                });
            return [hand.Slot];
        }
        throw new InvalidOperationException($"NotInstalled – no catalog part for slot={slot} family={family}.");
    }
}

public sealed class CreatureTextEditService : ICreatureTextEditService
{
    public async Task ApplyAsync(
        string prompt,
        ProjectBundle bundle,
        CharacterDocument character,
        string meshRoot,
        CommandStack stack,
        CancellationToken cancellationToken = default)
    {
        var plan = DeterministicAiParser.Parse(prompt);
        if (plan.Status != "valid")
            throw new InvalidOperationException(plan.Reason ?? "Unsupported – prompt is not a creature edit.");
        var op = plan.Operation;
        if (op is not ("creature.replacePart" or "creature.addPart" or "creature.swapPart"))
            throw new InvalidOperationException("Unsupported – not a ReplaceBodyPart/AddCreaturePart plan.");

        var state = character.CreatureState ?? throw new InvalidOperationException("Character has no CreatureState.");
        var slot = plan.Args.GetValueOrDefault("slot", "");
        var family = plan.Args.GetValueOrDefault("family", "catalog");
        var created = CreaturePartCatalog.Create(bundle, meshRoot, slot, family);
        var removeTypes = RemoveTypesFor(slot, op);
        var kept = state.ExtraBodyParts.Where(p => !removeTypes.Contains(p.SemanticType)).ToList();
        if (op == "creature.addPart")
            kept.AddRange(created.Where(p => state.ExtraBodyParts.All(e => e.SemanticType != p.SemanticType || e.ParentBoneSemantic != p.ParentBoneSemantic)));
        else
            kept.AddRange(created);

        var newMeshIds = character.MeshSet.MeshAssetIds
            .Concat(created.Select(p => p.MeshAssetId!.Value))
            .Distinct()
            .ToList();

        stack.BeginTransaction();
        try
        {
            await stack.ExecuteAsync(
                new SnapshotListCommand<BodyPartSlot>(character.CharacterId, state.ExtraBodyParts, kept, "creature.parts"),
                cancellationToken);
            await stack.ExecuteAsync(
                new SnapshotListCommand<Guid>(character.CharacterId, character.MeshSet.MeshAssetIds, newMeshIds, "creature.meshes"),
                cancellationToken);
            await stack.CommitTransactionAsync("creature.text-edit", cancellationToken);
        }
        catch
        {
            await stack.AbortTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static HashSet<SemanticBodyPartType> RemoveTypesFor(string slot, string? op)
    {
        if (op == "creature.addPart")
            return [];
        return slot switch
        {
            "head" => [SemanticBodyPartType.Head, SemanticBodyPartType.Ear],
            "rightHand" => [SemanticBodyPartType.RightHand],
            "horn" => [SemanticBodyPartType.Horn],
            _ => []
        };
    }
}
