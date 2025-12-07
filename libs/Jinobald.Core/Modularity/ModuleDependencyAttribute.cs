namespace Jinobald.Core.Modularity;

/// <summary>
///     모듈 의존성을 선언적으로 지정하는 속성
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ModuleDependencyAttribute : Attribute
{
    /// <summary>
    ///     의존하는 모듈 이름
    /// </summary>
    public string ModuleName { get; }

    /// <summary>
    ///     ModuleDependencyAttribute 생성자
    /// </summary>
    /// <param name="moduleName">의존하는 모듈 이름</param>
    public ModuleDependencyAttribute(string moduleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);
        ModuleName = moduleName;
    }

    /// <summary>
    ///     타입에서 모든 의존성 속성을 가져옵니다.
    /// </summary>
    /// <param name="moduleType">모듈 타입</param>
    /// <returns>의존 모듈 이름 목록</returns>
    public static IEnumerable<string> GetDependencies(Type moduleType)
    {
        ArgumentNullException.ThrowIfNull(moduleType);

        return moduleType
            .GetCustomAttributes(typeof(ModuleDependencyAttribute), false)
            .Cast<ModuleDependencyAttribute>()
            .Select(a => a.ModuleName);
    }
}
