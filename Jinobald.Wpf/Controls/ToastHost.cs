using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Jinobald.Core.Services.Toast;

namespace Jinobald.Wpf.Controls;

/// <summary>
///     토스트 호스트 컨트롤
///     비침투적(non-intrusive) 알림을 표시합니다.
///     사용자가 DataTemplate을 통해 UI를 커스터마이징할 수 있습니다.
/// </summary>
public class ToastHost : Control, IToastHost
{
    /// <summary>
    ///     Toasts 속성
    /// </summary>
    public static readonly DependencyProperty ToastsProperty =
        DependencyProperty.Register(
            nameof(Toasts),
            typeof(ObservableCollection<ToastMessage>),
            typeof(ToastHost),
            new PropertyMetadata(null));

    /// <summary>
    ///     Position 속성
    /// </summary>
    public static readonly DependencyProperty PositionProperty =
        DependencyProperty.Register(
            nameof(Position),
            typeof(ToastPosition),
            typeof(ToastHost),
            new PropertyMetadata(ToastPosition.TopRight));

    /// <summary>
    ///     MaxToasts 속성
    /// </summary>
    public static readonly DependencyProperty MaxToastsProperty =
        DependencyProperty.Register(
            nameof(MaxToasts),
            typeof(int),
            typeof(ToastHost),
            new PropertyMetadata(5));

    static ToastHost()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ToastHost),
            new FrameworkPropertyMetadata(typeof(ToastHost)));
    }

    public ToastHost()
    {
        Toasts = new ObservableCollection<ToastMessage>();
    }

    /// <inheritdoc />
    public ObservableCollection<ToastMessage> Toasts
    {
        get => (ObservableCollection<ToastMessage>)GetValue(ToastsProperty);
        set => SetValue(ToastsProperty, value);
    }

    /// <inheritdoc />
    public ToastPosition Position
    {
        get => (ToastPosition)GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    /// <inheritdoc />
    public int MaxToasts
    {
        get => (int)GetValue(MaxToastsProperty);
        set => SetValue(MaxToastsProperty, value);
    }
}
