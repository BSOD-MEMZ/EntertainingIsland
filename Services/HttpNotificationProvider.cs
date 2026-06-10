using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EntertainingIsland.Services;

/// <summary>
/// HTTP 通知提供程序。轻量封装，延迟绑定 NotificationHostService。
/// </summary>
[NotificationProviderInfo(
    "8DB4B182-8D47-4A11-B6FE-099D080D0ABA",
    "HTTP 通知",
    "\uE025"
)]
public class HttpNotificationProvider : NotificationProviderBase
{
    public HttpNotificationProvider() : base(false)  // 不自动注册，手动在 AppStarted 中注册
    {
    }

    /// <summary>
    /// 安全初始化——等待 AppStarted 后再注册到通知主机。
    /// </summary>
    public void SafeInitialize()
    {
        var host = IAppHost.TryGetService<INotificationHostService>();
        if (host == null)
        {
            Console.WriteLine("[HttpNotification] 警告：无法获取 INotificationHostService，通知功能不可用");
            return;
        }
        host.RegisterNotificationProvider(this);
        Console.WriteLine("[HttpNotification] 通知提供程序已注册");
    }
}

/// <summary>
/// HTTP 通知服务器。独立于通知提供程序，先启动 HTTP 监听，
/// 收到请求时再通过 Provider 发送通知。
/// </summary>
public class HttpNotificationServer : IHostedService, IDisposable
{
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly ILogger<HttpNotificationServer> _logger;
    private HttpNotificationProvider? _provider;

    public const int ListenPort = 8765;

    public HttpNotificationServer(ILogger<HttpNotificationServer> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{ListenPort}/");
            _listener.Start();
            _logger.LogInformation("[HttpNotification] HTTP 服务器已启动: http://localhost:{Port}/", ListenPort);
            Console.WriteLine($"[HttpNotification] HTTP 服务器已启动: http://localhost:{ListenPort}/");

            _ = Task.Run(() => ListenLoop(_cts.Token), _cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HttpNotification] HTTP 服务器启动失败");
            Console.WriteLine($"[HttpNotification] HTTP 服务器启动失败: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        if (_listener?.IsListening == true)
        {
            _listener.Stop();
            _listener.Close();
        }
        return Task.CompletedTask;
    }

    private async Task ListenLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener?.IsListening == true)
        {
            try
            {
                var context = await _listener.GetContextAsync().WaitAsync(ct);
                _ = HandleRequestAsync(context, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (HttpListenerException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HttpNotification] 接受请求时出错");
            }
        }
    }

    /// <summary>
    /// 延迟获取通知提供程序（AppStarted 之后才可用）
    /// </summary>
    private HttpNotificationProvider? GetProvider()
    {
        if (_provider != null) return _provider;
        _provider = IAppHost.TryGetService<HttpNotificationProvider>();
        return _provider;
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken ct)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (request.Url?.AbsolutePath != "/notify" || request.HttpMethod != "POST")
            {
                await RespondJsonAsync(response, 404, new { error = "Not Found. Use POST /notify" });
                return;
            }

            string body;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                body = await reader.ReadToEndAsync(ct);
            }

            var payload = JsonSerializer.Deserialize<NotifyPayload>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payload == null || string.IsNullOrWhiteSpace(payload.Message))
            {
                await RespondJsonAsync(response, 400, new { error = "Missing 'message' field" });
                return;
            }

            var provider = GetProvider();
            if (provider == null)
            {
                await RespondJsonAsync(response, 503, new { error = "通知服务尚未就绪，请等待 ClassIsland 完全启动后再试" });
                return;
            }

            var duration = payload.Duration > 0 ? payload.Duration : 5;
            var title = payload.Title ?? "HTTP 通知";

            var maskContent = new NotificationContent
            {
                Content = $"{title}\n{payload.Message}",
                Duration = TimeSpan.FromSeconds(duration),
                SpeechContent = payload.Speech ?? payload.Message
            };

            var overlayContent = new NotificationContent
            {
                Content = payload.Message,
                Duration = TimeSpan.FromSeconds(duration),
                SpeechContent = payload.Speech ?? payload.Message
            };

            var notificationRequest = new NotificationRequest
            {
                MaskContent = maskContent,
                OverlayContent = overlayContent
            };

            await provider.ShowNotificationAsync(notificationRequest);
            _logger.LogInformation("[HttpNotification] 已发送: {Message}", payload.Message);
            await RespondJsonAsync(response, 200, new { ok = true, message = "通知已发送" });
        }
        catch (JsonException)
        {
            await RespondJsonAsync(response, 400, new { error = "Invalid JSON" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HttpNotification] 处理请求时出错");
            try { await RespondJsonAsync(response, 500, new { error = ex.Message }); } catch { }
        }
    }

    private static async Task RespondJsonAsync(HttpListenerResponse response, int statusCode, object data)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json; charset=utf-8";
        var json = JsonSerializer.Serialize(data);
        var buffer = System.Text.Encoding.UTF8.GetBytes(json);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.OutputStream.Close();
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _listener?.Close();
    }

    private class NotifyPayload
    {
        public string Message { get; set; } = "";
        public string? Title { get; set; }
        public int Duration { get; set; } = 5;
        public string? Speech { get; set; }
    }
}
