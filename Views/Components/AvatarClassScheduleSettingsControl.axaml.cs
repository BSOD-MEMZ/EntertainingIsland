using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Shared;
using EntertainingIsland.Models;

namespace EntertainingIsland.Views.Components;

public partial class AvatarClassScheduleSettingsControl : ComponentBase<AvatarClassScheduleSettings>
{
    public ObservableCollection<SubjectAvatarRow> SubjectAvatarRows { get; } = new();

    public AvatarClassScheduleSettingsControl()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // 延迟加载：此时 IProfileService 等核心服务已就绪
        LoadSubjectRows();
    }

    private void LoadSubjectRows()
    {
        SubjectAvatarRows.Clear();

        try
        {
            var profileService = IAppHost.TryGetService<ClassIsland.Core.Abstractions.Services.IProfileService>();
            var subjects = profileService?.Profile?.Subjects;
            if (subjects == null) return;

            foreach (var kv in subjects)
            {
                var subject = kv.Value;
                var path = Settings.SubjectAvatarMap.GetValueOrDefault(subject.Name, "");

                IImage? preview = null;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    try { preview = new Bitmap(path); } catch { }
                }

                SubjectAvatarRows.Add(new SubjectAvatarRow
                {
                    SubjectName = subject.Name,
                    SubjectInitial = subject.Initial,
                    AvatarPath = path,
                    AvatarPreview = preview,
                    HasAvatar = !string.IsNullOrEmpty(path)
                });
            }
        }
        catch { /* 忽略 */ }
    }

    private async void OnBrowseAvatar_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not SubjectAvatarRow row) return;

        var window = TopLevel.GetTopLevel(this) ?? AppBase.Current.GetRootWindow();
        if (window == null) return;

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = $"选择「{row.SubjectName}」的头像",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("图片文件")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp"]
                }
            ]
        });

        if (files.Count == 0) return;

        var path = files[0].Path.LocalPath;
        row.AvatarPath = path;
        row.HasAvatar = true;

        try { row.AvatarPreview = new Bitmap(path); } catch { row.HasAvatar = false; }

        // 更新映射
        Settings.SubjectAvatarMap[row.SubjectName] = path;
        // 手动触发保存（替换整个字典引用以触发属性变更）
        var map = Settings.SubjectAvatarMap;
        Settings.SubjectAvatarMap = new Dictionary<string, string>(map);
    }

    private void OnClearAvatar_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not SubjectAvatarRow row) return;

        row.AvatarPath = "";
        row.AvatarPreview = null;
        row.HasAvatar = false;

        Settings.SubjectAvatarMap.Remove(row.SubjectName);
        var map = Settings.SubjectAvatarMap;
        Settings.SubjectAvatarMap = new Dictionary<string, string>(map);
    }
}

/// <summary>
/// 科目头像映射行视图模型
/// </summary>
public class SubjectAvatarRow : INotifyPropertyChanged
{
    private string _avatarPath = "";
    private IImage? _avatarPreview;
    private bool _hasAvatar;

    public string SubjectName { get; set; } = "";
    public string SubjectInitial { get; set; } = "";

    public string AvatarPath
    {
        get => _avatarPath;
        set { _avatarPath = value; OnPropertyChanged(); }
    }

    public IImage? AvatarPreview
    {
        get => _avatarPreview;
        set { _avatarPreview = value; OnPropertyChanged(); }
    }

    public bool HasAvatar
    {
        get => _hasAvatar;
        set { _hasAvatar = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
