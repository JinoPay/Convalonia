namespace Jinobald.Core.Services.Regions;

/// <summary>
///     기본 리전 구현체
///     뷰 컬렉션을 관리하고 활성화/비활성화를 처리합니다.
/// </summary>
public class Region : IRegion
{
    private readonly List<object> _activeViews = new();
    private readonly List<object> _views = new();

    public Region(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Region name cannot be null or empty.", nameof(name));

        Name = name;
    }

    public string Name { get; }

    public object? RegionTarget { get; set; }

    public IEnumerable<object> Views => _views.AsReadOnly();

    public IEnumerable<object> ActiveViews => _activeViews.AsReadOnly();

    public ViewSortHint SortHint { get; set; } = ViewSortHint.Default;

    public event EventHandler<object>? ViewAdded;
    public event EventHandler<object>? ViewRemoved;
    public event EventHandler<object>? ViewActivated;
    public event EventHandler<object>? ViewDeactivated;

    public object Add(object view)
    {
        if (view == null)
            throw new ArgumentNullException(nameof(view));

        if (_views.Contains(view))
            return view;

        if (SortHint == ViewSortHint.Reverse)
            _views.Insert(0, view);
        else
            _views.Add(view);

        ViewAdded?.Invoke(this, view);

        return view;
    }

    public void Remove(object view)
    {
        if (view == null)
            throw new ArgumentNullException(nameof(view));

        if (!_views.Contains(view))
            return;

        if (_activeViews.Contains(view))
            Deactivate(view);

        _views.Remove(view);
        ViewRemoved?.Invoke(this, view);
    }

    public void RemoveAll()
    {
        var viewsToRemove = _views.ToList();
        foreach (var view in viewsToRemove) Remove(view);
    }

    public void Activate(object view)
    {
        if (view == null)
            throw new ArgumentNullException(nameof(view));

        if (!_views.Contains(view))
            throw new InvalidOperationException("Cannot activate a view that is not in the region.");

        if (_activeViews.Contains(view))
            return;

        _activeViews.Add(view);
        ViewActivated?.Invoke(this, view);
    }

    public void Deactivate(object view)
    {
        if (view == null)
            throw new ArgumentNullException(nameof(view));

        if (!_activeViews.Contains(view))
            return;

        _activeViews.Remove(view);
        ViewDeactivated?.Invoke(this, view);
    }

    public bool Contains(object view)
    {
        return _views.Contains(view);
    }
}
