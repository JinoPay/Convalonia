namespace Jinobald.Core.Modularity;

/// <summary>
///     모듈 카탈로그 인터페이스
///     애플리케이션에서 사용할 모듈들을 관리합니다.
/// </summary>
public interface IModuleCatalog
{
    /// <summary>
    ///     카탈로그에 등록된 모든 모듈 정보
    /// </summary>
    IEnumerable<ModuleInfo> Modules { get; }

    /// <summary>
    ///     모듈을 카탈로그에 추가합니다.
    /// </summary>
    /// <param name="moduleInfo">추가할 모듈 정보</param>
    /// <returns>현재 카탈로그 (메서드 체이닝용)</returns>
    IModuleCatalog AddModule(ModuleInfo moduleInfo);

    /// <summary>
    ///     모듈 타입으로 카탈로그에 추가합니다.
    /// </summary>
    /// <typeparam name="TModule">모듈 타입</typeparam>
    /// <returns>현재 카탈로그 (메서드 체이닝용)</returns>
    IModuleCatalog AddModule<TModule>() where TModule : IModule;

    /// <summary>
    ///     모듈 타입과 초기화 모드로 카탈로그에 추가합니다.
    /// </summary>
    /// <typeparam name="TModule">모듈 타입</typeparam>
    /// <param name="initializationMode">초기화 모드</param>
    /// <param name="dependsOn">의존하는 모듈 이름들</param>
    /// <returns>현재 카탈로그 (메서드 체이닝용)</returns>
    IModuleCatalog AddModule<TModule>(
        InitializationMode initializationMode,
        params string[] dependsOn) where TModule : IModule;

    /// <summary>
    ///     이름으로 모듈 정보를 조회합니다.
    /// </summary>
    /// <param name="moduleName">모듈 이름</param>
    /// <returns>모듈 정보 (없으면 null)</returns>
    ModuleInfo? GetModule(string moduleName);

    /// <summary>
    ///     모듈이 카탈로그에 존재하는지 확인합니다.
    /// </summary>
    /// <param name="moduleName">모듈 이름</param>
    /// <returns>존재 여부</returns>
    bool ContainsModule(string moduleName);

    /// <summary>
    ///     카탈로그 초기화 (의존성 검증 등)
    /// </summary>
    void Initialize();

    /// <summary>
    ///     지정된 모듈과 그에 의존하는 모든 모듈을 의존성 순서대로 반환합니다.
    /// </summary>
    /// <param name="moduleInfo">시작 모듈</param>
    /// <returns>의존성 순서로 정렬된 모듈 목록</returns>
    IEnumerable<ModuleInfo> GetDependentModules(ModuleInfo moduleInfo);

    /// <summary>
    ///     WhenAvailable 모드인 모든 모듈을 의존성 순서대로 반환합니다.
    /// </summary>
    /// <returns>의존성 순서로 정렬된 모듈 목록</returns>
    IEnumerable<ModuleInfo> GetModulesForInitialization();
}
