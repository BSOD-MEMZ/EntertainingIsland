using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Controls;
using ClassIsland.Shared;
using EntertainingIsland.ViewModels;
using EntertainingIsland.Views.WelcomePages;
using FluentAvalonia.UI.Controls;

namespace EntertainingIsland.Views;

public partial class WelcomeWindow : MyWindow, INavigationPageFactory
{
    public WelcomeViewModel ViewModel { get; } = new();

    private readonly List<Type> _pages =
    [
        typeof(SoundChoicePage),
        typeof(IntroPage),
        typeof(FeaturesPage),
        typeof(QuickSetupPage),
        typeof(FinishPage),
    ];

    private readonly Dictionary<Type, object?> _pageCache = new();

    public WelcomeWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    public Control? GetPage(Type srcType)
    {
        if (_pageCache.TryGetValue(srcType, out var v) && v is Control control)
            return control;

        var page = Activator.CreateInstance(srcType);
        if (page is IWelcomePage welcomePage)
            welcomePage.ViewModel = ViewModel;

        ViewModel.CurrentPage = srcType;
        _pageCache[srcType] = page;
        return page as Control;
    }

    public Control? GetPageFromObject(object target) => null;

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        MainFrame.Navigate(_pages[0]);
        ViewModel.CurrentPage = _pages[0];
    }

    public void NavigateForward()
    {
        var current = ViewModel.CurrentPage ?? _pages[0];
        var index = Math.Min(_pages.IndexOf(current) + 1, _pages.Count - 1);
        MainFrame.Navigate(_pages[index]);
        ViewModel.CurrentPage = _pages[index];
    }

    public void NavigateBack()
    {
        var current = ViewModel.CurrentPage ?? _pages[0];
        var index = Math.Max(_pages.IndexOf(current) - 1, 0);
        MainFrame.Navigate(_pages[index]);
        ViewModel.CurrentPage = _pages[index];
    }

    public void FinishWizard()
    {
        var plugin = IAppHost.TryGetService<Plugin>();
        if (plugin != null)
        {
            var s = plugin.Settings;
            var ft = s.FeatureToggles;

            s.DismissHotkey.Ctrl = ViewModel.DismissKeyCtrl;
            s.DismissHotkey.Shift = ViewModel.DismissKeyShift;
            s.DismissHotkey.Alt = ViewModel.DismissKeyAlt;
            s.DismissHotkey.Key = ViewModel.DismissKeyKey;

            ft.AlertHotkey = ViewModel.EnableAlertHotkey;
            ft.NovelReader = ViewModel.EnableNovelReader;
            ft.Fortune = ViewModel.EnableFortune;
            ft.Catchphrase = ViewModel.EnableCatchphrase;
            ft.Sports = ViewModel.EnableSports;

            s.HasSeenWelcome = true;
            var configPath = Path.Combine(plugin.PluginConfigFolder, "Settings.json");
            ClassIsland.Shared.Helpers.ConfigureFileHelper.SaveConfig(configPath, s);
        }

        ViewModel.IsWizardCompleted = true;
        Close();
    }

    public static WelcomeWindow? FromControl(Control c) =>
        TopLevel.GetTopLevel(c) as WelcomeWindow;
}
