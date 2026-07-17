using Avalonia.Controls;
using Avalonia.Interactivity;
using EntertainingIsland.ViewModels;

namespace EntertainingIsland.Views.WelcomePages;

public partial class FeaturesPage : UserControl, IWelcomePage
{
    public WelcomeViewModel ViewModel { get; set; } = null!;

    public FeaturesPage()
    {
        InitializeComponent();
    }

    private void ButtonNext_OnClick(object? sender, RoutedEventArgs e) =>
        WelcomeWindow.FromControl(this)?.NavigateForward();

    private void ButtonBack_OnClick(object? sender, RoutedEventArgs e) =>
        WelcomeWindow.FromControl(this)?.NavigateBack();
}
