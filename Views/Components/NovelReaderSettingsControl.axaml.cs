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
