using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Convalonia.Services.Dialog;

namespace Convalonia.Controls;

/// <summary>
/// Dialog host control
/// </summary>
public class DialogHost : ContentControl, IDialogHost
{
    public static readonly StyledProperty<bool> IsDialogOpenProperty =
        AvaloniaProperty.Register<DialogHost, bool>(nameof(IsDialogOpen));

    public static readonly StyledProperty<object?> DialogContentProperty =
        AvaloniaProperty.Register<DialogHost, object?>(nameof(DialogContent));

    public bool IsDialogOpen
    {
        get => GetValue(IsDialogOpenProperty);
        set => SetValue(IsDialogOpenProperty, value);
    }

    public object? DialogContent
    {
        get => GetValue(DialogContentProperty);
        set => SetValue(DialogContentProperty, value);
    }

    private TaskCompletionSource<object?>? _dialogTask;

    public Task<object?> ShowAsync(object content)
    {
        DialogContent = content;
        IsDialogOpen = true;
        _dialogTask = new TaskCompletionSource<object?>();
        return _dialogTask.Task;
    }

    public void CloseDialog(object? result = null)
    {
        IsDialogOpen = false;
        DialogContent = null;
        _dialogTask?.SetResult(result);
        _dialogTask = null;
    }
}
