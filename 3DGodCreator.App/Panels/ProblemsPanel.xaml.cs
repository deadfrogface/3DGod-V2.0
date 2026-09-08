using System.Windows;
using System.Windows.Controls;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Infrastructure.Logging;

namespace ThreeDGodCreator.App.Panels;

public partial class ProblemsPanel : UserControl
{
    private readonly IDiagnosticService _diagnostics;

    public ProblemsPanel(IDiagnosticService diagnostics)
    {
        InitializeComponent();
        _diagnostics = diagnostics;
        Refresh();
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
            return;
        }
        ShortView.Text = $"{issue.ExceptionType}: {issue.Message} @ {issue.Location.Method}:{issue.Location.Line}";
        DetailsView.Text = CursorReportBuilder.CreateCursorReport(issue);
    }

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
