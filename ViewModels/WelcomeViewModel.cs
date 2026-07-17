using CommunityToolkit.Mvvm.ComponentModel;

namespace EntertainingIsland.ViewModels;

public partial class WelcomeViewModel : ObservableObject
{
    [ObservableProperty]
    private int _slideIndex = 0;

    [ObservableProperty]
    private Type? _currentPage;

    [ObservableProperty]
    private bool _enableAlertHotkey = true;

    [ObservableProperty]
    private bool _enableNovelReader = true;

    [ObservableProperty]
    private bool _enableFortune = true;

    [ObservableProperty]
    private bool _enableCatchphrase = true;

    [ObservableProperty]
    private bool _enableSports = true;

    // ===== 安全键 =====
    [ObservableProperty]
    private bool _dismissKeyCtrl = true;

    [ObservableProperty]
    private bool _dismissKeyShift = true;

    [ObservableProperty]
    private bool _dismissKeyAlt = false;

    [ObservableProperty]
    private string _dismissKeyKey = "K";

    [ObservableProperty]
    private bool _isWizardCompleted = false;
}
