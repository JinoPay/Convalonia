using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Jinobald.Core.Services.Dialog;

namespace Jinobald.Avalonia.Controls;

/// <summary>
///     다이얼로그 호스트 컨트롤
///     in-window overlay 방식으로 다이얼로그를 표시합니다.
///     중첩 다이얼로그를 지원합니다.
/// </summary>
public class DialogHost : ContentControl, IDialogHost
{
    /// <summary>
    ///     DialogStack 속성
    /// </summary>
    public static readonly StyledProperty<ObservableCollection<object>> DialogStackProperty =
        AvaloniaProperty.Register<DialogHost, ObservableCollection<object>>(
            nameof(DialogStack));

    /// <summary>
    ///     HasDialogs 속성
    /// </summary>
    public static readonly DirectProperty<DialogHost, bool> HasDialogsProperty =
        AvaloniaProperty.RegisterDirect<DialogHost, bool>(
            nameof(HasDialogs),
            o => o.HasDialogs);

    private bool _hasDialogs;

    public DialogHost()
    {
        DialogStack = new ObservableCollection<object>();
        DialogStack.CollectionChanged += (_, _) => UpdateHasDialogs();
    }

    /// <inheritdoc />
    public ObservableCollection<object> DialogStack
    {
        get => GetValue(DialogStackProperty);
        set => SetValue(DialogStackProperty, value);
    }

    /// <inheritdoc />
    public bool HasDialogs
    {
        get => _hasDialogs;
        private set => SetAndRaise(HasDialogsProperty, ref _hasDialogs, value);
    }

    private void UpdateHasDialogs()
    {
        HasDialogs = DialogStack.Count > 0;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdateHasDialogs();
    }
}
