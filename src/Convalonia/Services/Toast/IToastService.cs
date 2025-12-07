using System;
using System.Collections.ObjectModel;

namespace Convalonia.Services.Toast;

/// <summary>
/// Toast notification service
/// Displays non-intrusive notifications that auto-dismiss
/// </summary>
public interface IToastService
{
    /// <summary>
    /// Register toast host container
    /// Should be called from MainWindow
    /// </summary>
    void RegisterHost(IToastHost host);

    /// <summary>
    /// Show success toast message
    /// </summary>
    void ShowSuccess(string message, string? title = null, int? duration = null);

    /// <summary>
    /// Show info toast message
    /// </summary>
    void ShowInfo(string message, string? title = null, int? duration = null);

    /// <summary>
    /// Show warning toast message
    /// </summary>
    void ShowWarning(string message, string? title = null, int? duration = null);

    /// <summary>
    /// Show error toast message
    /// </summary>
    void ShowError(string message, string? title = null, int? duration = null);

    /// <summary>
    /// Show custom toast
    /// </summary>
    void Show(ToastMessage toast);

    /// <summary>
    /// Clear all toasts
    /// </summary>
    void ClearAll();
}

/// <summary>
/// Toast message model
/// </summary>
public class ToastMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string? Title { get; init; }
    public required string Message { get; init; }
    public ToastType Type { get; init; } = ToastType.Info;
    public int Duration { get; init; } = 3;
    public DateTime CreatedAt { get; init; } = DateTime.Now;
}

/// <summary>
/// Toast type enumeration
/// </summary>
public enum ToastType
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>
/// Toast position enumeration
/// </summary>
public enum ToastPosition
{
    TopRight,
    TopLeft,
    TopCenter,
    BottomRight,
    BottomLeft,
    BottomCenter
}

/// <summary>
/// Toast host interface for displaying toasts
/// </summary>
public interface IToastHost
{
    ObservableCollection<ToastMessage> Toasts { get; }
    ToastPosition Position { get; set; }
    int MaxToasts { get; set; }
}
