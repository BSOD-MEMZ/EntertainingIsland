# Video Player Component Design

## Summary

Add a video player component to the EntertainingIsland plugin, allowing video playback directly on the ClassIsland main window. Pure video display (no border), auto-plays on visibility, controlled via global hotkeys.

## Playback Engine

**LibVLCSharp** — the most robust and format-compatible video library for Avalonia.

- NuGet: `LibVLCSharp.Avalonia` + `VideoLAN.LibVLC.Windows`
- Supports: mp4, mkv, avi, wmv, flv, webm, mov, m4v, etc.
- GPU hardware decoding (Direct3D11)
- Native libs bundled via VideoLAN.LibVLC.Windows (~40MB)

## Architecture

```
Models/VideoPlayerSettings.cs                 -- settings POCO
Views/Components/VideoPlayerComponent.axaml    -- pure VideoView, no border
Views/Components/VideoPlayerComponent.axaml.cs -- playback logic + hotkeys
Views/Components/VideoPlayerSettingsControl.axaml  -- settings UI
Plugin.cs — registration line
```

## Playlist Logic (folder + manual hybrid)

1. Scan files in configured folder (sorted by name), filtered by video extensions
2. Append manually-added file paths from settings
3. Deduplicate (by full path) → final playlist
4. On component attach: build playlist, start playing from index 0
5. On video end: auto-advance to next; if at end, loop to start (if loop enabled) or stop (if not)
6. When hidden/re-shown: resume from last position

## Hotkeys

| Action | Default binding |
|--------|----------------|
| Previous video | Ctrl+Shift+Up |
| Next video | Ctrl+Shift+Down |
| Pause / Resume | Ctrl+Shift+Space |

Registered via Win32 `RegisterHotKey` + `WndProc` hook on the main window handle, same pattern as `RssComponent`. Hotkeys only active when the component is attached to visual tree.

## Settings

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `VideoFolderPath` | `string` | `""` | Folder to scan for video files |
| `ManualFilePaths` | `ObservableCollection<string>` | `[]` | Manually-added individual file paths |
| `Volume` | `int` | `80` | Playback volume (0–100) |
| `Loop` | `bool` | `true` | Loop playlist on completion |
| `Shuffle` | `bool` | `false` | Shuffle playlist order |
| `PlaybackSpeed` | `double` | `1.0` | Playback rate (0.5–2.0) |

## Component Layout

```xml
<ci:ComponentBase x:Class="EntertainingIsland.Views.Components.VideoPlayerComponent"
                  x:TypeArguments="models:VideoPlayerSettings">
    <!-- Pure video view, no border or background -->
    <vlc:VideoView x:Name="VideoView" />
</ci:ComponentBase>
```

No borders, no overlays, no controls overlay — pure video. The parent `ComponentBase` handles the container framing.

## Registration

In `Plugin.cs` `Initialize()`:
```csharp
services.AddComponent<VideoPlayerComponent, VideoPlayerSettingsControl>();
```

## Error Handling

- Missing VLC native libs → show placeholder text "VLC 组件未安装，请检查插件完整性"
- File not found → skip to next in playlist
- Empty playlist → show placeholder "未找到视频文件，请在设置中添加"
- Playback error → skip to next video after 3 seconds timeout

## Dependencies

- `LibVLCSharp.Avalonia` — Avalonia VideoView control
- `VideoLAN.LibVLC.Windows` — native VLC libraries for Windows
