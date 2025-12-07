namespace Jinobald.Core.Modularity;

/// <summary>
///     모듈 메타데이터 정보
/// </summary>
public class ModuleInfo
{
    /// <summary>
    ///     모듈 이름 (고유 식별자)
    /// </summary>
    public string ModuleName { get; }

    /// <summary>
    ///     모듈 타입
    /// </summary>
    public Type ModuleType { get; }

    /// <summary>
    ///     모듈 초기화 모드
    /// </summary>
    public InitializationMode InitializationMode { get; set; } = InitializationMode.WhenAvailable;

    /// <summary>
    ///     이 모듈이 의존하는 다른 모듈들의 이름
    /// </summary>
    public IList<string> DependsOn { get; } = new List<string>();

    /// <summary>
    ///     모듈의 현재 상태
    /// </summary>
    public ModuleState State { get; internal set; } = ModuleState.NotLoaded;

    /// <summary>
    ///     초기화 실패 시 발생한 예외
    /// </summary>
    public Exception? InitializationException { get; internal set; }

    /// <summary>
    ///     ModuleInfo 생성자
    /// </summary>
    /// <param name="moduleName">모듈 이름</param>
    /// <param name="moduleType">모듈 타입 (IModule 구현체)</param>
    public ModuleInfo(string moduleName, Type moduleType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);
        ArgumentNullException.ThrowIfNull(moduleType);

        if (!typeof(IModule).IsAssignableFrom(moduleType))
            throw new ArgumentException($"Type {moduleType.FullName} does not implement IModule.", nameof(moduleType));

        ModuleName = moduleName;
        ModuleType = moduleType;
    }

    /// <summary>
    ///     ModuleInfo 생성자 (타입 이름을 모듈 이름으로 사용)
    /// </summary>
    /// <param name="moduleType">모듈 타입</param>
    public ModuleInfo(Type moduleType)
        : this(moduleType?.Name ?? throw new ArgumentNullException(nameof(moduleType)), moduleType)
    {
    }

    /// <summary>
    ///     제네릭 방식으로 ModuleInfo 생성
    /// </summary>
    public static ModuleInfo Create<TModule>() where TModule : IModule
    {
        return new ModuleInfo(typeof(TModule));
    }

    /// <summary>
    ///     제네릭 방식으로 ModuleInfo 생성 (이름 지정)
    /// </summary>
    public static ModuleInfo Create<TModule>(string moduleName) where TModule : IModule
    {
        return new ModuleInfo(moduleName, typeof(TModule));
    }
}
