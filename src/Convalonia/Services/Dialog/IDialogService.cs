using System;
using System.Threading.Tasks;

namespace Convalonia.Services.Dialog;

/// <summary>
/// Dialog service interface
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Register dialog host
    /// </summary>
    void RegisterHost(IDialogHost host);

    /// <summary>
    /// Show a message dialog
    /// </summary>
    Task<DialogResult> ShowMessageAsync(string message, string? title = null, DialogButton buttons = DialogButton.OK);

    /// <summary>
    /// Show a confirmation dialog
    /// </summary>
    Task<bool> ShowConfirmationAsync(string message, string? title = null);
}

/// <summary>
/// Dialog host interface
/// </summary>
public interface IDialogHost
{
    /// <summary>
    /// Show content in dialog
    /// </summary>
    Task<object?> ShowAsync(object content);
}

/// <summary>
/// Dialog result
/// </summary>
public enum DialogResult
{
    None,
    OK,
    Cancel,
    Yes,
    No
}

/// <summary>
/// Dialog buttons
/// </summary>
[Flags]
public enum DialogButton
{
    OK = 1,
    Cancel = 2,
    Yes = 4,
    No = 8,
    OKCancel = OK | Cancel,
    YesNo = Yes | No,
    YesNoCancel = Yes | No | Cancel
}
