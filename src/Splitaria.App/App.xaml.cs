using System.Windows;

namespace Splitaria.App;

public partial class App : Application
{
    public AppSettings Settings { get; } = AppSettings.Load();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ThemeManager.Apply(Settings.Theme);
        ThemeManager.StartFollowingSystem(Settings);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var splash = new SplashWindow();
        splash.Show();

        // Permite que a tela seja desenhada antes do carregamento das bibliotecas nativas do VLC.
        await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);

        try
        {
            var initializeVideo = Task.Run(() => _ = VideoEngine.Shared);
            var minimumPresentationTime = Task.Delay(TimeSpan.FromSeconds(2));
            await Task.WhenAll(initializeVideo, minimumPresentationTime);
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainWindow.Show();
            splash.Close();
        }
        catch (Exception ex)
        {
            splash.Close();
            MessageBox.Show(
                $"Não foi possível iniciar o Splitaria.\n\n{ex.Message}",
                "Falha na inicialização", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ThemeManager.StopFollowingSystem();
        base.OnExit(e);
    }
}
