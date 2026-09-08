using ThreeDGod.Core.Domain;
using ThreeDGod.Persistence;

namespace ThreeDGodCreator.Core.Tests;

public class AutosaveServiceTests
{
    [Fact]
    public async Task SimulatedCrash_AfterAutosave_RecoveryIsDetectedAndLoadable()
    {
        var recovery = CreateTempDir();
        var mainPath = Path.Combine(CreateTempDir(), "project.3dgod");
        File.WriteAllText(mainPath, "MAIN-NOT-TOUCHED");
        try
        {
            var original = new ProjectBundle { Project = new ProjectDocument { Name = "CrashDemo" } };
            var live = new AutosaveService(recovery, TimeSpan.Zero);
            live.AssociateMainFile(mainPath);
            live.MarkDirty(original);
            Assert.True(live.IsDirty);
            await live.FlushAsync();
            Assert.False(live.IsDirty);
            Assert.Equal("MAIN-NOT-TOUCHED", File.ReadAllText(mainPath));

            // Simulated crash: new process, same recovery folder, main file unchanged.
            var restarted = new AutosaveService(recovery, TimeSpan.Zero);
            var sessions = restarted.ListRecoveries();
            Assert.NotEmpty(sessions);
            var restored = await restarted.RestoreAsync(sessions[0].SessionId);
            Assert.Equal(original.Project.ProjectId, restored.Project.ProjectId);
            Assert.Equal("CrashDemo", restored.Project.Name);
            Assert.Equal("MAIN-NOT-TOUCHED", File.ReadAllText(mainPath));
        }
        finally
        {
            Cleanup(recovery);
            Cleanup(Path.GetDirectoryName(mainPath)!);
        }
    }

    [Fact]
    public async Task Debounce_CollapsesRapidDirtyMarks_ToOneRecoveryFile()
    {
        var recovery = CreateTempDir();
        try
        {
            var svc = new AutosaveService(recovery, TimeSpan.FromMilliseconds(60));
            var bundle = new ProjectBundle { Project = new ProjectDocument { Name = "n1" } };
            var t1 = svc.ScheduleAutosaveAsync(bundle);
            bundle.Project.Name = "n2";
            var t2 = svc.ScheduleAutosaveAsync(bundle);
            bundle.Project.Name = "n3";
            var t3 = svc.ScheduleAutosaveAsync(bundle);
            await Task.WhenAll(t1, t2, t3);
            var files = Directory.GetFiles(recovery, "*.3dgod");
            Assert.Single(files);
            var loaded = await svc.RestoreAsync(Path.GetFileNameWithoutExtension(files[0]));
            Assert.Equal("n3", loaded.Project.Name);
        }
        finally
        {
            Cleanup(recovery);
        }
    }

    [Fact]
    public async Task RestoreAndDiscard_RemoveRecoverySession()
    {
        var recovery = CreateTempDir();
        try
        {
            var svc = new AutosaveService(recovery, TimeSpan.Zero);
            await svc.FlushAsync();
            var bundle = new ProjectBundle { Project = new ProjectDocument { Name = "keep" } };
            svc.MarkDirty(bundle);
            await svc.FlushAsync();
            var session = Assert.Single(svc.ListRecoveries());
            var restored = await svc.RestoreAsync(session.SessionId);
            Assert.Equal("keep", restored.Project.Name);
            svc.Discard(session.SessionId);
            Assert.Empty(svc.ListRecoveries());
        }
        finally
        {
            Cleanup(recovery);
        }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "3dgod-recovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void Cleanup(string dir)
    {
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }
}
