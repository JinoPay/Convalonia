namespace Jinobald.Core.Services.Regions;

/// <summary>
///     View와 ViewModel 간의 타입 추론 및 인스턴스 생성을 담당하는 인터페이스
///     플랫폼별로 구현됩니다 (WPF, Avalonia).
///     Prism의 View/ViewModel 컨벤션을 참고함
/// </summary>
public interface IViewResolver
{
    #region View → ViewModel

    /// <summary>
    ///     View 타입에서 ViewModel 타입을 추론합니다.
    ///     Views.XView → ViewModels.XViewModel 패턴
    /// </summary>
    /// <param name="viewType">View 타입</param>
    /// <returns>ViewModel 타입 (없으면 null)</returns>
    Type? ResolveViewModelType(Type viewType);

    /// <summary>
    ///     View 인스턴스를 생성하고 ViewModel을 자동으로 연결합니다.
    /// </summary>
    /// <param name="viewType">View 타입</param>
    /// <returns>생성된 View (DataContext에 ViewModel 바인딩됨)</returns>
    object ResolveView(Type viewType);

    #endregion

    #region ViewModel → View

    /// <summary>
    ///     ViewModel 타입에서 View 타입을 추론합니다.
    ///     ViewModels.XViewModel → Views.XView 패턴
    /// </summary>
    /// <param name="viewModelType">ViewModel 타입</param>
    /// <returns>View 타입 (없으면 null)</returns>
    Type? ResolveViewType(Type viewModelType);

    /// <summary>
    ///     지정된 ViewModel에 대한 View를 생성하고 DataContext를 바인딩합니다.
    /// </summary>
    /// <param name="viewModelType">ViewModel 타입</param>
    /// <param name="viewModel">ViewModel 인스턴스</param>
    /// <returns>생성된 View</returns>
    object ResolveView(Type viewModelType, object viewModel);

    #endregion
}
