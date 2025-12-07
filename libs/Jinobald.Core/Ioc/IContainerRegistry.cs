namespace Jinobald.Core.Ioc;

/// <summary>
///     DI 컨테이너에 서비스를 등록하는 인터페이스
/// </summary>
public interface IContainerRegistry
{
    /// <summary>
    ///     싱글톤으로 서비스를 등록합니다.
    /// </summary>
    /// <param name="from">서비스 인터페이스 타입</param>
    /// <param name="to">구현 타입</param>
    /// <returns>현재 레지스트리</returns>
    IContainerRegistry RegisterSingleton(Type from, Type to);

    /// <summary>
    ///     싱글톤으로 서비스를 등록합니다.
    /// </summary>
    /// <typeparam name="TFrom">서비스 인터페이스 타입</typeparam>
    /// <typeparam name="TTo">구현 타입</typeparam>
    /// <returns>현재 레지스트리</returns>
    IContainerRegistry RegisterSingleton<TFrom, TTo>() where TFrom : class where TTo : class, TFrom;

    /// <summary>
    ///     싱글톤으로 서비스를 등록합니다.
    /// </summary>
    /// <param name="type">서비스 타입</param>
    /// <returns>현재 레지스트리</returns>
    IContainerRegistry RegisterSingleton(Type type);

    /// <summary>
    ///     싱글톤으로 서비스를 등록합니다.
    /// </summary>
    /// <typeparam name="T">서비스 타입</typeparam>
    /// <returns>현재 레지스트리</returns>
    IContainerRegistry RegisterSingleton<T>() where T : class;

    /// <summary>
    ///     싱글톤 인스턴스를 등록합니다.
    /// </summary>
    /// <param name="type">서비스 타입</param>
    /// <param name="instance">인스턴스</param>
    /// <returns>현재 레지스트리</returns>
    IContainerRegistry RegisterInstance(Type type, object instance);

    /// <summary>
    ///     싱글톤 인스턴스를 등록합니다.
    /// </summary>
    /// <typeparam name="T">서비스 타입</typeparam>
    /// <param name="instance">인스턴스</param>
    /// <returns>현재 레지스트리</returns>
    IContainerRegistry RegisterInstance<T>(T instance) where T : class;

    /// <summary>
    ///     트랜지언트로 서비스를 등록합니다.
    /// </summary>
    /// <param name="from">서비스 인터페이스 타입</param>
    /// <param name="to">구현 타입</param>
    /// <returns>현재 레지스트리</returns>
    IContainerRegistry Register(Type from, Type to);

    /// <summary>
    ///     트랜지언트로 서비스를 등록합니다.
    /// </summary>
    /// <typeparam name="TFrom">서비스 인터페이스 타입</typeparam>
    /// <typeparam name="TTo">구현 타입</typeparam>
    /// <returns>현재 레지스트리</returns>
    IContainerRegistry Register<TFrom, TTo>() where TFrom : class where TTo : class, TFrom;

    /// <summary>
    ///     트랜지언트로 서비스를 등록합니다.
    /// </summary>
    /// <param name="type">서비스 타입</param>
    /// <returns>현재 레지스트리</returns>
    IContainerRegistry Register(Type type);

    /// <summary>
    ///     트랜지언트로 서비스를 등록합니다.
    /// </summary>
    /// <typeparam name="T">서비스 타입</typeparam>
    /// <returns>현재 레지스트리</returns>
    IContainerRegistry Register<T>() where T : class;

    /// <summary>
    ///     스코프드로 서비스를 등록합니다.
    /// </summary>
    /// <param name="from">서비스 인터페이스 타입</param>
    /// <param name="to">구현 타입</param>
    /// <returns>현재 레지스트리</returns>
    IContainerRegistry RegisterScoped(Type from, Type to);

    /// <summary>
    ///     스코프드로 서비스를 등록합니다.
    /// </summary>
    /// <typeparam name="TFrom">서비스 인터페이스 타입</typeparam>
    /// <typeparam name="TTo">구현 타입</typeparam>
    /// <returns>현재 레지스트리</returns>
    IContainerRegistry RegisterScoped<TFrom, TTo>() where TFrom : class where TTo : class, TFrom;

    /// <summary>
    ///     스코프드로 서비스를 등록합니다.
    /// </summary>
    /// <param name="type">서비스 타입</param>
    /// <returns>현재 레지스트리</returns>
    IContainerRegistry RegisterScoped(Type type);

    /// <summary>
    ///     스코프드로 서비스를 등록합니다.
    /// </summary>
    /// <typeparam name="T">서비스 타입</typeparam>
    /// <returns>현재 레지스트리</returns>
    IContainerRegistry RegisterScoped<T>() where T : class;
}
