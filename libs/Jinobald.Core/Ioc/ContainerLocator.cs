namespace Jinobald.Core.Ioc;

/// <summary>
///     컨테이너에 대한 정적 접근을 제공하는 로케이터
///     Prism 스타일의 ContainerLocator.Current 패턴
/// </summary>
public static class ContainerLocator
{
    private static IContainerExtension? _current;
    private static readonly object _lock = new();

    /// <summary>
    ///     현재 컨테이너 인스턴스
    /// </summary>
    /// <exception cref="InvalidOperationException">컨테이너가 설정되지 않은 경우</exception>
    public static IContainerExtension Current
    {
        get
        {
            if (_current == null)
                throw new InvalidOperationException(
                    "컨테이너가 설정되지 않았습니다. SetContainerExtension()을 먼저 호출하세요.");

            return _current;
        }
    }

    /// <summary>
    ///     컨테이너가 설정되었는지 여부
    /// </summary>
    public static bool IsSet => _current != null;

    /// <summary>
    ///     컨테이너 확장을 설정합니다.
    /// </summary>
    /// <param name="containerExtension">설정할 컨테이너 확장</param>
    public static void SetContainerExtension(IContainerExtension containerExtension)
    {
        lock (_lock)
        {
            if (_current != null)
                throw new InvalidOperationException("컨테이너가 이미 설정되었습니다.");

            _current = containerExtension ?? throw new ArgumentNullException(nameof(containerExtension));
        }
    }

    /// <summary>
    ///     컨테이너를 재설정합니다. (주로 테스트용)
    /// </summary>
    public static void ResetContainer()
    {
        lock (_lock)
        {
            _current = null;
        }
    }
}
