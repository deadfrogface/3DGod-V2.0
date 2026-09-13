using ThreeDGod.Application;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Core.Domain;
using ThreeDGod.Mesh;

namespace ThreeDGod.Infrastructure;

public sealed class RemeshService : IRemeshService
{
    private readonly IDiagnosticService? _diagnostics;
    private readonly IMeshProcessor _processor;

    public RemeshService(IDiagnosticService? diagnostics = null, IMeshProcessor? processor = null)
    {
        _diagnostics = diagnostics;
        _processor = processor ?? MeshProcessorSelector.Create();
    }

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

            var remeshed = _processor.Process(positions, indices, profile);
            PipelineTrace.Stage(_diagnostics, "Mesh", "Mesh.UV", "Completed", remeshed.BackendId);

            // Real LOD mesh via RemeshPipeline profiles (not triangle-count arithmetic alone).
            var lod = LodService.BuildLodMesh(remeshed.Positions, remeshed.Indices, level: 1);
            PipelineTrace.Stage(_diagnostics, "Mesh", "Mesh.LOD", "Completed", lod.BackendId);
            _ = LodService.EstimateTriangleBudget(indices.Count / 3, 1);

            TriangleMeshExport.WriteGlb(destinationGlb, lod.Positions, lod.Indices, lod.Uvs);
            return destinationGlb;
        }, provider: "remesh");
    }
}
