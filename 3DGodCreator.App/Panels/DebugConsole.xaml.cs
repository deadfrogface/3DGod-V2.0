using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.App.Panels;

public partial class DebugConsole : UserControl
{
    private readonly List<string> _fullLog = new();

    public System.Action? OnOpenSettingsRequested { get; set; }

    public DebugConsole()
    {
        InitializeComponent();
        UpdateChatPlaceholder();
    }

    private void ChatInput_GotFocus(object sender, RoutedEventArgs e) => UpdateChatPlaceholder();
    private void ChatInput_LostFocus(object sender, RoutedEventArgs e) => UpdateChatPlaceholder();
    private void ChatInput_TextChanged(object sender, TextChangedEventArgs e) => UpdateChatPlaceholder();

    private void UpdateChatPlaceholder()
    {
        ChatPlaceholder.Visibility = string.IsNullOrWhiteSpace(ChatInput.Text) ? Visibility.Visible : Visibility.Collapsed;
    }

    public void Log(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        _fullLog.Add(message);
        if (PassesFilter(message) && MatchesSearch(message))
            OutputBox.AppendText(message + "\n");
    }

    private bool PassesFilter(string line)
    {
        if (ChkAll.IsChecked == true) return true;
        var anyChecked = ChkSculpt.IsChecked == true || ChkExport.IsChecked == true || ChkRig.IsChecked == true || ChkAi.IsChecked == true;
        if (!anyChecked) return true;
        var lower = line.ToLowerInvariant();
        if (ChkSculpt.IsChecked == true && lower.Contains("sculpt")) return true;
        if (ChkExport.IsChecked == true && lower.Contains("export")) return true;
        if (ChkRig.IsChecked == true && (lower.Contains("rig") || lower.Contains("rigging"))) return true;
        if (ChkAi.IsChecked == true && (lower.Contains("ai") || lower.Contains("ki") || lower.Contains("froggy"))) return true;
        return false;
    }

    private bool MatchesSearch(string line)
    {
        var s = SearchBox.Text.Trim();
        return string.IsNullOrEmpty(s) || line.Contains(s, System.StringComparison.OrdinalIgnoreCase);
    }

    private void FilterChanged(object sender, RoutedEventArgs e) => ApplyFilter();

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        OutputBox.Clear();
        foreach (var line in _fullLog.Where(l => PassesFilter(l) && MatchesSearch(l)))
            OutputBox.AppendText(line + "\n");
    }

    private void BtnFroggy_Click(object sender, RoutedEventArgs e)
    {
        var logText = string.Join("\n", _fullLog);
        var result = FroggyService.AnalyzeLog(logText);

        OutputBox.AppendText("\n🐸 Froggy sagt:\n");
        OutputBox.AppendText($"❌ Problem: {result.Problem}\n");
        OutputBox.AppendText($"📎 Ursache: {result.Cause}\n");
        OutputBox.AppendText($"💡 Vorschlag: {result.Suggestion}\n");

        if (result.CanFix && result.FixAction == "OpenSettings")
        {
            var r = MessageBox.Show(
                "Froggy kann helfen: Öffne die Einstellungen, um den Blender-Pfad zu setzen?",
                "Froggy",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes)
                OnOpenSettingsRequested?.Invoke();
        }
    }

    private void ChatInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        AskFroggy();
    }

    private void AskFroggy()
    {
        var question = ChatInput.Text.Trim();
        if (string.IsNullOrEmpty(question)) return;

        OutputBox.AppendText($"\n🗣 Du: {question}\n");
        ChatInput.Clear();

        var logText = string.Join("\n", _fullLog);
        var answer = FroggyService.AnswerQuestion(question, logText);
        OutputBox.AppendText($"🐸 Froggy: {answer}\n");
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        _fullLog.Clear();
        OutputBox.Clear();
        SearchBox.Clear();
        Log("[Debug] Konsole geleert.");
    }

    private void BtnSystemCheck_Click(object sender, RoutedEventArgs e)
    {
        var report = DiagnosticsService.RunSystemCheck();
        Log(report);
    }
}
