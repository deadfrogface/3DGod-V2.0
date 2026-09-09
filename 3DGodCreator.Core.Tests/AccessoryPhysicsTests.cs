using ThreeDGod.Application;
using ThreeDGod.Physics;

namespace ThreeDGodCreator.Core.Tests;

public class AccessoryPhysicsTests
{
    [Fact]
    public void HangingChain_MovesThenComesToRest_AndResetRestores()
    {
        using var world = AccessoryPhysicsWorld.CreateHangingChain();
        var rest = world.Capture();
        Assert.Equal(8, rest.Bodies.Count);

        for (var i = 0; i < 24; i++)
            world.Step(1f / 60f);
        var swinging = world.Capture();
        Assert.True(swinging.KineticEnergy > 0.001f ||
                    swinging.Bodies.Zip(rest.Bodies, (a, b) => MathF.Abs(a.X - b.X) + MathF.Abs(a.Y - b.Y)).Max() > 0.02f,
            "Chain did not move under gravity.");

        for (var i = 0; i < 480; i++)
            world.Step(1f / 60f);
        var settled = world.Capture();
        Assert.True(settled.KineticEnergy < 0.08f || settled.KineticEnergy < swinging.KineticEnergy * 0.4f,
            $"Chain did not settle: swingKE={swinging.KineticEnergy} restKE={settled.KineticEnergy}");
        Assert.True(settled.Bodies[^1].Y < settled.Bodies[0].Y - 0.2f, "Chain should hang downward.");

        world.Reset();
        var afterReset = world.Capture();
        Assert.InRange(afterReset.Bodies[0].X, rest.Bodies[0].X - 0.02f, rest.Bodies[0].X + 0.02f);
        Assert.InRange(afterReset.Bodies[0].Y, rest.Bodies[0].Y - 0.02f, rest.Bodies[0].Y + 0.02f);
        Assert.True(afterReset.KineticEnergy < 0.001f);
    }

    [Fact]
    public void Earring_CollidesWithHeadAndDoesNotEnterIt()
    {
        using var world = AccessoryPhysicsWorld.CreateEarringAgainstHead();
        for (var i = 0; i < 240; i++)
            world.Step(1f / 60f);
        var snap = world.Capture();
        Assert.Equal(4, snap.Bodies.Count);
        const float headX = 0f, headY = 1.55f, headZ = 0f, headR = 0.12f, beadR = 0.018f;
        foreach (var bead in snap.Bodies)
        {
            var dx = bead.X - headX;
            var dy = bead.Y - headY;
            var dz = bead.Z - headZ;
            var dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
            Assert.True(dist + 1e-3f >= headR + beadR * 0.25f,
                $"Bead {bead.Name} penetrated the head collider: dist={dist}");
        }
        Assert.True(snap.Bodies.Max(b => b.Y) - snap.Bodies.Min(b => b.Y) > 0.04f, "Earring links should remain a hanging chain.");
    }

    [Fact]
    public void Service_CreatesDistinctPreviewKinds()
    {
        var svc = new AccessoryPhysicsService();
        using var chain = svc.CreateHangingChain();
        using var earring = svc.CreateEarringAgainstHead();
        Assert.Equal("chain", chain.Kind);
        Assert.Equal("earring", earring.Kind);
    }
}
