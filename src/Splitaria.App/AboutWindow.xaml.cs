using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Splitaria.App;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version();
        VersionText.Text = $"Versão {version.ToString(3)}";
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void OpenNotices_Click(object sender, RoutedEventArgs e)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "THIRD-PARTY-NOTICES.txt");
        if (File.Exists(path))
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        else
            MessageBox.Show("O arquivo de avisos não foi encontrado.", "Splitaria",
                MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
