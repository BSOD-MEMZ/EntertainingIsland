using System.Windows.Input;
using ClassIsland.Core.Abstractions.Controls;
using CommunityToolkit.Mvvm.Input;
using EntertainingIsland.Models;
using EntertainingIsland.Services;

namespace EntertainingIsland.Views.NotificationProviders;

public partial class ClassEndingReminderSettingsControl : NotificationProviderControlBase<ClassEndingReminderSettings>
{
    public ICommand DebugPreviewCommand { get; }

    public ClassEndingReminderSettingsControl()
    {
        InitializeComponent();
        DataContext = this;

        DebugPreviewCommand = new RelayCommand(() =>
        {
            ClassEndingReminderService.Instance?.DebugTriggerPreview();
        });
    }
}
