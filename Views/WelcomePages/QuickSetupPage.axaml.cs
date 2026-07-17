using Avalonia.Controls;
using Avalonia.Interactivity;
using EntertainingIsland.ViewModels;

namespace EntertainingIsland.Views.WelcomePages;

public partial class QuickSetupPage : UserControl, IWelcomePage
{
    private WelcomeViewModel _vm = null!;
    public WelcomeViewModel ViewModel
    {
        get => _vm;
        set { _vm = value; DataContext = value; }
    }

    public QuickSetupPage()
    {
        InitializeComponent();
    }

    private void ButtonNext_OnClick(object? sender, RoutedEventArgs e) =>
        WelcomeWindow.FromControl(this)?.NavigateForward();

    private void ButtonBack_OnClick(object? sender, RoutedEventArgs e) =>
        WelcomeWindow.FromControl(this)?.NavigateBack();
}
