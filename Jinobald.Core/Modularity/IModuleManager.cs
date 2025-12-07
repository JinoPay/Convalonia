namespace Jinobald.Core.Modularity;

/// <summary>
///     모듈 매니저 인터페이스
///     모듈의 로딩과 초기화를 담당합니다.
/// </summary>
public interface IModuleManager
{
    /// <summary>
    ///     모듈 초기화 완료 시 발생하는 이벤트
    /// </summary>
    event EventHandler<ModuleInitializedEventArgs>? ModuleInitialized;

    /// <summary>
    ///     모듈 초기화 실패 시 발생하는 이벤트
    /// </summary>
    event EventHandler<ModuleInitializationFailedEventArgs>? ModuleInitializationFailed;

    /// <summary>
    ///     모든 WhenAvailable 모듈을 초기화합니다.
    /// </summary>
    void Run();

    /// <summary>
    ///     특정 모듈을 로드하고 초기화합니다.
    /// </summary>
    /// <param name="moduleName">모듈 이름</param>
    void LoadModule(string moduleName);

    /// <summary>
    ///     특정 모듈을 로드하고 초기화합니다.
    /// </summary>
    /// <typeparam name="TModule">모듈 타입</typeparam>
    void LoadModule<TModule>() where TModule : IModule;

    /// <summary>
    ///     모듈이 초기화되었는지 확인합니다.
    /// </summary>
    /// <param name="moduleName">모듈 이름</param>
    /// <returns>초기화 여부</returns>
    bool IsModuleInitialized(string moduleName);
}

/// <summary>
///     모듈 초기화 완료 이벤트 인자
/// </summary>
public class ModuleInitializedEventArgs : EventArgs
{
    /// <summary>
    ///     초기화된 모듈 정보
    /// </summary>
    public ModuleInfo ModuleInfo { get; }

    public ModuleInitializedEventArgs(ModuleInfo moduleInfo)
    {
        ModuleInfo = moduleInfo;
    }
}

/// <summary>
///     모듈 초기화 실패 이벤트 인자
/// </summary>
public class ModuleInitializationFailedEventArgs : EventArgs
{
    /// <summary>
    ///     실패한 모듈 정보
    /// </summary>
    public ModuleInfo ModuleInfo { get; }

    /// <summary>
    ///     발생한 예외
    /// </summary>
    public Exception Exception { get; }

    public ModuleInitializationFailedEventArgs(ModuleInfo moduleInfo, Exception exception)
    {
        ModuleInfo = moduleInfo;
        Exception = exception;
    }
}
