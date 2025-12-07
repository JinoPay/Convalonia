using System.Windows.Controls;
using Jinobald.Core.Services.Regions;

namespace Jinobald.Wpf.Services.Regions;

/// <summary>
///     ContentControl을 리전으로 어댑트합니다.
///     활성화된 마지막 뷰를 ContentControl.Content에 표시합니다.
/// </summary>
public class ContentControlRegionAdapter : RegionAdapterBase<ContentControl>
{
    protected override void Adapt(IRegion region, ContentControl control)
    {
        if (region == null)
            throw new ArgumentNullException(nameof(region));
        if (control == null)
            throw new ArgumentNullException(nameof(control));

        // 활성화된 뷰를 Content에 표시
        region.ViewActivated += (_, view) => { control.Content = view; };

        // 뷰가 비활성화되면 Content에서 제거
        region.ViewDeactivated += (_, view) =>
        {
            if (control.Content == view) control.Content = null;
        };
    }
}
