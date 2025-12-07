using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Jinobald.Core.Services.Toast;

namespace Jinobald.Avalonia.Controls;

/// <summary>
///     토스트 호스트 컨트롤
///     비침투적(non-intrusive) 알림을 표시합니다.
///     사용자가 DataTemplate을 통해 UI를 커스터마이징할 수 있습니다.
/// </summary>
public class ToastHost : TemplatedControl, IToastHost
{
    /// <summary>
    ///     Toasts 속성
    /// </summary>
    public static readonly StyledProperty<ObservableCollection<ToastMessage>> ToastsProperty =
        AvaloniaProperty.Register<ToastHost, ObservableCollection<ToastMessage>>(
            nameof(Toasts));

    /// <summary>
    ///     Position 속성
    /// </summary>
    public static readonly StyledProperty<ToastPosition> PositionProperty =
        AvaloniaProperty.Register<ToastHost, ToastPosition>(
            nameof(Position), ToastPosition.TopRight);

    /// <summary>
    ///     MaxToasts 속성
    /// </summary>
    public static readonly StyledProperty<int> MaxToastsProperty =
        AvaloniaProperty.Register<ToastHost, int>(
            nameof(MaxToasts), 5);

    public ToastHost()
    {
        Toasts = new ObservableCollection<ToastMessage>();
    }

    /// <inheritdoc />
    public ObservableCollection<ToastMessage> Toasts
    {
        get => GetValue(ToastsProperty);
        set => SetValue(ToastsProperty, value);
    }

    /// <inheritdoc />
    public ToastPosition Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    /// <inheritdoc />
    public int MaxToasts
    {
        get => GetValue(MaxToastsProperty);
        set => SetValue(MaxToastsProperty, value);
    }
}
