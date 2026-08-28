using Microsoft.Win32;
using LibVLCSharp.Shared;
using Splitaria.Core;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.ComponentModel;

namespace Splitaria.App;

public partial class MainWindow : Window
{
    private const int WmGetMinMaxInfo = 0x0024;
    private const uint MonitorDefaultToNearest = 0x00000002;
    private readonly ObservableCollection<MediaItem> _items = [];
    private readonly ObservableCollection<string> _sourceFolders = [];
    private readonly MediaScanner _scanner = new();
    private readonly MediaOrganizer _organizer = new();
    private readonly MediaPlayer _previewPlayer;
    private Media? _previewMedia;
    private string? _currentPreviewPath;
    private int _previewRequest;
    private AppSettings Settings => ((App)Application.Current).Settings;

    public MainWindow()
    {
        InitializeComponent();
        _previewPlayer = new MediaPlayer(VideoEngine.Shared) { Mute = true, Volume = 0 };
        _previewPlayer.Playing += PreviewPlayer_Playing;
        _previewPlayer.EncounteredError += PreviewPlayer_EncounteredError;
        PreviewVideo.MediaPlayer = _previewPlayer;
        FilesGrid.ItemsSource = _items;
        SourceFoldersList.ItemsSource = _sourceFolders;
        SetDefaultDestinations();
        RestoreWindowPlacement();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MaximizeRestore_Click(object sender, RoutedEventArgs e) => ToggleMaximize();
    private void CloseWindow_Click(object sender, RoutedEventArgs e) => Close();
    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void BrandMenu_Click(object sender, RoutedEventArgs e)
    {
        if (BrandMenuButton.ContextMenu is not { } menu) return;
        menu.PlacementTarget = BrandMenuButton;
        menu.IsOpen = true;
    }

    private void Preferences_Click(object sender, RoutedEventArgs e)
    {
        var previousPattern = Settings.FolderPattern;
        if (new PreferencesWindow(Settings) { Owner = this }.ShowDialog() == true && previousPattern != Settings.FolderPattern)
            InvalidateAnalysis();
    }

    private void ToggleMaximize() => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (MaximizeIcon is null || RestoreIcon is null) return;
        var maximized = WindowState == WindowState.Maximized;
        MaximizeIcon.Visibility = maximized ? Visibility.Collapsed : Visibility.Visible;
        RestoreIcon.Visibility = maximized ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        source?.AddHook(WindowProc);
    }

    private IntPtr WindowProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != WmGetMinMaxInfo) return IntPtr.Zero;
        var info = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        var dpiScale = GetDpiForWindow(hwnd) / 96d;
        info.MinTrackSize.X = (int)Math.Ceiling(MinWidth * dpiScale);
        info.MinTrackSize.Y = (int)Math.Ceiling(MinHeight * dpiScale);
        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor != IntPtr.Zero)
        {
            var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (GetMonitorInfo(monitor, ref monitorInfo))
            {
                var work = monitorInfo.WorkArea;
                var bounds = monitorInfo.MonitorArea;
                info.MaxPosition.X = Math.Abs(work.Left - bounds.Left);
                info.MaxPosition.Y = Math.Abs(work.Top - bounds.Top);
                info.MaxSize.X = Math.Abs(work.Right - work.Left);
                info.MaxSize.Y = Math.Abs(work.Bottom - work.Top);
                info.MinTrackSize.X = Math.Min(info.MinTrackSize.X, info.MaxSize.X);
                info.MinTrackSize.Y = Math.Min(info.MinTrackSize.Y, info.MaxSize.Y);
            }
        }
        Marshal.StructureToPtr(info, lParam, true);
        handled = true;
        return IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public NativePoint Reserved;
        public NativePoint MaxSize;
        public NativePoint MaxPosition;
        public NativePoint MinTrackSize;
        public NativePoint MaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left; public int Top; public int Right; public int Bottom; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect MonitorArea;
        public NativeRect WorkArea;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    private void ChooseSource_Click(object sender, RoutedEventArgs e)
    {
        var folder = ChooseFolder("Escolha a pasta que contém suas fotos e vídeos");
        if (folder is null) return;
        if (!_sourceFolders.Contains(folder, StringComparer.OrdinalIgnoreCase)) _sourceFolders.Add(folder);
        InvalidateAnalysis();
    }

    private void RemoveSource_Click(object sender, RoutedEventArgs e)
    {
        var selected = SourceFoldersList.SelectedItems.Cast<string>().ToArray();
        foreach (var folder in selected) _sourceFolders.Remove(folder);
        InvalidateAnalysis();
    }

    private void RemoveSourceItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string folder })
        {
            _sourceFolders.Remove(folder);
            InvalidateAnalysis();
        }
        e.Handled = true;
    }

    private void ChoosePhotoDestination_Click(object sender, RoutedEventArgs e)
    {
        var folder = ChooseFolder("Escolha onde organizar as fotos");
        if (folder is null) return;
        PhotoDestinationTextBox.Text = folder;
        InvalidateAnalysis();
    }

    private void ChooseVideoDestination_Click(object sender, RoutedEventArgs e)
    {
        var folder = ChooseFolder("Escolha onde organizar os vídeos");
        if (folder is null) return;
        VideoDestinationTextBox.Text = folder;
        InvalidateAnalysis();
    }

    private void UseDefaults_Click(object sender, RoutedEventArgs e)
    {
        SetDefaultDestinations();
        InvalidateAnalysis();
    }

    private async void Analyze_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateFolders()) return;
        HideCompletion();
        SetBusy(true, "Analisando metadados e duplicados…");
        _items.Clear();
        try
        {
            var progress = new Progress<int>(value => ProgressBar.Value = value);
            var result = await _scanner.ScanAsync(new ScanOptions(_sourceFolders.ToArray(),
                PhotoDestinationTextBox.Text, VideoDestinationTextBox.Text,
                SubfoldersCheckBox.IsChecked == true, Settings.FolderPattern), progress);
            foreach (var item in result)
            {
                item.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(MediaItem.IsSelected)) RefreshSelectionSummary(); };
                _items.Add(item);
            }
            RefreshSelectionSummary();
            StatusText.Text = result.Count == 0 ? "Nenhuma foto ou vídeo encontrado" : "Análise concluída — revise a seleção";
            ProgressBar.Value = result.Count == 0 ? 0 : 100;
            OrganizeButton.IsEnabled = result.Any(item => item.IsSelected);
            if (_items.Count > 0) FilesGrid.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Não foi possível analisar", MessageBoxButton.OK, MessageBoxImage.Warning);
            StatusText.Text = "A análise não foi concluída";
            ProgressBar.Value = 0;
        }
        finally
        {
            AnalyzeButton.IsEnabled = true;
        }
    }

    private async void Organize_Click(object sender, RoutedEventArgs e)
    {
        var selected = _items.Where(item => item.IsSelected).ToArray();
        if (selected.Length == 0) return;
        var duplicateAction = DuplicateAction.Skip;
        var conflicts = selected.Count(item => item.HasDuplicateIssue);
        if (conflicts > 0)
        {
            var choice = MessageBox.Show(
                $"Há {conflicts} arquivo(s) duplicado(s) ou com o mesmo nome.\n\n" +
                "Sim — manter os dois, criando nomes como ‘foto (2).jpg’.\n" +
                "Não — ignorar duplicados e conflitos.\n" +
                "Cancelar — voltar para a revisão.",
                "Como tratar duplicados?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (choice == MessageBoxResult.Cancel) return;
            duplicateAction = choice == MessageBoxResult.Yes ? DuplicateAction.KeepBoth : DuplicateAction.Skip;
        }

        var confirmation = MessageBox.Show(
            $"Copiar {selected.Length} arquivo(s) selecionado(s)?\n\nOs originais serão preservados.",
            "Confirmar organização", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirmation != MessageBoxResult.Yes) return;

        SetBusy(true, $"Organizando 0/{selected.Length}…");
        var progress = new Progress<(int Current, int Total)>(value =>
        {
            ProgressBar.Value = value.Current * 100d / Math.Max(value.Total, 1);
            StatusText.Text = $"Organizando {value.Current}/{value.Total}…";
        });
        var stopwatch = Stopwatch.StartNew();
        var result = await _organizer.CopyAsync(selected, duplicateAction, progress);
        stopwatch.Stop();
        StatusText.Text = $"Concluído: {result.Copied} copiados, {result.Skipped} ignorados, {result.Failed} falhas";
        ProgressBar.Value = 100;
        AnalyzeButton.IsEnabled = true;
        OrganizeButton.IsEnabled = false;
        ShowCompletion(result, stopwatch.Elapsed);
    }

    private void ShowCompletion(OrganizationResult result, TimeSpan elapsed)
    {
        CopiedCountText.Text = result.Copied.ToString();
        PhotoCountText.Text = result.CopiedPhotos.ToString();
        VideoCountText.Text = result.CopiedVideos.ToString();
        SkippedFailedCountText.Text = $"{result.Skipped} · {result.Failed}";
        CompletionSubtitleText.Text = $"{result.Processed} arquivos processados em {FormatDuration(elapsed)}";
        OpenPhotosButton.IsEnabled = result.CopiedPhotos > 0 && Directory.Exists(PhotoDestinationTextBox.Text);
        OpenVideosButton.IsEnabled = result.CopiedVideos > 0 && Directory.Exists(VideoDestinationTextBox.Text);
        PreviewPanel.Visibility = Visibility.Collapsed;
        CompletionPanel.Visibility = Visibility.Visible;
    }

    private static string FormatDuration(TimeSpan elapsed) => elapsed.TotalSeconds < 1
        ? "menos de 1 segundo"
        : elapsed.TotalSeconds < 60
            ? $"{elapsed.TotalSeconds:0.#} segundos"
            : $"{(int)elapsed.TotalMinutes} min {elapsed.Seconds} s";

    private void HideCompletion()
    {
        CompletionPanel.Visibility = Visibility.Collapsed;
        PreviewPanel.Visibility = Visibility.Visible;
    }

    private void CloseCompletion_Click(object sender, RoutedEventArgs e) => HideCompletion();
    private void NewOrganization_Click(object sender, RoutedEventArgs e) => InvalidateAnalysis();
    private void OpenPhotos_Click(object sender, RoutedEventArgs e) => OpenDestination(PhotoDestinationTextBox.Text);
    private void OpenVideos_Click(object sender, RoutedEventArgs e) => OpenDestination(VideoDestinationTextBox.Text);

    private static void OpenDestination(string path)
    {
        if (Directory.Exists(path)) Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    private void FilesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FilesGrid.SelectedItem is not MediaItem item) { ClearPreview(); return; }
        SelectedNameText.Text = item.Name;
        SelectedDateText.Text = $"Data usada: {item.DateLabel} · {item.DateSource}";
        SelectedSourceText.Text = $"Origem: {item.SourcePath}";
        SelectedDestinationText.Text = $"Destino: {item.DestinationPath}";
        SelectedDuplicateText.Text = item.HasDuplicateIssue
            ? $"Atenção: {item.StatusLabel}{(item.DuplicateOf is null ? "" : $" · {item.DuplicateOf}")}" : "Sem duplicidade detectada";
        SelectedDuplicateText.Foreground = item.HasDuplicateIssue
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(160, 73, 32))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(55, 125, 78));
        LoadPreview(item);
    }

    private void Preview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (FilesGrid.SelectedItem is MediaItem item)
        {
            _previewRequest++;
            _previewPlayer.Stop();
            _previewMedia?.Dispose();
            _previewMedia = null;
            new PreviewWindow(item) { Owner = this }.ShowDialog();
            if (FilesGrid.SelectedItem is MediaItem selected &&
                string.Equals(selected.SourcePath, item.SourcePath, StringComparison.OrdinalIgnoreCase))
                LoadPreview(selected);
        }
    }

    private void LoadPreview(MediaItem item)
    {
        _currentPreviewPath = item.SourcePath;
        PreviewImage.Source = null;
        _previewRequest++;
        _previewPlayer.Stop();
        _previewMedia?.Dispose();
        _previewMedia = null;
        PreviewVideo.Visibility = Visibility.Collapsed;
        if (item.Kind == MediaKind.Video)
        {
            PreviewPlaceholder.Visibility = Visibility.Collapsed;
            PreviewVideo.Visibility = Visibility.Visible;
            _previewMedia = new Media(VideoEngine.Shared, item.SourcePath, FromType.FromPath);
            _previewMedia.AddOption(":no-audio");
            if (!_previewPlayer.Play(_previewMedia)) ShowVideoPreviewError();
            _previewPlayer.Mute = true;
            _previewPlayer.Volume = 0;
            return;
        }
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 520;
            bitmap.UriSource = new Uri(item.SourcePath);
            bitmap.EndInit();
            bitmap.Freeze();
            PreviewImage.Source = bitmap;
            PreviewPlaceholder.Visibility = Visibility.Collapsed;
        }
        catch
        {
            PreviewPlaceholder.Text = "Não foi possível gerar a prévia deste arquivo.";
            PreviewPlaceholder.Visibility = Visibility.Visible;
        }
    }

    private async void PreviewPlayer_Playing(object? sender, EventArgs e)
    {
        var request = _previewRequest;
        _previewPlayer.Mute = true;
        _previewPlayer.Volume = 0;
        await Dispatcher.InvokeAsync(() => PreviewPlaceholder.Visibility = Visibility.Collapsed);
        if (Settings.AutoPlayVideoPreview) return;
        await Task.Delay(350);
        await Dispatcher.InvokeAsync(() =>
        {
            if (request != _previewRequest) return;
            if (_previewPlayer.Length > 2000) _previewPlayer.Time = 1000;
            _previewPlayer.SetPause(true);
        });
    }

    private void PreviewPlayer_EncounteredError(object? sender, EventArgs e) =>
        Dispatcher.BeginInvoke(ShowVideoPreviewError);

    private void ShowVideoPreviewError()
    {
        if (FilesGrid.SelectedItem is not MediaItem { Kind: MediaKind.Video } selected ||
            !string.Equals(selected.SourcePath, _currentPreviewPath, StringComparison.OrdinalIgnoreCase)) return;
        PreviewVideo.Visibility = Visibility.Collapsed;
        PreviewPlaceholder.Text = "Não foi possível gerar a prévia deste vídeo.";
        PreviewPlaceholder.Visibility = Visibility.Visible;
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _items) item.IsSelected = true;
        RefreshSelectionSummary();
    }

    private void ClearSelection_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _items) item.IsSelected = false;
        RefreshSelectionSummary();
    }

    private void HeaderSelectAll_Click(object sender, RoutedEventArgs e)
    {
        var select = sender is CheckBox { IsChecked: true };
        foreach (var item in _items) item.IsSelected = select;
        RefreshSelectionSummary();
        e.Handled = true;
    }

    private void RefreshSelectionSummary()
    {
        var photos = _items.Count(item => item.Kind == MediaKind.Photo);
        var videos = _items.Count - photos;
        var duplicates = _items.Count(item => item.HasDuplicateIssue);
        var selected = _items.Count(item => item.IsSelected);
        SummaryText.Text = $"{_items.Count} arquivos  •  {photos} fotos  •  {videos} vídeos  •  {duplicates} alertas  •  {selected} selecionados";
        OrganizeButton.IsEnabled = selected > 0 && AnalyzeButton.IsEnabled;
    }

    private void About_Click(object sender, RoutedEventArgs e) =>
        new AboutWindow { Owner = this }.ShowDialog();

    private bool ValidateFolders()
    {
        if (_sourceFolders.Count == 0 || _sourceFolders.Any(folder => !Directory.Exists(folder)))
            return Warn("Adicione ao menos uma pasta de origem válida.");
        if (string.IsNullOrWhiteSpace(PhotoDestinationTextBox.Text) || string.IsNullOrWhiteSpace(VideoDestinationTextBox.Text))
            return Warn("Escolha os destinos de fotos e vídeos.");
        if (_sourceFolders.Any(source => IsSameOrSubfolder(PhotoDestinationTextBox.Text, source) ||
                                         IsSameOrSubfolder(VideoDestinationTextBox.Text, source)))
            return Warn("Os destinos não podem ficar dentro da pasta de origem.");
        return true;
    }

    private static bool IsSameOrSubfolder(string candidate, string parent)
    {
        var fullCandidate = Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullParent = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullCandidate.StartsWith(fullParent, StringComparison.OrdinalIgnoreCase);
    }

    private static bool Warn(string message)
    {
        MessageBox.Show(message, "Splitaria", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private static string? ChooseFolder(string title)
    {
        var dialog = new OpenFolderDialog { Title = title, Multiselect = false };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    private void SetDefaultDestinations()
    {
        PhotoDestinationTextBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        VideoDestinationTextBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
    }

    private void InvalidateAnalysis()
    {
        HideCompletion();
        _items.Clear(); OrganizeButton.IsEnabled = false;
        SummaryText.Text = "Escolha as pastas e analise para montar a prévia.";
        StatusText.Text = "Pronto para analisar"; ProgressBar.Value = 0; ClearPreview();
    }

    private void ClearPreview()
    {
        _currentPreviewPath = null;
        PreviewImage.Source = null; PreviewPlaceholder.Text = "Selecione um arquivo para visualizar";
        _previewRequest++; _previewPlayer.Stop(); _previewMedia?.Dispose(); _previewMedia = null;
        PreviewVideo.Visibility = Visibility.Collapsed;
        PreviewPlaceholder.Visibility = Visibility.Visible; SelectedNameText.Text = "Nenhum arquivo selecionado";
        SelectedDateText.Text = SelectedSourceText.Text = SelectedDestinationText.Text = SelectedDuplicateText.Text = string.Empty;
    }

    private void SetBusy(bool busy, string status)
    {
        AnalyzeButton.IsEnabled = !busy; OrganizeButton.IsEnabled = !busy && _items.Any(item => item.IsSelected);
        StatusText.Text = status; ProgressBar.Value = 0;
    }

    protected override void OnClosed(EventArgs e)
    {
        _previewPlayer.Playing -= PreviewPlayer_Playing;
        _previewPlayer.EncounteredError -= PreviewPlayer_EncounteredError;
        _previewPlayer.Stop();
        PreviewVideo.MediaPlayer = null;
        _previewMedia?.Dispose();
        _previewMedia = null;
        _previewPlayer.Dispose();
        base.OnClosed(e);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        var bounds = WindowState == WindowState.Normal
            ? new Rect(Left, Top, ActualWidth, ActualHeight)
            : RestoreBounds;
        Settings.HasWindowPlacement = true;
        Settings.WindowWidth = Math.Max(MinWidth, bounds.Width);
        Settings.WindowHeight = Math.Max(MinHeight, bounds.Height);
        Settings.WindowLeft = bounds.Left;
        Settings.WindowTop = bounds.Top;
        Settings.WindowWasMaximized = WindowState == WindowState.Maximized;
        Settings.Save();
        base.OnClosing(e);
    }

    private void RestoreWindowPlacement()
    {
        if (!Settings.HasWindowPlacement) return;
        Width = Math.Max(MinWidth, Settings.WindowWidth);
        Height = Math.Max(MinHeight, Settings.WindowHeight);

        var visibleLeft = Math.Max(SystemParameters.VirtualScreenLeft, Math.Min(Settings.WindowLeft,
            SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - MinWidth));
        var visibleTop = Math.Max(SystemParameters.VirtualScreenTop, Math.Min(Settings.WindowTop,
            SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 80));
        Left = visibleLeft;
        Top = visibleTop;
        WindowStartupLocation = WindowStartupLocation.Manual;
        if (Settings.WindowWasMaximized) WindowState = WindowState.Maximized;
    }
}
