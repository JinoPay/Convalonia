using Avalonia.Threading;
using Jinobald.Core.Services.Toast;
using Serilog;

namespace Jinobald.Avalonia.Services.Toast;

/// <summary>
///     Avalonia 토스트 서비스 구현
///     비침투적(non-intrusive) 알림을 표시합니다.
/// </summary>
public class ToastService : IToastService
{
    private readonly ILogger _logger;
    private IToastHost? _toastHost;
    private readonly Dictionary<Guid, CancellationTokenSource> _toastTimers = new();
    private readonly object _lock = new();

    /// <summary>
    ///     기본 표시 시간(초)
    /// </summary>
    public int DefaultDuration { get; set; } = 3;

    public ToastService()
    {
        _logger = Log.ForContext<ToastService>();
    }

    /// <inheritdoc />
    public void RegisterHost(IToastHost host)
    {
        _toastHost = host ?? throw new ArgumentNullException(nameof(host));
        _logger.Information("토스트 호스트 등록 완료");
    }

    /// <inheritdoc />
    public void ShowSuccess(string message, string? title = null, int? duration = null)
    {
        Show(new ToastMessage
        {
            Type = ToastType.Success,
            Title = title ?? "성공",
            Message = message,
            Duration = duration ?? DefaultDuration
        });
    }

    /// <inheritdoc />
    public void ShowInfo(string message, string? title = null, int? duration = null)
    {
        Show(new ToastMessage
        {
            Type = ToastType.Info,
            Title = title ?? "정보",
            Message = message,
            Duration = duration ?? DefaultDuration
        });
    }

    /// <inheritdoc />
    public void ShowWarning(string message, string? title = null, int? duration = null)
    {
        Show(new ToastMessage
        {
            Type = ToastType.Warning,
            Title = title ?? "경고",
            Message = message,
            Duration = duration ?? DefaultDuration
        });
    }

    /// <inheritdoc />
    public void ShowError(string message, string? title = null, int? duration = null)
    {
        Show(new ToastMessage
        {
            Type = ToastType.Error,
            Title = title ?? "에러",
            Message = message,
            Duration = duration ?? DefaultDuration
        });
    }

    /// <inheritdoc />
    public void Show(ToastMessage toast)
    {
        if (_toastHost == null)
        {
            _logger.Error("토스트 호스트가 등록되지 않음");
            throw new InvalidOperationException("토스트 호스트가 등록되지 않았습니다. RegisterHost()를 먼저 호출하세요.");
        }

        _logger.Debug("토스트 표시: {Type} - {Message}", toast.Type, toast.Message);

        // UI 쓰레드에서 토스트 추가
        Dispatcher.UIThread.Post(() =>
        {
            // 최대 토스트 개수 체크
            lock (_lock)
            {
                while (_toastHost.Toasts.Count >= _toastHost.MaxToasts)
                {
                    // 가장 오래된 토스트 제거
                    var oldest = _toastHost.Toasts.First();
                    RemoveToast(oldest);
                }

                _toastHost.Toasts.Add(toast);
            }

            // 자동 닫기 타이머 설정
            if (toast.Duration > 0)
            {
                var cts = new CancellationTokenSource();
                lock (_lock)
                {
                    _toastTimers[toast.Id] = cts;
                }

                _ = AutoCloseToastAsync(toast, cts.Token);
            }
        });
    }

    /// <inheritdoc />
    public void ClearAll()
    {
        if (_toastHost == null)
            return;

        _logger.Debug("모든 토스트 제거");

        lock (_lock)
        {
            // 모든 타이머 취소
            foreach (var cts in _toastTimers.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _toastTimers.Clear();

            // UI 쓰레드에서 토스트 제거
            Dispatcher.UIThread.Post(() =>
            {
                _toastHost.Toasts.Clear();
            });
        }
    }

    /// <summary>
    ///     토스트를 수동으로 닫습니다.
    /// </summary>
    public void Close(ToastMessage toast)
    {
        if (_toastHost == null)
            return;

        RemoveToast(toast);
    }

    private async Task AutoCloseToastAsync(ToastMessage toast, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(toast.Duration), cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                RemoveToast(toast);
            }
        }
        catch (TaskCanceledException)
        {
            // 타이머가 취소됨 (정상)
        }
    }

    private void RemoveToast(ToastMessage toast)
    {
        if (_toastHost == null)
            return;

        lock (_lock)
        {
            // 타이머 취소 및 제거
            if (_toastTimers.TryGetValue(toast.Id, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                _toastTimers.Remove(toast.Id);
            }

            // UI 쓰레드에서 토스트 제거
            Dispatcher.UIThread.Post(() =>
            {
                _toastHost.Toasts.Remove(toast);
            });
        }

        _logger.Debug("토스트 제거: {Id}", toast.Id);
    }
}
