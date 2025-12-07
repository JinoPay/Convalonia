using Jinobald.Core.Services.Regions;
using CoreRegion = Jinobald.Core.Services.Regions.Region;

namespace Jinobald.Avalonia.Services.Regions;

/// <summary>
///     UI 컨트롤을 리전으로 어댑트하는 기본 클래스
/// </summary>
/// <typeparam name="T">UI 컨트롤 타입</typeparam>
public abstract class RegionAdapterBase<T> where T : class
{
    /// <summary>
    ///     리전을 초기화하고 UI 컨트롤에 연결합니다.
    /// </summary>
    public IRegion Initialize(T control, string regionName)
    {
        if (control == null)
            throw new ArgumentNullException(nameof(control));
        if (string.IsNullOrWhiteSpace(regionName))
            throw new ArgumentException("Region name cannot be null or empty.", nameof(regionName));

        var region = CreateRegion(regionName);
        region.RegionTarget = control;

        Adapt(region, control);
        AttachBehaviors(region, control);

        return region;
    }

    /// <summary>
    ///     리전 인스턴스를 생성합니다.
    /// </summary>
    protected virtual CoreRegion CreateRegion(string regionName)
    {
        return new CoreRegion(regionName);
    }

    /// <summary>
    ///     리전을 UI 컨트롤에 어댑트합니다.
    /// </summary>
    protected abstract void Adapt(IRegion region, T control);

    /// <summary>
    ///     리전에 동작을 추가합니다.
    /// </summary>
    protected virtual void AttachBehaviors(IRegion region, T control)
    {
    }
}
