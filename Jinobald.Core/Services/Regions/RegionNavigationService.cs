using Jinobald.Core.Ioc;
using Jinobald.Core.Mvvm;

namespace Jinobald.Core.Services.Regions;

/// <summary>
///     리전 내 네비게이션 서비스 구현체
///     Prism의 RegionNavigationService를 참고하여 View 기반으로 재설계됨
///     - Back/Forward 히스토리 지원
///     - Keep-Alive 뷰 캐싱 지원
///     - 완전한 생명주기 관리
/// </summary>
public class RegionNavigationService : IRegionNavigationService
{
    private readonly IViewResolver _viewResolver;
    private readonly SemaphoreSlim _navigationLock = new(1, 1);

    // 히스토리 스택
    private readonly Stack<NavigationEntry> _backStack = new();
    private readonly Stack<NavigationEntry> _forwardStack = new();

    // Keep-Alive 캐시
    private readonly Dictionary<Type, (object View, object? ViewModel)> _viewCache = new();

    // 현재 네비게이션 엔트리
    private NavigationEntry? _currentEntry;
    private bool _isNavigating;

    public RegionNavigationService(IRegion region, IViewResolver viewResolver)
    {
        Region = region ?? throw new ArgumentNullException(nameof(region));
        _viewResolver = viewResolver ?? throw new ArgumentNullException(nameof(viewResolver));
    }

    #region IRegionNavigationService 구현

    public IRegion Region { get; }

    public object? CurrentView => _currentEntry?.View;

    public object? CurrentViewModel => _currentEntry?.ViewModel;

    public bool IsNavigating => _isNavigating;

    public bool CanGoBack => _backStack.Count > 0;

    public bool CanGoForward => _forwardStack.Count > 0;

    public int HistoryCount => _backStack.Count + _forwardStack.Count + (_currentEntry != null ? 1 : 0);

    public bool KeepAlive { get; set; }

    public RegionNavigationMode NavigationMode { get; set; } = RegionNavigationMode.Stack;

    public event EventHandler<object?>? Navigated;

    #endregion

    #region View 기반 네비게이션

    public Task<bool> NavigateAsync<TView>(object? parameter = null,
        CancellationToken cancellationToken = default) where TView : class
    {
        return NavigateAsync(typeof(TView), parameter, cancellationToken);
    }

    public async Task<bool> NavigateAsync(Type viewType, object? parameter = null,
        CancellationToken cancellationToken = default)
    {
        // 동시 네비게이션 방지
        if (!await _navigationLock.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
            return false;

        _isNavigating = true;

        try
        {
            // 1. 새 뷰 준비 (캐시에서 가져오거나 새로 생성)
            var (newView, newViewModel) = await PrepareViewAsync(viewType, cancellationToken);

            // 2. 네비게이션 컨텍스트 생성
            var context = CreateNavigationContext(viewType, parameter, NavigationDirection.Forward, cancellationToken);

            // 3. Guard: 현재 뷰에서 나갈 수 있는지 확인
            if (!await CanNavigateFromAsync(context))
                return false;

            // 4. Guard: 새 뷰로 들어갈 수 있는지 확인
            if (!await CanNavigateToAsync(newViewModel, context))
                return false;

            // 5. 현재 뷰 비활성화 및 저장
            await DeactivateCurrentViewAsync(context);

            // 6. 히스토리 관리 (NavigationMode에 따라)
            ManageHistory(viewType, newView, newViewModel, parameter);

            // 7. 새 뷰 활성화
            await ActivateNewViewAsync(newView, newViewModel, context);

            // 8. 이벤트 발생
            Navigated?.Invoke(this, newView);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            _isNavigating = false;
            _navigationLock.Release();
        }
    }

    #endregion

    #region Back/Forward 네비게이션

    public async Task<bool> GoBackAsync(CancellationToken cancellationToken = default)
    {
        if (!CanGoBack)
            return false;

        if (!await _navigationLock.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
            return false;

        _isNavigating = true;

        try
        {
            var previousEntry = _backStack.Pop();

            // 네비게이션 컨텍스트 생성
            var context = CreateNavigationContext(
                previousEntry.ViewType,
                previousEntry.Parameter,
                NavigationDirection.Back,
                cancellationToken);

            // Guard: 현재 뷰에서 나갈 수 있는지 확인
            if (!await CanNavigateFromAsync(context))
            {
                _backStack.Push(previousEntry); // 롤백
                return false;
            }

            // Guard: 이전 뷰로 들어갈 수 있는지 확인
            if (!await CanNavigateToAsync(previousEntry.ViewModel, context))
            {
                _backStack.Push(previousEntry); // 롤백
                return false;
            }

            // 현재 엔트리를 Forward 스택에 추가
            if (_currentEntry != null)
                _forwardStack.Push(_currentEntry);

            // 현재 뷰 비활성화
            await DeactivateCurrentViewAsync(context);

            // 이전 뷰 활성화
            await ActivateNewViewAsync(previousEntry.View, previousEntry.ViewModel, context);

            // 현재 엔트리 업데이트
            _currentEntry = previousEntry;

            // 이벤트 발생
            Navigated?.Invoke(this, previousEntry.View);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            _isNavigating = false;
            _navigationLock.Release();
        }
    }

    public async Task<bool> GoForwardAsync(CancellationToken cancellationToken = default)
    {
        if (!CanGoForward)
            return false;

        if (!await _navigationLock.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
            return false;

        _isNavigating = true;

        try
        {
            var nextEntry = _forwardStack.Pop();

            // 네비게이션 컨텍스트 생성
            var context = CreateNavigationContext(
                nextEntry.ViewType,
                nextEntry.Parameter,
                NavigationDirection.Forward,
                cancellationToken);

            // Guard: 현재 뷰에서 나갈 수 있는지 확인
            if (!await CanNavigateFromAsync(context))
            {
                _forwardStack.Push(nextEntry); // 롤백
                return false;
            }

            // Guard: 다음 뷰로 들어갈 수 있는지 확인
            if (!await CanNavigateToAsync(nextEntry.ViewModel, context))
            {
                _forwardStack.Push(nextEntry); // 롤백
                return false;
            }

            // 현재 엔트리를 Back 스택에 추가
            if (_currentEntry != null)
                _backStack.Push(_currentEntry);

            // 현재 뷰 비활성화
            await DeactivateCurrentViewAsync(context);

            // 다음 뷰 활성화
            await ActivateNewViewAsync(nextEntry.View, nextEntry.ViewModel, context);

            // 현재 엔트리 업데이트
            _currentEntry = nextEntry;

            // 이벤트 발생
            Navigated?.Invoke(this, nextEntry.View);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            _isNavigating = false;
            _navigationLock.Release();
        }
    }

    #endregion

    #region 히스토리 및 캐시 관리

    public void ClearHistory()
    {
        _backStack.Clear();
        _forwardStack.Clear();
    }

    public void ClearCurrentView()
    {
        if (_currentEntry != null)
        {
            Region.Deactivate(_currentEntry.View);
            Region.Remove(_currentEntry.View);

            // ViewModel 리소스 정리
            if (_currentEntry.ViewModel is IDestructible destructible)
                destructible.Destroy();

            _currentEntry = null;
        }
    }

    public void ClearCache()
    {
        // 캐시된 모든 뷰 정리
        foreach (var (view, viewModel) in _viewCache.Values)
        {
            if (viewModel is IDestructible destructible)
                destructible.Destroy();
        }

        _viewCache.Clear();
    }

    #endregion

    #region Private 헬퍼 메서드

    /// <summary>
    ///     뷰 준비 (캐시에서 가져오거나 새로 생성)
    /// </summary>
    private async Task<(object View, object? ViewModel)> PrepareViewAsync(Type viewType,
        CancellationToken cancellationToken)
    {
        // 캐시에 있으면 재사용 (IRegionMemberLifetime 또는 Region KeepAlive로 캐시된 경우)
        if (_viewCache.TryGetValue(viewType, out var cached))
        {
            return cached;
        }

        // 새 뷰 생성
        var view = _viewResolver.ResolveView(viewType);

        // ViewModel 가져오기 (ViewResolver가 자동으로 DataContext에 바인딩함)
        object? viewModel = null;
        if (view is IViewFor viewFor)
        {
            viewModel = viewFor.ViewModel;
        }
        else
        {
            // 플랫폼별 DataContext 접근 (Avalonia, WPF)
            var dataContextProperty = view.GetType().GetProperty("DataContext");
            viewModel = dataContextProperty?.GetValue(view);
        }

        // 초기화
        if (viewModel is IInitializableAsync initializable)
            await initializable.InitializeAsync(cancellationToken);

        // Keep-Alive 캐시에 추가 (IRegionMemberLifetime 또는 Region 설정에 따라)
        if (ShouldKeepAlive(view, viewModel))
        {
            _viewCache[viewType] = (view, viewModel);
        }

        return (view, viewModel);
    }

    /// <summary>
    ///     View/ViewModel이 캐시되어야 하는지 결정
    ///     IRegionMemberLifetime이 구현되어 있으면 해당 설정을 우선 사용
    /// </summary>
    private bool ShouldKeepAlive(object view, object? viewModel)
    {
        // 1. ViewModel에서 IRegionMemberLifetime 확인 (우선순위 높음)
        if (viewModel is IRegionMemberLifetime viewModelLifetime)
            return viewModelLifetime.KeepAlive;

        // 2. View에서 IRegionMemberLifetime 확인
        if (view is IRegionMemberLifetime viewLifetime)
            return viewLifetime.KeepAlive;

        // 3. Region의 전역 KeepAlive 설정 사용
        return KeepAlive;
    }

    /// <summary>
    ///     네비게이션 컨텍스트 생성
    /// </summary>
    private NavigationContext CreateNavigationContext(Type targetViewType, object? parameter,
        NavigationDirection direction, CancellationToken cancellationToken)
    {
        var targetViewModelType = _viewResolver.ResolveViewModelType(targetViewType);
        var sourceViewType = _currentEntry?.ViewType;
        var sourceViewModelType = _currentEntry?.ViewModel?.GetType();

        return new NavigationContext
        {
            Parameter = parameter,
            Direction = direction,
            SourceViewType = sourceViewType,
            TargetViewType = targetViewType,
            SourceViewModelType = sourceViewModelType,
            TargetViewModelType = targetViewModelType,
            CancellationToken = cancellationToken
        };
    }

    /// <summary>
    ///     현재 뷰에서 나갈 수 있는지 확인 (Guard)
    ///     IConfirmNavigationRequest, IConfirmNavigationRequestAsync, INavigationAware 순으로 확인
    /// </summary>
    private async Task<bool> CanNavigateFromAsync(NavigationContext context)
    {
        // IConfirmNavigationRequest 또는 IConfirmNavigationRequestAsync를 통합적으로 처리
        // 이 메서드는 INavigationAware.OnNavigatingFromAsync도 확인함
        return await context.ConfirmNavigationAsync(CurrentViewModel);
    }

    /// <summary>
    ///     새 뷰로 들어갈 수 있는지 확인 (Guard)
    /// </summary>
    private async Task<bool> CanNavigateToAsync(object? viewModel, NavigationContext context)
    {
        if (viewModel is INavigationAware aware)
        {
            return await aware.OnNavigatingToAsync(context);
        }

        return true;
    }

    /// <summary>
    ///     현재 뷰 비활성화
    /// </summary>
    private async Task DeactivateCurrentViewAsync(NavigationContext context)
    {
        if (_currentEntry == null)
            return;

        // 비활성화
        if (_currentEntry.ViewModel is IActivatable activatable)
            await activatable.DeactivateAsync(context.CancellationToken);

        // OnNavigatedFrom 호출
        if (_currentEntry.ViewModel is INavigationAware aware)
            await aware.OnNavigatedFromAsync(context);

        // 리전에서 비활성화 및 제거
        Region.Deactivate(_currentEntry.View);

        // Keep-Alive 여부 확인 (IRegionMemberLifetime 또는 Region 설정)
        var shouldKeepAlive = ShouldKeepAlive(_currentEntry.View, _currentEntry.ViewModel);

        // Keep-Alive가 아니거나 Replace 모드이면 제거 및 리소스 정리
        if (!shouldKeepAlive || NavigationMode == RegionNavigationMode.Replace)
        {
            Region.Remove(_currentEntry.View);

            // 캐시에서도 제거
            _viewCache.Remove(_currentEntry.ViewType);

            if (_currentEntry.ViewModel is IDestructible destructible)
                destructible.Destroy();
        }
    }

    /// <summary>
    ///     새 뷰 활성화
    /// </summary>
    private async Task ActivateNewViewAsync(object view, object? viewModel, NavigationContext context)
    {
        // 리전에 추가 및 활성화
        if (!Region.Contains(view))
            Region.Add(view);

        Region.Activate(view);

        // OnNavigatedTo 호출
        if (viewModel is INavigationAware aware)
            await aware.OnNavigatedToAsync(context);

        // 활성화
        if (viewModel is IActivatable activatable)
            await activatable.ActivateAsync(context.CancellationToken);
    }

    /// <summary>
    ///     히스토리 관리 (NavigationMode에 따라)
    /// </summary>
    private void ManageHistory(Type viewType, object view, object? viewModel, object? parameter)
    {
        switch (NavigationMode)
        {
            case RegionNavigationMode.Stack:
                // 현재 엔트리를 Back 스택에 추가
                if (_currentEntry != null)
                    _backStack.Push(_currentEntry);

                // Forward 스택 클리어 (새로운 경로 시작)
                _forwardStack.Clear();
                break;

            case RegionNavigationMode.Replace:
                // 히스토리 관리 안 함
                ClearHistory();
                break;

            case RegionNavigationMode.Accumulate:
                // 뷰를 누적 (히스토리 없음)
                // 이 모드에서는 여러 뷰가 동시에 표시됨 (ItemsControl 용)
                break;
        }

        // 새 엔트리를 현재로 설정
        _currentEntry = new NavigationEntry
        {
            ViewType = viewType,
            View = view,
            ViewModel = viewModel,
            Parameter = parameter
        };
    }

    #endregion
}

/// <summary>
///     네비게이션 히스토리 엔트리
/// </summary>
internal class NavigationEntry
{
    public required Type ViewType { get; init; }
    public required object View { get; init; }
    public object? ViewModel { get; init; }
    public object? Parameter { get; init; }
}

/// <summary>
///     View와 ViewModel 연결을 위한 마커 인터페이스
/// </summary>
public interface IViewFor
{
    object? ViewModel { get; }
}
