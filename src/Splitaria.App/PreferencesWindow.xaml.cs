using Splitaria.Core;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace Splitaria.App;

public partial class PreferencesWindow : Window
{
    private readonly AppSettings _settings;
    private readonly AppTheme _originalTheme;
    private bool _initialized;

    public PreferencesWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        _originalTheme = settings.Theme;
        SelectByTag(ThemeCombo, settings.Theme.ToString());
        SelectByTag(FolderPatternCombo, settings.FolderPattern.ToString());
        AutoPlayCheck.IsChecked = settings.AutoPlayVideoPreview;
        _initialized = true;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _settings.Theme = Enum.Parse<AppTheme>(SelectedTag(ThemeCombo));
        _settings.FolderPattern = Enum.Parse<FolderPattern>(SelectedTag(FolderPatternCombo));
        _settings.AutoPlayVideoPreview = AutoPlayCheck.IsChecked == true;
        _settings.Save();
        ThemeManager.Apply(_settings.Theme);
        DialogResult = true;
    }

    private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        UpdateButton.IsEnabled = false;
        UpdateStatusText.Text = "Consultando atualizações…";
        try
        {
            using var client = new HttpClient();
            var current = Assembly.GetExecutingAssembly().GetName().Version ?? new Version();
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"Splitaria/{current.ToString(3)}");
            var json = await client.GetStringAsync("https://api.github.com/repos/RY0UK3N/Splitaria/releases/latest");
            using var document = JsonDocument.Parse(json);
            var tag = document.RootElement.GetProperty("tag_name").GetString() ?? "";
            var page = document.RootElement.GetProperty("html_url").GetString();
            var available = Version.TryParse(tag.TrimStart('v', 'V'), out var latest) && latest > current;
            UpdateStatusText.Text = available ? $"Nova versão disponível: {tag}" : "Você já está usando a versão mais recente.";
            if (available && page is not null && MessageBox.Show($"A versão {tag} está disponível. Abrir a página para baixar?",
                    "Atualização disponível", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                Process.Start(new ProcessStartInfo(page) { UseShellExecute = true });
        }
        catch
        {
            UpdateStatusText.Text = "Não foi possível consultar as atualizações. Verifique sua conexão ou se já existe uma versão publicada.";
        }
        finally { UpdateButton.IsEnabled = true; }
    }

    private static string SelectedTag(ComboBox combo) => (combo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "System";
    private static void SelectByTag(ComboBox combo, string tag) => combo.SelectedItem = combo.Items.Cast<ComboBoxItem>().First(item => Equals(item.Tag, tag));
    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initialized) ThemeManager.Apply(Enum.Parse<AppTheme>(SelectedTag(ThemeCombo)));
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        ThemeManager.Apply(_originalTheme);
        DialogResult = false;
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DialogResult != true) ThemeManager.Apply(_originalTheme);
        base.OnClosed(e);
    }
}
