namespace Jinobald.Core.Mvvm;

/// <summary>
///     네비게이션 확인 요청 인터페이스
///     ViewModel이 네비게이션을 승인하거나 거부할 수 있도록 합니다.
///     저장되지 않은 변경사항 등이 있을 때 사용자에게 확인을 요청하는 데 사용됩니다.
/// </summary>
/// <remarks>
///     이 인터페이스는 INavigationAware를 확장하며, 비동기 확인 대화상자를
///     표시할 수 있는 콜백 기반 확인 메커니즘을 제공합니다.
///
///     사용 예시:
///     <code>
///     public void ConfirmNavigationRequest(NavigationContext context, Action&lt;bool&gt; continuationCallback)
///     {
///         if (HasUnsavedChanges)
///         {
///             var result = _dialogService.ShowConfirmation("저장하지 않은 변경사항이 있습니다. 계속하시겠습니까?");
///             continuationCallback(result);
///         }
///         else
///         {
///             continuationCallback(true);
///         }
///     }
///     </code>
/// </remarks>
public interface IConfirmNavigationRequest : INavigationAware
{
    /// <summary>
    ///     네비게이션 확인을 요청합니다.
    ///     ViewModel은 사용자에게 확인을 요청한 후 콜백을 호출해야 합니다.
    /// </summary>
    /// <param name="context">네비게이션 컨텍스트</param>
    /// <param name="continuationCallback">
    ///     true면 네비게이션 계속, false면 취소.
    ///     반드시 한 번만 호출해야 합니다.
    /// </param>
    void ConfirmNavigationRequest(NavigationContext context, Action<bool> continuationCallback);
}

/// <summary>
///     비동기 네비게이션 확인 요청 인터페이스
///     async/await 패턴을 사용하여 네비게이션 확인을 처리합니다.
/// </summary>
public interface IConfirmNavigationRequestAsync : INavigationAware
{
    /// <summary>
    ///     네비게이션 확인을 비동기적으로 요청합니다.
    /// </summary>
    /// <param name="context">네비게이션 컨텍스트</param>
    /// <returns>true면 네비게이션 계속, false면 취소</returns>
    Task<bool> ConfirmNavigationRequestAsync(NavigationContext context);
}

/// <summary>
///     NavigationContext 확장 메서드
/// </summary>
public static class NavigationContextExtensions
{
    /// <summary>
    ///     ViewModel이 IConfirmNavigationRequest를 구현하는지 확인하고
    ///     네비게이션 확인을 요청합니다.
    /// </summary>
    /// <param name="context">네비게이션 컨텍스트</param>
    /// <param name="viewModel">확인을 요청할 ViewModel</param>
    /// <param name="continuationCallback">결과 콜백</param>
    /// <returns>확인 요청이 수행되었는지 여부</returns>
    public static bool TryConfirmNavigation(
        this NavigationContext context,
        object? viewModel,
        Action<bool> continuationCallback)
    {
        if (viewModel is IConfirmNavigationRequest confirmable)
        {
            confirmable.ConfirmNavigationRequest(context, continuationCallback);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     ViewModel이 IConfirmNavigationRequestAsync를 구현하는지 확인하고
    ///     비동기적으로 네비게이션 확인을 요청합니다.
    /// </summary>
    /// <param name="context">네비게이션 컨텍스트</param>
    /// <param name="viewModel">확인을 요청할 ViewModel</param>
    /// <returns>확인 결과. ViewModel이 인터페이스를 구현하지 않으면 true</returns>
    public static async Task<bool> TryConfirmNavigationAsync(
        this NavigationContext context,
        object? viewModel)
    {
        if (viewModel is IConfirmNavigationRequestAsync confirmable)
        {
            return await confirmable.ConfirmNavigationRequestAsync(context);
        }

        return true; // Default: allow navigation
    }

    /// <summary>
    ///     콜백 기반 또는 비동기 확인을 통합적으로 처리합니다.
    /// </summary>
    /// <param name="context">네비게이션 컨텍스트</param>
    /// <param name="viewModel">확인을 요청할 ViewModel</param>
    /// <returns>확인 결과</returns>
    public static async Task<bool> ConfirmNavigationAsync(
        this NavigationContext context,
        object? viewModel)
    {
        if (viewModel == null)
            return true;

        // Async 버전 우선 확인
        if (viewModel is IConfirmNavigationRequestAsync asyncConfirmable)
        {
            return await asyncConfirmable.ConfirmNavigationRequestAsync(context);
        }

        // 콜백 버전을 Task로 래핑
        if (viewModel is IConfirmNavigationRequest confirmable)
        {
            var tcs = new TaskCompletionSource<bool>();
            confirmable.ConfirmNavigationRequest(context, result => tcs.TrySetResult(result));
            return await tcs.Task;
        }

        // INavigationAware의 OnNavigatingFromAsync 확인
        if (viewModel is INavigationAware navigationAware)
        {
            return await navigationAware.OnNavigatingFromAsync(context);
        }

        return true;
    }
}
