namespace ThreeDGod.Core.Diagnostics;

/// <summary>
/// Emits Started/Completed/Failed/Fallback breadcrumbs around real pipeline work.
/// </summary>
public static class PipelineTrace
{
    public static void Stage(
        IDiagnosticService? diagnostics,
        string pipeline,
        string stage,
        string status,
        string? provider = null,
        string substage = "")
    {
        diagnostics?.AddBreadcrumb(new DiagnosticBreadcrumb
        {
            Pipeline = pipeline,
            Stage = stage,
            Substage = substage,
            Status = status,
            Provider = provider
        });
    }

    public static T Run<T>(
        IDiagnosticService? diagnostics,
        string pipeline,
        string stage,
        Func<T> action,
        string? provider = null,
        string substage = "")
    {
        Stage(diagnostics, pipeline, stage, "Started", provider, substage);
        try
        {
            var result = action();
            Stage(diagnostics, pipeline, stage, "Completed", provider, substage);
            return result;
        }
        catch
        {
            Stage(diagnostics, pipeline, stage, "Failed", provider, substage);
            throw;
        }
    }

    public static void Run(
        IDiagnosticService? diagnostics,
        string pipeline,
        string stage,
        Action action,
        string? provider = null,
        string substage = "")
    {
        Run(diagnostics, pipeline, stage, () =>
        {
            action();
            return true;
        }, provider, substage);
    }

    public static async Task<T> RunAsync<T>(
        IDiagnosticService? diagnostics,
        string pipeline,
        string stage,
        Func<Task<T>> action,
        string? provider = null,
        string substage = "")
    {
        Stage(diagnostics, pipeline, stage, "Started", provider, substage);
        try
        {
            var result = await action().ConfigureAwait(false);
            Stage(diagnostics, pipeline, stage, "Completed", provider, substage);
            return result;
        }
        catch
        {
            Stage(diagnostics, pipeline, stage, "Failed", provider, substage);
            throw;
        }
    }

    public static async Task RunAsync(
        IDiagnosticService? diagnostics,
        string pipeline,
        string stage,
        Func<Task> action,
        string? provider = null,
        string substage = "")
    {
        await RunAsync(diagnostics, pipeline, stage, async () =>
        {
            await action().ConfigureAwait(false);
            return true;
        }, provider, substage).ConfigureAwait(false);
    }

    public static void Fallback(
        IDiagnosticService? diagnostics,
        string pipeline,
        string stage,
        string? provider = null,
        string substage = "") =>
        Stage(diagnostics, pipeline, stage, "Fallback", provider, substage);
}
