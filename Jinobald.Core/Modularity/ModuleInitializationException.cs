namespace Jinobald.Core.Modularity;

/// <summary>
///     모듈 초기화 실패 시 발생하는 예외
/// </summary>
public class ModuleInitializationException : Exception
{
    /// <summary>
    ///     실패한 모듈 이름
    /// </summary>
    public string ModuleName { get; }

    public ModuleInitializationException(string moduleName)
        : base($"An error occurred while initializing module '{moduleName}'.")
    {
        ModuleName = moduleName;
    }

    public ModuleInitializationException(string moduleName, Exception innerException)
        : base($"An error occurred while initializing module '{moduleName}'.", innerException)
    {
        ModuleName = moduleName;
    }

    public ModuleInitializationException(string moduleName, string message)
        : base(message)
    {
        ModuleName = moduleName;
    }

    public ModuleInitializationException(string moduleName, string message, Exception innerException)
        : base(message, innerException)
    {
        ModuleName = moduleName;
    }
}
