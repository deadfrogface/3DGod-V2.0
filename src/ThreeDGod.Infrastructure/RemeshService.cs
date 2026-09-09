using ThreeDGod.Application;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Core.Domain;
using ThreeDGod.Mesh;

namespace ThreeDGod.Infrastructure;

public sealed class RemeshService : IRemeshService
{
    private readonly IDiagnosticService? _diagnostics;

    public RemeshService(IDiagnosticService? diagnostics = null) => _diagnostics = diagnostics;

    public string RemeshGlb(string sourceGlb, string destinationGlb, RemeshProfile profile)
    {
        return PipelineTrace.Run(_diagnostics, "Mesh", "Mesh.Cleanup", () =>
        {
            var (positions, indices) = MeshCompare.ReadMesh(sourceGlb);
            PipelineTrace.Run(_diagnostics, "Mesh", "Mesh.Validate", () =>
            {
                var report = MeshValidator.Validate(positions, indices, uvCount: 0);
                if (report.Rejected)
                    throw new InvalidOperationException("Mesh.Validate failed before remesh.");
            });

            var remeshed = RemeshPipeline.Run(positions, indices, profile);
            PipelineTrace.Stage(_diagnostics, "Mesh", "Mesh.UV", "Completed", "spherical");

            // Real LOD mesh via RemeshPipeline profiles (not triangle-count arithmetic alone).
            var lod = LodService.BuildLodMesh(remeshed.Positions, remeshed.Indices, level: 1);
            PipelineTrace.Stage(_diagnostics, "Mesh", "Mesh.LOD", "Completed", lod.BackendId);
            _ = LodService.EstimateTriangleBudget(indices.Count / 3, 1);

            TriangleMeshExport.WriteGlb(destinationGlb, lod.Positions, lod.Indices, lod.Uvs);
            return destinationGlb;
        }, provider: "remesh");
    }
}
