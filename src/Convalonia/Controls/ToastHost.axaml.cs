using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Convalonia.Services.Toast;

namespace Convalonia.Controls;

/// <summary>
/// Toast notification host control
/// </summary>
public class ToastHost : TemplatedControl, IToastHost
{
    public static readonly StyledProperty<ObservableCollection<ToastMessage>> ToastsProperty =
        AvaloniaProperty.Register<ToastHost, ObservableCollection<ToastMessage>>(
            nameof(Toasts),
            defaultValue: new ObservableCollection<ToastMessage>());

    public static readonly StyledProperty<ToastPosition> PositionProperty =
        AvaloniaProperty.Register<ToastHost, ToastPosition>(
            nameof(Position),
            defaultValue: ToastPosition.TopRight);

    public static readonly StyledProperty<int> MaxToastsProperty =
        AvaloniaProperty.Register<ToastHost, int>(
            nameof(MaxToasts),
            defaultValue: 5);

    public ObservableCollection<ToastMessage> Toasts
    {
        get => GetValue(ToastsProperty);
        set => SetValue(ToastsProperty, value);
    }

    public ToastPosition Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    public int MaxToasts
    {
        get => GetValue(MaxToastsProperty);
        set => SetValue(MaxToastsProperty, value);
    }

    public ToastHost()
    {
        Toasts = new ObservableCollection<ToastMessage>();
    }
}
