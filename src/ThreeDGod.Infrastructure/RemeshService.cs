using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Mesh;

namespace ThreeDGod.Infrastructure;

public sealed class RemeshService : IRemeshService
{
    public string RemeshGlb(string sourceGlb, string destinationGlb, RemeshProfile profile) =>
        RemeshPipeline.RemeshGlb(sourceGlb, destinationGlb, profile);
}
