using System.Collections.Generic;
using ClassIsland.Core.Abstractions.Controls;
using EntertainingIsland.Models;

namespace EntertainingIsland.Views.NotificationProviders;

public partial class AlertHotkeySettingsControl : NotificationProviderControlBase<AlertHotkeySettings>
{
    public static List<string> KeyOptions { get; } = new()
    {
        "A","B","C","D","E","F","G","H","I","J","K","L","M",
        "N","O","P","Q","R","S","T","U","V","W","X","Y","Z",
        "0","1","2","3","4","5","6","7","8","9",
        "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12"
    };

    public AlertHotkeySettingsControl()
    {
        InitializeComponent();
        DataContext = this;
    }
}
