using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Jinobald.Core.Services.Dialog;

namespace Jinobald.Wpf.Controls;

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
    public static readonly DependencyProperty DialogStackProperty =
        DependencyProperty.Register(
            nameof(DialogStack),
            typeof(ObservableCollection<object>),
            typeof(DialogHost),
            new PropertyMetadata(null));

    /// <summary>
    ///     HasDialogs 속성 (읽기 전용)
    /// </summary>
    private static readonly DependencyPropertyKey HasDialogsPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(HasDialogs),
            typeof(bool),
            typeof(DialogHost),
            new PropertyMetadata(false));

    public static readonly DependencyProperty HasDialogsProperty =
        HasDialogsPropertyKey.DependencyProperty;

    static DialogHost()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(DialogHost),
            new FrameworkPropertyMetadata(typeof(DialogHost)));
    }

    public DialogHost()
    {
        DialogStack = new ObservableCollection<object>();
        DialogStack.CollectionChanged += (_, _) => UpdateHasDialogs();
    }

    /// <inheritdoc />
    public ObservableCollection<object> DialogStack
    {
        get => (ObservableCollection<object>)GetValue(DialogStackProperty);
        set => SetValue(DialogStackProperty, value);
    }

    /// <inheritdoc />
    public bool HasDialogs
    {
        get => (bool)GetValue(HasDialogsProperty);
        private set => SetValue(HasDialogsPropertyKey, value);
    }

    private void UpdateHasDialogs()
    {
        HasDialogs = DialogStack.Count > 0;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateHasDialogs();
    }
}
