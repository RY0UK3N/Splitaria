using LibVLCSharp.Shared;
using Splitaria.Core;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Splitaria.App;

public partial class PreviewWindow : Window
{
    private readonly bool _isVideo;
    private readonly MediaPlayer? _videoPlayer;
    private Media? _videoMedia;
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _controlsTimer;
    private readonly DispatcherTimer _settingsSaveTimer;
    private bool _isPlaying;
    private bool _isMuted;
    private int _lastAudibleVolume = 50;
    private bool _updatingTimeline;
    private bool _controlsReady;
    private bool _isClosing;

    public PreviewWindow(MediaItem item)
    {
        InitializeComponent();
        _settings = ((App)Application.Current).Settings;
        _controlsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
        _controlsTimer.Tick += ControlsTimer_Tick;
        _settingsSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
        _settingsSaveTimer.Tick += SettingsSaveTimer_Tick;
        Title = $"{item.Name} — Splitaria";
        WindowTitleText.Text = item.Name;
        PathText.Text = item.SourcePath;
        _isVideo = item.Kind == MediaKind.Video;

        if (_isVideo)
        {
            _isMuted = _settings.VideoMuted;
            if (_settings.VideoVolume > 0) _lastAudibleVolume = Math.Clamp(_settings.VideoVolume, 1, 100);
            _videoPlayer = new MediaPlayer(VideoEngine.Shared)
            {
                Volume = Math.Clamp(_settings.VideoVolume, 0, 100),
                Mute = _isMuted
            };
            _videoPlayer.Playing += VideoPlayer_Playing;
            _videoPlayer.Paused += VideoPlayer_Paused;
            _videoPlayer.TimeChanged += VideoPlayer_TimeChanged;
            _videoPlayer.LengthChanged += VideoPlayer_LengthChanged;
            _videoPlayer.EndReached += VideoPlayer_EndReached;
            _videoPlayer.EncounteredError += VideoPlayer_EncounteredError;
            LargeVideo.MediaPlayer = _videoPlayer;
            VolumeSlider.Value = _videoPlayer.Volume;
            UpdateMuteLabel();
            _controlsReady = true;
            LoadVideo(item.SourcePath);
        }
        else LoadPhoto(item.SourcePath);
    }

    private void LoadPhoto(string path)
    {
        LargeVideo.Visibility = Visibility.Collapsed;
        VideoControlsPanel.Visibility = Visibility.Collapsed;
        MuteButton.Visibility = Visibility.Collapsed;
        VolumeSlider.Visibility = Visibility.Collapsed;
        ErrorText.Visibility = Visibility.Collapsed;
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path);
            bitmap.EndInit();
            bitmap.Freeze();
            LargeImage.Source = bitmap;
        }
        catch
        {
            ErrorText.Text = "Não foi possível abrir esta imagem.";
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void LoadVideo(string path)
    {
        LargeImage.Visibility = Visibility.Collapsed;
        LargeVideo.Visibility = Visibility.Visible;
        VideoControlsPanel.Visibility = Visibility.Visible;
        MuteButton.Visibility = Visibility.Visible;
        VolumeSlider.Visibility = Visibility.Visible;
        _videoMedia?.Dispose();
        _videoMedia = new Media(VideoEngine.Shared, path, FromType.FromPath);
        _isPlaying = _videoPlayer?.Play(_videoMedia) == true;
        if (!_isPlaying) ShowVideoError();
    }

    private void VideoPlayer_Playing(object? sender, EventArgs e) => DispatchIfOpen(() =>
    {
        _isPlaying = true;
        UpdatePlaybackIcon();
        RestartControlsTimer();
    });

    private void VideoPlayer_Paused(object? sender, EventArgs e) => DispatchIfOpen(() =>
    {
        _isPlaying = false;
        UpdatePlaybackIcon();
        ShowControls();
    });

    private void VideoPlayer_TimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e) =>
        DispatchIfOpen(() => UpdateTimeline(e.Time, _videoPlayer?.Length ?? 0));

    private void VideoPlayer_LengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e) =>
        DispatchIfOpen(() => UpdateTimeline(_videoPlayer?.Time ?? 0, e.Length));

    private void VideoPlayer_EndReached(object? sender, EventArgs e) => DispatchIfOpen(() =>
    {
        _isPlaying = false;
        UpdatePlaybackIcon();
        ShowControls();
    });

    private void VideoPlayer_EncounteredError(object? sender, EventArgs e) => DispatchIfOpen(ShowVideoError);

    private void DispatchIfOpen(Action action)
    {
        if (_isClosing || Dispatcher.HasShutdownStarted) return;
        Dispatcher.BeginInvoke(() => { if (!_isClosing) action(); });
    }

    private void UpdateTimeline(long current, long total)
    {
        _updatingTimeline = true;
        TimelineSlider.Maximum = Math.Max(total, 1);
        TimelineSlider.Value = Math.Clamp(current, 0, Math.Max(total, 1));
        _updatingTimeline = false;
        TimeText.Text = $"{FormatTime(current)} / {FormatTime(total)}";
    }

    private static string FormatTime(long milliseconds)
    {
        var time = TimeSpan.FromMilliseconds(Math.Max(milliseconds, 0));
        return time.TotalHours >= 1 ? time.ToString(@"h\:mm\:ss") : time.ToString(@"mm\:ss");
    }

    private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_controlsReady || _updatingTimeline || _videoPlayer is null) return;
        _videoPlayer.Time = (long)e.NewValue;
        RestartControlsTimer();
    }

    private void TimelineSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_videoPlayer is null || TimelineSlider.ActualWidth <= 0 || IsInsideThumb(e.OriginalSource as DependencyObject)) return;
        var ratio = Math.Clamp(e.GetPosition(TimelineSlider).X / TimelineSlider.ActualWidth, 0, 1);
        TimelineSlider.Value = ratio * TimelineSlider.Maximum;
        _videoPlayer.Time = (long)TimelineSlider.Value;
        RestartControlsTimer();
        e.Handled = true;
    }

    private static bool IsInsideThumb(DependencyObject? element)
    {
        while (element is not null)
        {
            if (element is System.Windows.Controls.Primitives.Thumb) return true;
            element = System.Windows.Media.VisualTreeHelper.GetParent(element);
        }
        return false;
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_controlsReady || _videoPlayer is null) return;
        var volume = Math.Clamp((int)Math.Round(e.NewValue), 0, 100);
        _videoPlayer.Volume = volume;
        _settings.VideoVolume = volume;
        if (volume == 0)
        {
            _isMuted = true;
            _videoPlayer.Mute = true;
            _settings.VideoMuted = true;
        }
        else
        {
            _lastAudibleVolume = volume;
            if (_isMuted)
            {
                _isMuted = false;
                _videoPlayer.Mute = false;
                _settings.VideoMuted = false;
            }
        }
        UpdateMuteLabel();
        ScheduleSettingsSave();
    }

    private void Mute_Click(object sender, RoutedEventArgs e)
    {
        if (_videoPlayer is null) return;
        if (_isMuted)
        {
            if (VolumeSlider.Value <= 0) VolumeSlider.Value = _lastAudibleVolume;
            _isMuted = false;
        }
        else
        {
            _isMuted = true;
        }
        _videoPlayer.Mute = _isMuted;
        _settings.VideoMuted = _isMuted;
        UpdateMuteLabel();
        ScheduleSettingsSave();
        RestartControlsTimer();
    }

    private void UpdateMuteLabel()
    {
        var volume = (int)Math.Round(VolumeSlider.Value);
        VolumeIcon.Text = _isMuted || volume == 0 ? "\uE74F"
            : volume <= 33 ? "\uE993"
            : volume <= 66 ? "\uE994"
            : "\uE995";
        MuteButton.ToolTip = _isMuted ? "Ativar áudio" : "Silenciar áudio";
    }

    private void ScheduleSettingsSave()
    {
        _settingsSaveTimer.Stop();
        _settingsSaveTimer.Start();
    }

    private void SettingsSaveTimer_Tick(object? sender, EventArgs e)
    {
        _settingsSaveTimer.Stop();
        _settings.Save();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MaximizeRestore_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (MaximizeIcon is null || RestoreIcon is null) return;
        var maximized = WindowState == WindowState.Maximized;
        MaximizeIcon.Visibility = maximized ? Visibility.Collapsed : Visibility.Visible;
        RestoreIcon.Visibility = maximized ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            MaximizeRestore_Click(sender, new RoutedEventArgs());
        else if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void PlayPause_Click(object sender, RoutedEventArgs e) => TogglePlayback();

    private void TogglePlayback()
    {
        if (_videoPlayer is null) return;
        _videoPlayer.SetPause(_isPlaying);
        _isPlaying = !_isPlaying;
        UpdatePlaybackIcon();
        if (_isPlaying) RestartControlsTimer(); else ShowControls();
    }

    private void UpdatePlaybackIcon()
    {
        PlayIcon.Visibility = _isPlaying ? Visibility.Collapsed : Visibility.Visible;
        PauseIcon.Visibility = _isPlaying ? Visibility.Visible : Visibility.Collapsed;
        PlayPauseButton.ToolTip = _isPlaying ? "Pausar" : "Reproduzir";
    }

    private void ShowVideoError()
    {
        if (!_isVideo) return;
        LargeVideo.Visibility = Visibility.Collapsed;
        VideoControlsPanel.Visibility = Visibility.Collapsed;
        MuteButton.Visibility = Visibility.Collapsed;
        VolumeSlider.Visibility = Visibility.Collapsed;
        ErrorText.Text = "Não foi possível reproduzir este vídeo.";
        ErrorText.Visibility = Visibility.Visible;
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isVideo) return;
        ShowControls();
        if (_isPlaying) RestartControlsTimer();
    }

    private void RestartControlsTimer()
    {
        AnimateControls(1, true, 140);
        _controlsTimer.Stop();
        _controlsTimer.Start();
    }

    private void ShowControls()
    {
        _controlsTimer.Stop();
        AnimateControls(1, true, 140);
    }

    private void ControlsTimer_Tick(object? sender, EventArgs e)
    {
        _controlsTimer.Stop();
        if (!_isPlaying) return;
        AnimateControls(0, false, 240);
    }

    private void AnimateControls(double opacity, bool hitTestVisible, int milliseconds)
    {
        ControlsBar.IsHitTestVisible = hitTestVisible;
        ControlsBar.BeginAnimation(OpacityProperty, new DoubleAnimation(opacity, TimeSpan.FromMilliseconds(milliseconds)));
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { Close(); return; }
        if (_videoPlayer is null) return;
        switch (e.Key)
        {
            case Key.Space: TogglePlayback(); break;
            case Key.Left: _videoPlayer.Time = Math.Max(0, _videoPlayer.Time - 5000); break;
            case Key.Right: _videoPlayer.Time = Math.Min(_videoPlayer.Length, _videoPlayer.Time + 5000); break;
            case Key.M: Mute_Click(this, new RoutedEventArgs()); break;
            case Key.Up: VolumeSlider.Value = Math.Min(100, VolumeSlider.Value + 5); break;
            case Key.Down: VolumeSlider.Value = Math.Max(0, VolumeSlider.Value - 5); break;
            default: return;
        }
        e.Handled = true;
        ShowControls();
    }

    protected override void OnClosed(EventArgs e)
    {
        _isClosing = true;
        _controlsTimer.Stop();
        _settingsSaveTimer.Stop();
        _settings.Save();
        if (_videoPlayer is not null)
        {
            _videoPlayer.Playing -= VideoPlayer_Playing;
            _videoPlayer.Paused -= VideoPlayer_Paused;
            _videoPlayer.TimeChanged -= VideoPlayer_TimeChanged;
            _videoPlayer.LengthChanged -= VideoPlayer_LengthChanged;
            _videoPlayer.EndReached -= VideoPlayer_EndReached;
            _videoPlayer.EncounteredError -= VideoPlayer_EncounteredError;
            LargeVideo.MediaPlayer = null;
            _videoPlayer.Stop();
            _videoMedia?.Dispose();
            _videoMedia = null;
            _videoPlayer.Dispose();
        }
        base.OnClosed(e);
    }
}
