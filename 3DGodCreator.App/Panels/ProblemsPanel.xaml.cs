using System.Windows;
using System.Windows.Controls;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Infrastructure.Logging;
using ThreeDGodCreator.App.Localization;
using ThreeDGodCreator.Core.Localization;

namespace ThreeDGodCreator.App.Panels;

public partial class ProblemsPanel : UserControl, ILocalizableView
{
    private readonly IDiagnosticService _diagnostics;
    private readonly Func<DiagnosticIssue, bool>? _showInViewport;
    private readonly Action? _clearHighlight;

    public ProblemsPanel(
        IDiagnosticService diagnostics,
        Func<DiagnosticIssue, bool>? showInViewport = null,
        Action? clearHighlight = null)
    {
        InitializeComponent();
        _diagnostics = diagnostics;
        _showInViewport = showInViewport;
        _clearHighlight = clearHighlight;
        Refresh();
        ApplyLocalization();
    }

    public void ApplyLocalization()
    {
        LblTitle.Text = Loc.Get("problems.title");
        BtnShowObject.Content = Loc.Get("problems.show_object");
        BtnShowObject.ToolTip = Loc.Get("problems.show_object.tooltip");
        BtnClearHighlight.Content = Loc.Get("problems.clear_highlight");
        BtnCopyError.Content = Loc.Get("problems.copy_error");
        BtnCopyDetails.Content = Loc.Get("problems.copy_details");
        BtnOpenLog.Content = Loc.Get("problems.open_log");
    }

    public void Refresh()
    {
        IssueList.Items.Clear();
        foreach (var issue in _diagnostics.Issues)
            IssueList.Items.Add(issue);
        IssueList.DisplayMemberPath = nameof(DiagnosticIssue.Message);
        if (IssueList.Items.Count > 0)
            IssueList.SelectedIndex = IssueList.Items.Count - 1;
    }

    private DiagnosticIssue? Selected => IssueList.SelectedItem as DiagnosticIssue;

    private void IssueList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var issue = Selected;
        if (issue is null)
        {
            ShortView.Text = "";
            DetailsView.Text = "";
            BtnShowObject.IsEnabled = false;
            return;
        }
        ShortView.Text = $"{issue.ExceptionType}: {issue.Message} @ {issue.Location.Method}:{issue.Location.Line}";
        DetailsView.Text = CursorReportBuilder.CreateCursorReport(issue);
        BtnShowObject.IsEnabled = issue.Scene is not null;
    }

    private void BtnShowObject_Click(object sender, RoutedEventArgs e)
    {
        var issue = Selected;
        if (issue is null)
            return;
        if (issue.Scene is null)
        {
            MessageBox.Show(
                "Dieses Diagnostic hat keine Mesh-/Vertex-Referenz. Betroffenes Objekt kann nicht fokussiert werden.",
                "Betroffenes Objekt",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (_showInViewport is null)
        {
            MessageBox.Show("Viewport ist nicht verdrahtet.", "Betroffenes Objekt", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _showInViewport(issue);
    }

    private void BtnClearHighlight_Click(object sender, RoutedEventArgs e) => _clearHighlight?.Invoke();

    private void BtnCopyError_Click(object sender, RoutedEventArgs e)
    {
        var issue = Selected;
        if (issue is null) return;
        Clipboard.SetText($"{issue.ExceptionType}: {issue.Message}\n{issue.Location.FilePath}:{issue.Location.Line}");
    }

    private void BtnCopyDetails_Click(object sender, RoutedEventArgs e)
    {
        var issue = Selected;
        if (issue is null) return;
        Clipboard.SetText(CursorReportBuilder.CreateCursorReport(issue));
    }

    private void BtnOpenLog_Click(object sender, RoutedEventArgs e) => GodLog.OpenLogFolder();
}
