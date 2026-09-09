using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;
using ThreeDGod.Application;

namespace ThreeDGod.Physics;

public sealed class AccessoryPhysicsWorld : IAccessoryPhysicsPreview
{
    private readonly BufferPool _pool;
    private readonly Simulation _simulation;
    private readonly List<BodyHandle> _dynamic = [];
    private readonly List<RigidPose> _restPoses = [];
    private readonly List<string> _names = [];
    private readonly List<float> _masses = [];
    private bool _disposed;

    private AccessoryPhysicsWorld(Simulation simulation, BufferPool pool, string kind)
    {
        _simulation = simulation;
        _pool = pool;
        Kind = kind;
    }

    public string Kind { get; }

    public static AccessoryPhysicsWorld CreateHangingChain()
    {
        var (sim, pool) = CreateSimulation();
        var world = new AccessoryPhysicsWorld(sim, pool, "chain");
        var radius = 0.035f;
        var sphere = new Sphere(radius);
        var shape = sim.Shapes.Add(sphere);
        var inertia = sphere.ComputeInertia(0.08f);
        var spacing = 0.09f;
        var anchor = new Vector3(0, 1.6f, 0);

        var kinematic = sim.Bodies.Add(BodyDescription.CreateKinematic(anchor, sim.Shapes.Add(new Sphere(0.02f)), 0.01f));
        BodyHandle previous = kinematic;
        for (var i = 0; i < 8; i++)
        {
            var pos = new Vector3(0.25f, 1.6f - (i + 1) * spacing, 0);
            var handle = sim.Bodies.Add(BodyDescription.CreateDynamic(pos, inertia, shape, 0.01f));
            sim.Solver.Add(previous, handle, new BallSocket
            {
                LocalOffsetA = i == 0 ? Vector3.Zero : new Vector3(0, -spacing * 0.5f, 0),
                LocalOffsetB = new Vector3(0, spacing * 0.5f, 0),
                SpringSettings = new SpringSettings(30, 3)
            });
            world.Track(handle, $"link-{i}", pos, 0.08f);
            previous = handle;
        }

        var floor = new Box(8, 0.2f, 8);
        sim.Statics.Add(new StaticDescription(new Vector3(0, -0.1f, 0), sim.Shapes.Add(floor)));
        return world;
    }

    public static AccessoryPhysicsWorld CreateEarringAgainstHead()
    {
        var (sim, pool) = CreateSimulation();
        var world = new AccessoryPhysicsWorld(sim, pool, "earring");
        var headRadius = 0.12f;
        var headCenter = new Vector3(0, 1.55f, 0);
        sim.Statics.Add(new StaticDescription(headCenter, sim.Shapes.Add(new Sphere(headRadius))));

        var ear = new Vector3(headRadius + 0.02f, 1.58f, 0);
        var kinematic = sim.Bodies.Add(BodyDescription.CreateKinematic(ear, sim.Shapes.Add(new Sphere(0.01f)), 0.01f));
        var beadR = 0.018f;
        var bead = new Sphere(beadR);
        var shape = sim.Shapes.Add(bead);
        var inertia = bead.ComputeInertia(0.03f);
        var spacing = 0.045f;
        BodyHandle previous = kinematic;
        for (var i = 0; i < 4; i++)
        {
            var pos = ear + new Vector3(0.04f, -(i + 1) * spacing, 0.02f);
            var handle = sim.Bodies.Add(BodyDescription.CreateDynamic(pos, inertia, shape, 0.01f));
            sim.Solver.Add(previous, handle, new BallSocket
            {
                LocalOffsetA = i == 0 ? Vector3.Zero : new Vector3(0, -spacing * 0.45f, 0),
                LocalOffsetB = new Vector3(0, spacing * 0.45f, 0),
                SpringSettings = new SpringSettings(40, 3)
            });
            world.Track(handle, $"earring-{i}", pos, 0.03f);
            previous = handle;
        }
        return world;
    }

    public void Step(float dt)
    {
        EnsureAlive();
        _simulation.Timestep(dt);
    }

    public void Reset()
    {
        EnsureAlive();
        for (var i = 0; i < _dynamic.Count; i++)
        {
            var body = _simulation.Bodies.GetBodyReference(_dynamic[i]);
            body.Pose = _restPoses[i];
            body.Velocity = default;
            body.Awake = true;
        }
    }

    public PhysicsSnapshot Capture()
    {
        EnsureAlive();
        var bodies = new List<PhysicsBodySample>(_dynamic.Count);
        var energy = 0f;
        for (var i = 0; i < _dynamic.Count; i++)
        {
            var body = _simulation.Bodies.GetBodyReference(_dynamic[i]);
            var v = body.Velocity.Linear;
            var speed = v.Length();
            energy += 0.5f * _masses[i] * speed * speed;
            var p = body.Pose.Position;
            bodies.Add(new PhysicsBodySample
            {
                Name = _names[i],
                X = p.X,
                Y = p.Y,
                Z = p.Z,
                Speed = speed
            });
        }
        return new PhysicsSnapshot { Bodies = bodies, KineticEnergy = energy };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _simulation.Dispose();
        _pool.Clear();
        _disposed = true;
    }

    private void Track(BodyHandle handle, string name, Vector3 rest, float mass)
    {
        _dynamic.Add(handle);
        _names.Add(name);
        _restPoses.Add(new RigidPose(rest));
        _masses.Add(mass);
    }

    private void EnsureAlive()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AccessoryPhysicsWorld));
    }

    private static (Simulation Simulation, BufferPool Pool) CreateSimulation()
    {
        var pool = new BufferPool();
        var sim = Simulation.Create(
            pool,
            new AccessoryNarrowPhaseCallbacks(),
            new GravityPoseIntegratorCallbacks(new Vector3(0, -9.81f, 0)),
            new SolveDescription(8, 1));
        return (sim, pool);
    }
}

public sealed class AccessoryPhysicsService : IAccessoryPhysicsService
{
    public IAccessoryPhysicsPreview CreateHangingChain() => AccessoryPhysicsWorld.CreateHangingChain();
    public IAccessoryPhysicsPreview CreateEarringAgainstHead() => AccessoryPhysicsWorld.CreateEarringAgainstHead();
}
