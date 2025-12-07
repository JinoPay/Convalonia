using System;
using System.Threading.Tasks;

namespace Convalonia.Services.Dialog;

/// <summary>
/// Simple dialog service implementation
/// </summary>
public class DialogService : IDialogService
{
    private IDialogHost? _host;

    public void RegisterHost(IDialogHost host)
    {
        _host = host;
    }

    public async Task<DialogResult> ShowMessageAsync(string message, string? title = null, DialogButton buttons = DialogButton.OK)
    {
        // For now, just log to console
        // In a real implementation, you'd show a proper dialog
        Console.WriteLine($"[Dialog] {title ?? "Message"}: {message}");
        await Task.CompletedTask;
        return DialogResult.OK;
    }

    public async Task<bool> ShowConfirmationAsync(string message, string? title = null)
    {
        var result = await ShowMessageAsync(message, title, DialogButton.YesNo);
        return result == DialogResult.Yes;
    }
}
