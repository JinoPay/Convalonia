using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;

namespace Jinobald.Core.Mvvm;

/// <summary>
///     모든 ViewModel의 기본 클래스
///     CommunityToolkit.Mvvm의 ObservableObject를 상속하고 추가 기능을 제공합니다.
///     기본적으로 IInitializableAsync, IActivatable, IDestructible, IDisposable 인터페이스를 구현합니다.
/// </summary>
/// <remarks>
///     IDisposable 패턴을 구현하여 using 문이나 DI 컨테이너의 자동 Dispose를 지원합니다.
///     Dispose() 호출 시 Destroy()도 함께 호출됩니다.
/// </remarks>
public abstract class ViewModelBase : ObservableObject,
    IInitializableAsync,
    IActivatable,
    IDestructible,
    IDisposable
{
    private bool _disposed;

    /// <summary>
    ///     로거 인스턴스
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    ///     ViewModel이 초기화되었는지 여부
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    ///     ViewModel이 활성화되었는지 여부
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    ///     리소스가 해제되었는지 여부
    /// </summary>
    public bool IsDisposed => _disposed;

    protected ViewModelBase()
    {
        Logger = Log.ForContext(GetType());
    }

    #region IInitializableAsync 구현

    /// <summary>
    ///     ViewModel 초기화 (비동기)
    ///     NavigationService에 의해 자동 호출됩니다.
    /// </summary>
    async Task IInitializableAsync.InitializeAsync(CancellationToken cancellationToken)
    {
        if (IsInitialized)
            return;

        await OnInitializeAsync(cancellationToken);
        IsInitialized = true;
        Logger.Debug("{ViewModelName} 초기화됨", GetType().Name);
    }

    /// <summary>
    ///     파생 클래스에서 초기화 로직을 구현합니다.
    /// </summary>
    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    #endregion

    #region IActivatable 구현

    /// <summary>
    ///     ViewModel 활성화
    ///     NavigationService에 의해 자동 호출됩니다.
    /// </summary>
    async Task IActivatable.ActivateAsync(CancellationToken cancellationToken)
    {
        await OnActivateAsync(cancellationToken);
        IsActive = true;
        Logger.Debug("{ViewModelName} 활성화됨", GetType().Name);
    }

    /// <summary>
    ///     ViewModel 비활성화
    ///     NavigationService에 의해 자동 호출됩니다.
    /// </summary>
    async Task IActivatable.DeactivateAsync(CancellationToken cancellationToken)
    {
        await OnDeactivateAsync(cancellationToken);
        IsActive = false;
        Logger.Debug("{ViewModelName} 비활성화됨", GetType().Name);
    }

    /// <summary>
    ///     파생 클래스에서 활성화 로직을 구현합니다.
    /// </summary>
    protected virtual Task OnActivateAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     파생 클래스에서 비활성화 로직을 구현합니다.
    /// </summary>
    protected virtual Task OnDeactivateAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    #endregion

    #region IDestructible 구현

    /// <summary>
    ///     리소스 정리
    ///     NavigationService에 의해 자동 호출됩니다.
    ///     내부적으로 Dispose(true)를 호출합니다.
    /// </summary>
    public void Destroy()
    {
        Dispose(true);
    }

    /// <summary>
    ///     파생 클래스에서 정리 로직을 구현합니다.
    ///     관리되는 리소스와 비관리 리소스 모두 정리할 수 있습니다.
    /// </summary>
    /// <param name="disposing">
    ///     true이면 Dispose()에서 호출됨 (관리 리소스 정리 가능),
    ///     false이면 Finalizer에서 호출됨 (비관리 리소스만 정리)
    /// </param>
    protected virtual void OnDestroy(bool disposing)
    {
        // 파생 클래스에서 override하여 리소스 정리
        // disposing이 true이면 관리 리소스도 정리 가능
        // 예: _subscription?.Dispose(); _timer?.Dispose();
    }

    /// <summary>
    ///     파생 클래스에서 정리 로직을 구현합니다. (하위 호환성 유지)
    /// </summary>
    [Obsolete("Use OnDestroy(bool disposing) instead for proper dispose pattern")]
    protected virtual void OnDestroy()
    {
    }

    #endregion

    #region IDisposable 구현

    /// <summary>
    ///     리소스 해제
    ///     using 문 또는 DI 컨테이너에서 자동 호출됩니다.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     리소스 해제 (Dispose 패턴)
    /// </summary>
    /// <param name="disposing">
    ///     true이면 Dispose()에서 호출됨,
    ///     false이면 Finalizer에서 호출됨
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // 관리되는 리소스 정리
            OnDestroy(disposing);

#pragma warning disable CS0618 // Obsolete warning 억제 (하위 호환성)
            OnDestroy();
#pragma warning restore CS0618

            Logger.Debug("{ViewModelName} Disposed", GetType().Name);
        }

        _disposed = true;
    }

    /// <summary>
    ///     리소스 해제 확인 헬퍼
    /// </summary>
    /// <exception cref="ObjectDisposedException">이미 Dispose된 경우</exception>
    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    #endregion
}
