using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using ClassIsland.Shared;
using EntertainingIsland.ViewModels;

namespace EntertainingIsland.Views.WelcomePages;

public partial class FinishPage : UserControl, IWelcomePage
{
    public WelcomeViewModel ViewModel { get; set; } = null!;

    public FinishPage()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        Root.Classes.Add("anim");
        try
        {
            var plugin = IAppHost.TryGetService<Plugin>();
            if (plugin != null)
            {
                var p = Path.Combine(plugin.Info.PluginFolderPath, "banner.png");
                if (File.Exists(p)) BannerImage.Source = new Bitmap(p);
            }
        }
        catch { }
    }

    private void ButtonFinish_OnClick(object? sender, RoutedEventArgs e)
    {
        WelcomeWindow.FromControl(this)?.FinishWizard();
    }
}
