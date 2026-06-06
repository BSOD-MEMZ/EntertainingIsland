using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Shared;
using EntertainingIsland.Models;
using EntertainingIsland.Services;

namespace EntertainingIsland.Views.Components;

public partial class NovelReaderSettingsControl : ComponentBase<NovelReaderSettings>
{
    public ObservableCollection<string> KeyOptions { get; } = new()
    {
        "A","B","C","D","E","F","G","H","I","J","K","L","M",
        "N","O","P","Q","R","S","T","U","V","W","X","Y","Z",
        "0","1","2","3","4","5","6","7","8","9",
        "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12",
        "Left","Right","Up","Down",
        "Space","Enter","Tab","Escape"
    };
    public NovelReaderSettingsControl()
    {
        InitializeComponent();
        BrowseBtn.Click += async (_, _) =>
        {
            var window = TopLevel.GetTopLevel(this) ?? AppBase.Current.GetRootWindow();
            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择小说文件",
                AllowMultiple = false
            });
            if (files.Count > 0)
                Settings.NovelFilePath = files[0].Path.LocalPath;
        };

        RestartBtn.Click += (_, _) =>
        {
            // 通过 EntertainmentState 发送重新开始信号
            var state = IAppHost.TryGetService<EntertainmentState>();
            if (state != null)
            {
                // 用 NovelRestartRequest 标记触发重新开始
                state.NovelRestartRequest = !state.NovelRestartRequest;
            }
        };
    }
}
