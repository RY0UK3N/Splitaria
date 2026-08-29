using Splitaria.Core;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Splitaria.App;

public partial class PreferencesWindow : Window
{
    private readonly AppSettings _settings;
    private readonly AppTheme _originalTheme;
    private UpdateRelease? _availableRelease;
    private bool _initialized;

    public PreferencesWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        _originalTheme = settings.Theme;
        SelectByTag(ThemeCombo, settings.Theme.ToString());
        SelectByTag(FolderPatternCombo, settings.FolderPattern.ToString());
        AutoPlayCheck.IsChecked = settings.AutoPlayVideoPreview;
        InstalledVersionText.Text = UpdateService.CurrentVersion.ToString(3);
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

    private async void CheckUpdates_Click(object sender, RoutedEventArgs e) => await CheckUpdatesAsync();

    private async Task CheckUpdatesAsync()
    {
        UpdateButton.IsEnabled = false;
        UpdateActionsPanel.Visibility = Visibility.Collapsed;
        UpdateStatusText.Text = "Consultando atualizações…";
        try
        {
            _availableRelease = await UpdateService.CheckAsync();
            if (_availableRelease is null)
            {
                AvailableVersionText.Text = UpdateService.CurrentVersion.ToString(3);
                UpdateStatusText.Text = "Você já está usando a versão mais recente.";
                return;
            }

            AvailableVersionText.Text = _availableRelease.Version.ToString(3);
            UpdateStatusText.Text = "Uma nova versão está pronta para download.";
            UpdateActionsPanel.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            AvailableVersionText.Text = "Não foi possível consultar";
            UpdateStatusText.Text = $"Não foi possível verificar: {ex.Message}";
        }
        finally
        {
            UpdateButton.IsEnabled = true;
        }
    }

    private async void InstallUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (_availableRelease is null) return;
        UpdateButton.IsEnabled = false;
        InstallUpdateButton.IsEnabled = false;
        try
        {
            var release = _availableRelease;

            var progress = new Progress<int>(percent =>
            {
                UpdateStatusText.Text = $"Baixando atualização… {percent}%";
                InstallUpdateButton.Content = $"Baixando… {percent}%";
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
            UpdateButton.IsEnabled = true;
            InstallUpdateButton.Content = "Baixar e instalar";
            InstallUpdateButton.IsEnabled = true;
        }
    }

    private void OpenRepository_Click(object sender, RoutedEventArgs e) => OpenUrl("https://github.com/RY0UK3N/Splitaria");
    private void OpenReleaseNotes_Click(object sender, RoutedEventArgs e)
    {
        if (_availableRelease is not null) OpenUrl(_availableRelease.PageUrl);
    }

    private static void OpenUrl(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

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

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DialogResult != true) ThemeManager.Apply(_originalTheme);
        base.OnClosed(e);
    }
}
