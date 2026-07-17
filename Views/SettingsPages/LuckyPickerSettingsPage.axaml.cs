using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Shared;
using EntertainingIsland.Models;

namespace EntertainingIsland.Views.SettingsPages;

[SettingsPageInfo(
    "entertainingisland.luckypicker",
    "点名器",
    "\uE716",
    "\uE716",
    SettingsPageCategory.External
)]
public partial class LuckyPickerSettingsPage : SettingsPageBase
{
    private Plugin PluginEntry => IAppHost.GetService<Plugin>();
    public Settings Settings => PluginEntry.Settings;
    private bool _riggedPanelVisible;

    public LuckyPickerSettingsPage()
    {
        InitializeComponent();
        DataContext = this;

        // 名单导入按钮
        ImportBtn.Click += async (_, _) =>
        {
            var window = TopLevel.GetTopLevel(this) ?? AppBase.Current.GetRootWindow();
            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择名单文件（.txt）",
                AllowMultiple = false
            });
            if (files.Count > 0)
            {
                try
                {
                    var text = await File.ReadAllTextAsync(files[0].Path.LocalPath);
                    Settings.LuckyPicker.NameListText = text;
                }
                catch (Exception ex)
                {
                    NameCountLabel.Text = $"❌ 导入失败: {ex.Message}";
                }
            }
        };

        // 监听名单变化，更新计数和爆率列表
        Settings.LuckyPicker.PropertyChanged += (_, a) =>
        {
            if (a.PropertyName == nameof(LuckyPickerSettings.NameListText))
            {
                UpdateNameCount();
                RebuildRiggedList();
            }
        };

        UpdateNameCount();
        RebuildRiggedList();

        // 监听键盘事件：Ctrl+Shift+Alt+C 切换隐藏面板
        KeyDown += OnPageKeyDown;
    }

    private void OnPageKeyDown(object? sender, KeyEventArgs e)
    {
        var modifiers = e.KeyModifiers;
        bool isCtrl = (modifiers & KeyModifiers.Control) != 0;
        bool isShift = (modifiers & KeyModifiers.Shift) != 0;
        bool isAlt = (modifiers & KeyModifiers.Alt) != 0;

        if (isCtrl && isShift && isAlt && e.Key == Key.C)
        {
            _riggedPanelVisible = !_riggedPanelVisible;
            RiggedPanel.IsVisible = _riggedPanelVisible;
            e.Handled = true;
        }
    }

    private void UpdateNameCount()
    {
        var text = Settings.LuckyPicker.NameListText ?? "";
        var names = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(n => n.Trim())
                        .Where(n => n.Length > 0)
                        .ToList();
        NameCountLabel.Text = $"当前名单共 {names.Count} 人";
    }

    /// <summary>
    /// 根据名单重建爆率列表 UI
    /// </summary>
    private void RebuildRiggedList()
    {
        var panel = new StackPanel { Spacing = 6 };
        var entries = Settings.LuckyPicker.RiggedEntries;

        // 先解析当前名单
        var text = Settings.LuckyPicker.NameListText ?? "";
        var names = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(n => n.Trim())
                        .Where(n => n.Length > 0)
                        .ToList();

        // 同步爆率条目
        foreach (var name in names)
        {
            if (!entries.Any(e => e.Name == name))
                entries.Add(new RiggedNameEntry { Name = name, Weight = 1.0 });
        }
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            if (!names.Contains(entries[i].Name))
                entries.RemoveAt(i);
        }

        // 为每个人构建一行
        foreach (var entry in entries)
        {
            var row = new StackPanel { Spacing = 4, Margin = new Avalonia.Thickness(0, 2) };

            var header = new TextBlock
            {
                Text = $"{entry.Name}",
                FontSize = 13,
                FontWeight = Avalonia.Media.FontWeight.Bold
            };
            row.Children.Add(header);

            // 权重滑块
            var weightRow = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };
            weightRow.Children.Add(new TextBlock { Text = "权重:", FontSize = 12, Opacity = 0.6, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Width = 45 });
            var weightSlider = new Slider
            {
                Minimum = 0.1, Maximum = 10.0, Width = 130,
                Value = entry.Weight
            };
            var weightLabel = new TextBlock { Width = 40, FontSize = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            weightSlider.PropertyChanged += (s, a) =>
            {
                if (a.Property.Name == nameof(Slider.Value))
                {
                    entry.Weight = Math.Round(weightSlider.Value, 1);
                    weightLabel.Text = entry.Weight.ToString("F1");
                }
            };
            weightLabel.Text = entry.Weight.ToString("F1");
            weightRow.Children.Add(weightSlider);
            weightRow.Children.Add(weightLabel);
            row.Children.Add(weightRow);

            // 保底次数
            var pityRow = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };
            pityRow.Children.Add(new TextBlock { Text = "保底:", FontSize = 12, Opacity = 0.6, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Width = 45 });
            var pitySlider = new Slider
            {
                Minimum = 0, Maximum = 50, Width = 130,
                Value = entry.PityThreshold
            };
            var pityLabel = new TextBlock { Width = 50, FontSize = 12, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            pitySlider.PropertyChanged += (s, a) =>
            {
                if (a.Property.Name == nameof(Slider.Value))
                {
                    entry.PityThreshold = (int)Math.Round(pitySlider.Value);
                    pityLabel.Text = entry.PityThreshold == 0 ? "不保底" : $"每 {entry.PityThreshold} 次必中";
                }
            };
            pityLabel.Text = entry.PityThreshold == 0 ? "不保底" : $"每 {entry.PityThreshold} 次必中";
            pityRow.Children.Add(pitySlider);
            pityRow.Children.Add(pityLabel);
            row.Children.Add(pityRow);

            // 当前连续未中
            if (entry.CurrentMissStreak > 0)
            {
                row.Children.Add(new TextBlock
                {
                    Text = $"  当前连续 {entry.CurrentMissStreak} 次未中",
                    FontSize = 11, Opacity = 0.4
                });
            }

            panel.Children.Add(row);
        }

        RiggedListPanel.Children.Clear();
        RiggedListPanel.Children.Add(panel);
    }
}
