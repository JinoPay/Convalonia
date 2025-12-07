using System.Windows;
using Jinobald.Core.Ioc;
using Jinobald.Core.Services.Regions;
using Jinobald.Wpf.Mvvm;
using Microsoft.Extensions.DependencyInjection;

namespace Jinobald.Wpf.Services.Regions;

/// <summary>
///     WPF용 View Resolver
///     Prism의 컨벤션 기반 View/ViewModel 해석을 따름
///     - View → ViewModel: Views.XView → ViewModels.XViewModel
///     - ViewModel → View: ViewModels.XViewModel → Views.XView
/// </summary>
public class WpfViewResolver : IViewResolver
{
    #region View → ViewModel

    public Type? ResolveViewModelType(Type viewType)
    {
        return ViewModelLocator.ResolveViewModelType(viewType);
    }

    public object ResolveView(Type viewType)
    {
        // 1. View 인스턴스 생성 (DI 우선, 없으면 ActivatorUtilities로 생성)
        var view = ResolveOrCreate(viewType);
        if (view is not FrameworkElement element)
            throw new InvalidOperationException($"{viewType.FullName} is not a valid WPF FrameworkElement");

        // 2. ViewModel 타입 추론
        var viewModelType = ResolveViewModelType(viewType);
        if (viewModelType != null)
        {
            // 3. ViewModel 생성 (DI 우선, 없으면 ActivatorUtilities로 생성)
            var viewModel = ResolveOrCreate(viewModelType);

            // 4. DataContext 바인딩
            element.DataContext = viewModel;
        }

        return element;
    }

    #endregion

    #region ViewModel → View

    public Type? ResolveViewType(Type viewModelType)
    {
        return ViewModelLocator.ResolveViewType(viewModelType);
    }

    public object ResolveView(Type viewModelType, object viewModel)
    {
        // 1. View 타입 추론
        var viewType = ResolveViewType(viewModelType);
        if (viewType == null)
            throw new InvalidOperationException($"Could not resolve view type for {viewModelType.FullName}");

        // 2. View 인스턴스 생성 (DI 우선, 없으면 ActivatorUtilities로 생성)
        var view = ResolveOrCreate(viewType);
        if (view is not FrameworkElement element)
            throw new InvalidOperationException($"{viewType.FullName} is not a valid WPF FrameworkElement");

        // 3. DataContext 바인딩
        element.DataContext = viewModel;

        return element;
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///     DI 컨테이너에서 먼저 resolve를 시도하고, 등록되지 않은 경우 ActivatorUtilities로 생성합니다.
    ///     Prism과 동일하게 View를 명시적으로 등록하지 않아도 자동으로 생성됩니다.
    /// </summary>
    private static object ResolveOrCreate(Type type)
    {
        var serviceProvider = (IServiceProvider)ContainerLocator.Current.Instance;

        // DI에서 먼저 시도
        var service = serviceProvider.GetService(type);
        if (service != null)
            return service;

        // DI에 없으면 ActivatorUtilities로 생성 (생성자 의존성 주입 지원)
        return ActivatorUtilities.CreateInstance(serviceProvider, type);
    }

    #endregion
}
