using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Convalonia.Services.Toast;

/// <summary>
/// Toast service implementation
/// </summary>
public class ToastService : IToastService
{
    private IToastHost? _host;
    private const int DefaultDuration = 3;

    public void RegisterHost(IToastHost host)
    {
        _host = host;
    }

    public void ShowSuccess(string message, string? title = null, int? duration = null)
    {
        Show(new ToastMessage
        {
            Message = message,
            Title = title,
            Type = ToastType.Success,
            Duration = duration ?? DefaultDuration
        });
    }

    public void ShowInfo(string message, string? title = null, int? duration = null)
    {
        Show(new ToastMessage
        {
            Message = message,
            Title = title,
            Type = ToastType.Info,
            Duration = duration ?? DefaultDuration
        });
    }

    public void ShowWarning(string message, string? title = null, int? duration = null)
    {
        Show(new ToastMessage
        {
            Message = message,
            Title = title,
            Type = ToastType.Warning,
            Duration = duration ?? DefaultDuration
        });
    }

    public void ShowError(string message, string? title = null, int? duration = null)
    {
        Show(new ToastMessage
        {
            Message = message,
            Title = title,
            Type = ToastType.Error,
            Duration = duration ?? DefaultDuration
        });
    }

    public void Show(ToastMessage toast)
    {
        if (_host == null)
        {
            // Fallback to console if no host registered
            Console.WriteLine($"[{toast.Type}] {toast.Title ?? ""}: {toast.Message}");
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            // Add to host's toast collection
            _host.Toasts.Add(toast);

            // Remove old toasts if exceeding max
            while (_host.Toasts.Count > _host.MaxToasts)
            {
                _host.Toasts.RemoveAt(0);
            }

            // Auto-dismiss after duration
            if (toast.Duration > 0)
            {
                Task.Delay(TimeSpan.FromSeconds(toast.Duration))
                    .ContinueWith(_ =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            var toastToRemove = _host.Toasts.FirstOrDefault(t => t.Id == toast.Id);
                            if (toastToRemove != null)
                            {
                                _host.Toasts.Remove(toastToRemove);
                            }
                        });
                    });
            }
        });
    }

    public void ClearAll()
    {
        if (_host == null)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            _host.Toasts.Clear();
        });
    }
}
