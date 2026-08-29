using System.Windows;
using System.Reflection;

namespace Splitaria.App;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version();
        VersionText.Text = version.ToString(3);
    }
}
