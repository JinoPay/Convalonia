using System.Collections.Specialized;
using System.Windows.Controls;
using Jinobald.Core.Services.Regions;

namespace Jinobald.Wpf.Services.Regions;

/// <summary>
///     ItemsControl을 리전으로 어댑트합니다.
///     리전의 모든 활성화된 뷰를 ItemsControl.Items에 표시합니다.
/// </summary>
public class ItemsControlRegionAdapter : RegionAdapterBase<ItemsControl>
{
    protected override void Adapt(IRegion region, ItemsControl control)
    {
        if (region == null)
            throw new ArgumentNullException(nameof(region));
        if (control == null)
            throw new ArgumentNullException(nameof(control));

        // 활성화된 뷰를 Items에 추가
        region.ViewActivated += (_, view) =>
        {
            if (!control.Items.Contains(view)) control.Items.Add(view);
        };

        // 비활성화된 뷰를 Items에서 제거
        region.ViewDeactivated += (_, view) =>
        {
            if (control.Items.Contains(view)) control.Items.Remove(view);
        };

        // 뷰가 제거되면 Items에서도 제거
        region.ViewRemoved += (_, view) =>
        {
            if (control.Items.Contains(view)) control.Items.Remove(view);
        };
    }
}
