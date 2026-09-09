using ThreeDGod.Application;
using ThreeDGod.Rigging;

namespace ThreeDGod.Infrastructure;

public sealed class GarmentSkinService : IGarmentSkinService
{
    public string BindToBody(string skinnedBodyGlb, string garmentGlb, string destinationGlb) =>
        GarmentSkinBinder.Bind(skinnedBodyGlb, garmentGlb, destinationGlb);
}
