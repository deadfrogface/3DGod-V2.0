using System.Numerics;

namespace ThreeDGod.Rigging;

public readonly record struct BoneWeight(int BoneIndex, float Weight);

/// <summary>
/// Adapter for skin-weight backends (authored, distance heuristic, future AutoRig workers).
/// </summary>
public interface ISkinWeightSolver
{
    string BackendId { get; }
    IReadOnlyList<BoneWeight> Solve(Vector3 position, IReadOnlyList<Vector3> bonePositions, int maxInfluences = 4);
}

/// <summary>
/// Mesh2Motion-inspired inverse-distance soft weights (deterministic, no neural net).
/// </summary>
public sealed class DistanceSkinWeightSolver : ISkinWeightSolver
{
    public string BackendId => "distance-inv";

    public IReadOnlyList<BoneWeight> Solve(Vector3 position, IReadOnlyList<Vector3> bonePositions, int maxInfluences = 4)
    {
        if (bonePositions.Count == 0)
            return [new BoneWeight(0, 1f)];

        var scored = new List<(int Index, float Score)>(bonePositions.Count);
        for (var i = 0; i < bonePositions.Count; i++)
        {
            var d = Vector3.Distance(position, bonePositions[i]);
            scored.Add((i, 1f / MathF.Max(d * d, 1e-6f)));
        }
        scored.Sort((a, b) => b.Score.CompareTo(a.Score));
        var take = Math.Min(Math.Max(maxInfluences, 1), scored.Count);
        var sum = 0f;
        for (var i = 0; i < take; i++)
            sum += scored[i].Score;
        var result = new BoneWeight[take];
        for (var i = 0; i < take; i++)
            result[i] = new BoneWeight(scored[i].Index, scored[i].Score / sum);
        return result;
    }
}
