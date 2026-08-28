using Splitaria.Core;
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
            var release = await UpdateService.CheckAsync();
            if (release is null)
            {
                UpdateStatusText.Text = "Você já está usando a versão mais recente.";
                return;
            }

            UpdateStatusText.Text = $"Nova versão disponível: {release.Tag}";
            var choice = MessageBox.Show(
                $"A versão {release.Tag} está disponível.\n\nDeseja baixar e instalar a atualização agora?\nO Splitaria será fechado e reaberto ao concluir.",
                "Atualização disponível", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (choice != MessageBoxResult.Yes) return;

            var progress = new Progress<int>(percent =>
            {
                UpdateStatusText.Text = $"Baixando atualização… {percent}%";
                UpdateButton.Content = $"Baixando… {percent}%";
            });
            var installer = await UpdateService.DownloadAsync(release, progress);
            UpdateStatusText.Text = "Download concluído. Iniciando a atualização…";
            UpdateService.StartInstaller(installer);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = $"Não foi possível atualizar: {ex.Message}";
        }
        finally
        {
            UpdateButton.Content = "Verificar atualizações";
            UpdateButton.IsEnabled = true;
        }
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
