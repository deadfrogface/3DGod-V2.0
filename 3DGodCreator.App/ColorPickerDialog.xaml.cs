using System.Windows;
using System.Windows.Media;

namespace ThreeDGodCreator.App;

public partial class ColorPickerDialog : Window
{
    public Color? SelectedColor { get; private set; }

    public ColorPickerDialog(string initialHex = "#cccccc")
    {
        InitializeComponent();
        TxtHex.Text = initialHex.TrimStart('#');
        UpdatePreview();
        TxtHex.TextChanged += (_, _) => UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (ParseHex(TxtHex.Text) is { } c)
            ColorPreview.Background = new SolidColorBrush(c);
    }

    private static Color? ParseHex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length is 6 or 8)
        {
            try
            {
                var r = Convert.ToByte(hex.Substring(0, 2), 16);
                var g = Convert.ToByte(hex.Substring(2, 2), 16);
                var b = Convert.ToByte(hex.Substring(4, 2), 16);
                return Color.FromRgb(r, g, b);
            }
            catch { }
        }
        return null;
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        SelectedColor = ParseHex(TxtHex.Text);
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
