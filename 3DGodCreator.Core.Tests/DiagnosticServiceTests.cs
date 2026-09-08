using System.Runtime.CompilerServices;
using ThreeDGod.Core.Diagnostics;

namespace ThreeDGodCreator.Core.Tests;

public class DiagnosticServiceTests
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowDiagnosticProbe()
    {
        throw new InvalidOperationException("diagnostic-probe");
    }

    [Fact]
    public void IntentionalException_HasRealSourceLocationAndCodeContext()
    {
        var enricher = new CSharpExceptionEnricher();
        DiagnosticIssue issue;
        try
        {
            ThrowDiagnosticProbe();
            throw new InvalidOperationException("probe did not throw");
        }
        catch (InvalidOperationException ex)
        {
            issue = enricher.Enrich(ex, "test");
        }

        Assert.Equal("diagnostic-probe", issue.Message);
        Assert.Contains("ThrowDiagnosticProbe", issue.Location.Method ?? "", StringComparison.Ordinal);
        Assert.Equal(SymbolStatus.Available, issue.Location.SymbolStatus);
        Assert.NotNull(issue.Location.FilePath);
        Assert.EndsWith("DiagnosticServiceTests.cs", issue.Location.FilePath, StringComparison.OrdinalIgnoreCase);
        Assert.True(issue.Location.Line > 0);
        Assert.NotNull(issue.CodeContext);
        Assert.Contains("ThrowDiagnosticProbe", string.Join('\n', issue.CodeContext!.Lines), StringComparison.Ordinal);
        var errorLine = issue.CodeContext.Lines[issue.CodeContext.ErrorLine - issue.CodeContext.StartLine];
        Assert.False(string.IsNullOrWhiteSpace(errorLine));
    }

    [Fact]
    public void MissingSymbols_DoNotInventLineNumbers()
    {
        var issue = new CSharpExceptionEnricher().Enrich(new Exception("no-stack-file"), "boundary");
        if (issue.Location.SymbolStatus == SymbolStatus.Unavailable)
            Assert.Null(issue.Location.Line);
    }
}
