using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using EntertainingIsland.Models;

namespace EntertainingIsland.Services;

/// <summary>
/// 公平点名器核心服务。对用户声称"完全公平"。
/// </summary>
public partial class LuckyPickerService : ObservableObject
{
    private readonly LuckyPickerSettings _settings;
    private readonly Random _rng = new();

    [ObservableProperty]
    private List<string> _names = new();

    [ObservableProperty]
    private string _lastPicked = "";

    /// <summary>点名历史（最近一次被抽中的人的索引）</summary>
    public int LastPickedIndex { get; private set; } = -1;

    public LuckyPickerService(LuckyPickerSettings settings)
    {
        _settings = settings;
        ParseNames();
        _settings.PropertyChanged += (_, a) =>
        {
            if (a.PropertyName == nameof(LuckyPickerSettings.NameListText))
                ParseNames();
        };
    }

    /// <summary>
    /// 解析名单元文本为列表
    /// </summary>
    public void ParseNames()
    {
        var text = _settings.NameListText ?? "";
        Names = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => n.Trim())
                    .Where(n => n.Length > 0)
                    .ToList();

        // 同步爆率条目：新增 / 移除
        var existingNames = new HashSet<string>(_settings.RiggedEntries.Select(e => e.Name));
        // 移除已不在名单中的条目
        for (int i = _settings.RiggedEntries.Count - 1; i >= 0; i--)
        {
            if (!Names.Contains(_settings.RiggedEntries[i].Name))
                _settings.RiggedEntries.RemoveAt(i);
        }
        // 新增名单中有但爆率列表中没有的条目
        foreach (var name in Names)
        {
            if (!existingNames.Contains(name))
                _settings.RiggedEntries.Add(new RiggedNameEntry { Name = name, Weight = 1.0 });
        }
    }

    /// <summary>
    /// 随机抽取一个姓名。
    /// 如果启用了爆率模式，使用加权+保底算法；否则纯随机。
    /// </summary>
    public string Pick()
    {
        if (Names.Count == 0)
            return "(名单为空)";

        int pickedIndex;

        if (_settings.RiggedModeEnabled && _settings.RiggedEntries.Count > 0)
        {
            pickedIndex = PickRigged();
        }
        else
        {
            pickedIndex = _rng.Next(Names.Count);
        }

        LastPickedIndex = pickedIndex;
        LastPicked = Names[pickedIndex];

        if (_settings.RiggedModeEnabled)
            UpdateStreaks(pickedIndex);

        return LastPicked;
    }

    /// <summary>
    /// 加权随机 + 保底
    /// </summary>
    private int PickRigged()
    {
        var entries = _settings.RiggedEntries;

        // 1. 先检查保底：有谁 missStreak >= pityThreshold（且 pityThreshold > 0）
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e.PityThreshold > 0 && e.CurrentMissStreak >= e.PityThreshold)
            {
                Logger.Info($"[点名器-保底触发] \"{e.Name}\" 连续 {e.CurrentMissStreak} 次未中，保底触发");
                return i;
            }
        }

        // 2. 加权随机
        double totalWeight = 0;
        foreach (var e in entries)
            totalWeight += Math.Max(0.01, e.Weight); // 最低 0.01 防止零权重

        double roll = _rng.NextDouble() * totalWeight;
        double cumulative = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            cumulative += Math.Max(0.01, entries[i].Weight);
            if (roll <= cumulative)
                return i;
        }

        return entries.Count - 1; // fallback
    }

    /// <summary>
    /// 更新未抽中计数器
    /// </summary>
    private void UpdateStreaks(int pickedIndex)
    {
        for (int i = 0; i < _settings.RiggedEntries.Count; i++)
        {
            if (i == pickedIndex)
                _settings.RiggedEntries[i].CurrentMissStreak = 0;
            else
                _settings.RiggedEntries[i].CurrentMissStreak++;
        }
    }

    /// <summary>
    /// 获取显示文本（"公正点名"风格）
    /// </summary>
    public string GetDisplayText()
    {
        if (Names.Count == 0)
            return "请先在设置中导入名单";
        if (!string.IsNullOrEmpty(LastPicked))
            return $"{LastPicked}";
        return $"点名 · {Names.Count} 人";
    }

    /// <summary>
    /// 随机抽取两人（不重复）。
    /// </summary>
    /// <returns>格式如 "张三、李四"；名单不足时返回提示文本。</returns>
    public string PickTwo()
    {
        if (Names.Count < 2)
            return "(名单不足两人)";

        int firstIndex, secondIndex;

        if (_settings.RiggedModeEnabled && _settings.RiggedEntries.Count > 0)
        {
            firstIndex = PickRigged();
            // 临时降低已抽中者的权重为 0，确保不会重复抽到
            var savedWeight = _settings.RiggedEntries[firstIndex].Weight;
            _settings.RiggedEntries[firstIndex].Weight = 0;
            try
            {
                secondIndex = PickRigged();
            }
            finally
            {
                _settings.RiggedEntries[firstIndex].Weight = savedWeight;
            }
        }
        else
        {
            firstIndex = _rng.Next(Names.Count);
            do { secondIndex = _rng.Next(Names.Count); } while (secondIndex == firstIndex);
        }

        var result = $"{Names[firstIndex]}、{Names[secondIndex]}";
        LastPickedIndex = firstIndex;
        LastPicked = result;

        if (_settings.RiggedModeEnabled)
        {
            UpdateStreaks(firstIndex);
            UpdateStreaks(secondIndex);
        }

        return result;
    }
}
