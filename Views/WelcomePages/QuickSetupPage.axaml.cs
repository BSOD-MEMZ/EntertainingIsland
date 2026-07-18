using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using EntertainingIsland.ViewModels;
using FluentAvalonia.UI.Controls;

namespace EntertainingIsland.Views.WelcomePages;

public partial class QuickSetupPage : UserControl, IWelcomePage
{
    private WelcomeViewModel _vm = null!;
    public WelcomeViewModel ViewModel
    {
        get => _vm;
        set { _vm = value; DataContext = value; }
    }

    public static List<string> KeyOptions { get; } = new()
    {
        "A","B","C","D","E","F","G","H","I","J","K","L","M",
        "N","O","P","Q","R","S","T","U","V","W","X","Y","Z",
        "0","1","2","3","4","5","6","7","8","9",
        "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12"
    };

    public QuickSetupPage()
    {
        InitializeComponent();
        DataContext = this;
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // 确保 ViewModel 绑定已就绪
        if (_vm == null) return;

        // 卡片入场顺序
        var cards = new SettingsExpander[]
        {
            CardAlertHotkey, CardNovelReader, CardFortune,
            CardCatchphrase, CardSports, CardDismissKey,
        };

        // 逐个显示卡片（入场动画）
        foreach (var card in cards)
        {
            await Task.Delay(80);
            card.Classes.Add("visible");
        }
    }

    private void ButtonNext_OnClick(object? sender, RoutedEventArgs e) =>
        WelcomeWindow.FromControl(this)?.NavigateForward();

    private void ButtonBack_OnClick(object? sender, RoutedEventArgs e) =>
        WelcomeWindow.FromControl(this)?.NavigateBack();
}
