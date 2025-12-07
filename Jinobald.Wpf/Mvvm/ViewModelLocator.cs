using System.Windows;
using Jinobald.Core.Ioc;

namespace Jinobald.Wpf.Mvvm;

/// <summary>
///     View와 ViewModel을 컨벤션 기반으로 자동 연결하는 ViewModelLocator
///     Views.SplashView → ViewModels.SplashViewModel 패턴으로 자동 매칭
///     ContainerLocator를 통해 ViewModel을 해결합니다.
/// </summary>
public static class ViewModelLocator
{
    /// <summary>
    ///     AutoWireViewModel Attached Property
    /// </summary>
    public static readonly DependencyProperty AutoWireViewModelProperty =
        DependencyProperty.RegisterAttached(
            "AutoWireViewModel",
            typeof(bool),
            typeof(ViewModelLocator),
            new PropertyMetadata(false, OnAutoWireViewModelChanged));

    public static bool GetAutoWireViewModel(DependencyObject obj)
    {
        return (bool)obj.GetValue(AutoWireViewModelProperty);
    }

    public static void SetAutoWireViewModel(DependencyObject obj, bool value)
    {
        obj.SetValue(AutoWireViewModelProperty, value);
    }

    /// <summary>
    ///     View 타입에서 ViewModel 타입을 추론
    /// </summary>
    public static Type? ResolveViewModelType(Type viewType)
    {
        var viewName = viewType.FullName;
        if (string.IsNullOrEmpty(viewName))
            return null;

        // 네임스페이스 변환: Views → ViewModels
        var viewModelName = viewName.Replace(".Views.", ".ViewModels.");

        // 클래스명 변환
        if (viewModelName.EndsWith("Window"))
        {
            // MainWindow → MainWindowViewModel
            viewModelName += "ViewModel";
        }
        else if (viewModelName.EndsWith("View"))
        {
            // HomeView → HomeViewModel
            viewModelName = viewModelName[..^4] + "ViewModel";
        }

        return viewType.Assembly.GetType(viewModelName);
    }

    /// <summary>
    ///     ViewModel 타입에서 View 타입을 추론
    /// </summary>
    public static Type? ResolveViewType(Type viewModelType)
    {
        var viewModelName = viewModelType.FullName;
        if (string.IsNullOrEmpty(viewModelName))
            return null;

        // 네임스페이스 변환: ViewModels → Views
        var viewName = viewModelName.Replace(".ViewModels.", ".Views.");

        // 클래스명 변환
        if (viewName.EndsWith("WindowViewModel"))
        {
            // MainWindowViewModel → MainWindow
            viewName = viewName[..^9]; // "ViewModel" 제거
        }
        else if (viewName.EndsWith("ViewModel"))
        {
            // HomeViewModel → HomeView
            viewName = viewName[..^9] + "View"; // "ViewModel" 제거, "View" 추가
        }

        return viewModelType.Assembly.GetType(viewName);
    }

    /// <summary>
    ///     View 타입에서 ViewModel을 찾아 ContainerLocator를 통해 resolve
    /// </summary>
    private static object? ResolveViewModel(Type viewType)
    {
        if (!ContainerLocator.IsSet)
            return null;

        var viewModelType = ResolveViewModelType(viewType);
        if (viewModelType == null)
            return null;

        return ContainerLocator.Current.Resolve(viewModelType);
    }

    private static void OnAutoWireViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && d is FrameworkElement element)
        {
            var viewModel = ResolveViewModel(element.GetType());
            if (viewModel != null)
                element.DataContext = viewModel;
        }
    }
}
