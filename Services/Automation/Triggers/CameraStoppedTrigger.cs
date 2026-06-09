using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using EntertainingIsland.Services;

namespace EntertainingIsland.Services.Automation.Triggers;

[TriggerInfo("entertainingisland.camera.stopped", "摄像头被关闭时", "\uE392")]
public class CameraStoppedTrigger : TriggerBase
{
    private CameraMonitorService? _service;

    public override void Loaded()
    {
        _service = IAppHost.TryGetService<CameraMonitorService>();
        if (_service != null)
            _service.PropertyChanged += OnCameraStatusChanged;
    }

    public override void UnLoaded()
    {
        if (_service != null)
            _service.PropertyChanged -= OnCameraStatusChanged;
    }

    private void OnCameraStatusChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CameraMonitorService.IsCameraInUse) && _service?.IsCameraInUse == false)
            Trigger();
    }
}
