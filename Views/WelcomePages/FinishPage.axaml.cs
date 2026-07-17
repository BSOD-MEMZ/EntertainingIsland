using Avalonia.Controls;
using Avalonia.Interactivity;
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
    }

    private void ButtonFinish_OnClick(object? sender, RoutedEventArgs e)
    {
        WelcomeWindow.FromControl(this)?.FinishWizard();
    }
}
