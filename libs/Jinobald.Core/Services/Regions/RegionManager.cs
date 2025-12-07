using Jinobald.Core.Ioc;

namespace Jinobald.Core.Services.Regions;

/// <summary>
///     리전 매니저 구현체
///     Prism의 RegionManager를 참고하여 View 기반으로 재설계됨
///     애플리케이션 내 모든 리전을 관리하고 리전 기반 네비게이션을 제공합니다.
/// </summary>
public class RegionManager : IRegionManager
{
    private readonly Dictionary<string, IRegion> _regions = new();
    private readonly Dictionary<string, IRegionNavigationService> _navigationServices = new();
    private readonly Dictionary<string, Type> _pendingViewRegistrations = new();
    private readonly IViewResolver _viewResolver;

    public RegionManager(IViewResolver viewResolver)
    {
        _viewResolver = viewResolver ?? throw new ArgumentNullException(nameof(viewResolver));
    }

    #region 리전 관리

    public IEnumerable<IRegion> Regions => _regions.Values;

    public event EventHandler<IRegion>? RegionAdded;
    public event EventHandler<string>? RegionRemoved;

    public IRegion CreateOrGetRegion(string regionName)
    {
        if (string.IsNullOrWhiteSpace(regionName))
            throw new ArgumentException("Region name cannot be null or empty.", nameof(regionName));

        if (_regions.TryGetValue(regionName, out var existingRegion))
            return existingRegion;

        var region = new Region(regionName);
        RegisterRegion(region);

        return region;
    }

    public IRegion? GetRegion(string regionName)
    {
        if (string.IsNullOrWhiteSpace(regionName))
            return null;

        _regions.TryGetValue(regionName, out var region);
        return region;
    }

    public bool ContainsRegion(string regionName)
    {
        return !string.IsNullOrWhiteSpace(regionName) && _regions.ContainsKey(regionName);
    }

    public void RegisterRegion(IRegion region)
    {
        if (region == null)
            throw new ArgumentNullException(nameof(region));

        // 이미 등록된 Region이면 기존 Region 사용 (중복 등록 허용)
        if (_regions.ContainsKey(region.Name))
            return;

        _regions[region.Name] = region;

        // 각 리전에 대한 네비게이션 서비스 생성
        var navigationService = new RegionNavigationService(region, _viewResolver);
        _navigationServices[region.Name] = navigationService;

        RegionAdded?.Invoke(this, region);

        // RegisterViewWithRegion으로 등록된 View가 있으면 자동으로 네비게이션
        if (_pendingViewRegistrations.TryGetValue(region.Name, out var viewType))
        {
            _pendingViewRegistrations.Remove(region.Name);
            _ = NavigateAsync(region.Name, viewType);
        }
    }

    public bool RemoveRegion(string regionName)
    {
        if (string.IsNullOrWhiteSpace(regionName))
            return false;

        if (!_regions.ContainsKey(regionName))
            return false;

        var region = _regions[regionName];
        region.RemoveAll();

        _regions.Remove(regionName);
        _navigationServices.Remove(regionName);

        RegionRemoved?.Invoke(this, regionName);

        return true;
    }

    public IRegionNavigationService? GetNavigationService(string regionName)
    {
        if (string.IsNullOrWhiteSpace(regionName))
            return null;

        _navigationServices.TryGetValue(regionName, out var service);
        return service;
    }

    #endregion

    #region View 추가/제거

    public object AddToRegion(string regionName, object view)
    {
        if (view == null)
            throw new ArgumentNullException(nameof(view));

        var region = CreateOrGetRegion(regionName);
        return region.Add(view);
    }

    public object AddToRegion(string regionName, Type viewType)
    {
        if (viewType == null)
            throw new ArgumentNullException(nameof(viewType));

        var region = CreateOrGetRegion(regionName);

        // View 생성 (ViewModel 자동 연결)
        var view = _viewResolver.ResolveView(viewType);

        return region.Add(view);
    }

    public object AddToRegion<TView>(string regionName) where TView : class
    {
        return AddToRegion(regionName, typeof(TView));
    }

    public void RemoveFromRegion(string regionName, object view)
    {
        var region = GetRegion(regionName);
        region?.Remove(view);
    }

    #endregion

    #region View 기반 네비게이션

    public Task<bool> NavigateAsync<TView>(string regionName, object? parameter = null,
        CancellationToken cancellationToken = default) where TView : class
    {
        return NavigateAsync(regionName, typeof(TView), parameter, cancellationToken);
    }

    public async Task<bool> NavigateAsync(string regionName, Type viewType, object? parameter = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(regionName))
            return false;

        if (viewType == null)
            return false;

        if (!_navigationServices.TryGetValue(regionName, out var navigationService))
            return false;

        return await navigationService.NavigateAsync(viewType, parameter, cancellationToken);
    }

    #endregion

    #region Back/Forward 네비게이션

    public bool CanGoBack(string regionName)
    {
        return GetNavigationService(regionName)?.CanGoBack ?? false;
    }

    public bool CanGoForward(string regionName)
    {
        return GetNavigationService(regionName)?.CanGoForward ?? false;
    }

    public async Task<bool> GoBackAsync(string regionName, CancellationToken cancellationToken = default)
    {
        var navigationService = GetNavigationService(regionName);
        if (navigationService == null)
            return false;

        return await navigationService.GoBackAsync(cancellationToken);
    }

    public async Task<bool> GoForwardAsync(string regionName, CancellationToken cancellationToken = default)
    {
        var navigationService = GetNavigationService(regionName);
        if (navigationService == null)
            return false;

        return await navigationService.GoForwardAsync(cancellationToken);
    }

    #endregion

    #region RegisterViewWithRegion

    public void RegisterViewWithRegion(string regionName, Type viewType)
    {
        if (string.IsNullOrWhiteSpace(regionName))
            throw new ArgumentException("Region name cannot be null or empty.", nameof(regionName));

        if (viewType == null)
            throw new ArgumentNullException(nameof(viewType));

        // 이미 리전이 존재하면 바로 네비게이션
        if (_regions.ContainsKey(regionName))
        {
            _ = NavigateAsync(regionName, viewType);
        }
        else
        {
            // 리전이 아직 생성되지 않았으면 pending에 저장
            _pendingViewRegistrations[regionName] = viewType;
        }
    }

    public void RegisterViewWithRegion<TView>(string regionName) where TView : class
    {
        RegisterViewWithRegion(regionName, typeof(TView));
    }

    #endregion
}
