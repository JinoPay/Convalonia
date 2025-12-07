namespace Jinobald.Core.Ioc;

/// <summary>
///     DI 컨테이너에서 서비스를 해결(Resolve)하는 인터페이스
/// </summary>
public interface IContainerProvider
{
    /// <summary>
    ///     지정된 타입의 서비스를 해결합니다.
    /// </summary>
    /// <param name="serviceType">해결할 서비스 타입</param>
    /// <returns>해결된 서비스 인스턴스</returns>
    object Resolve(Type serviceType);

    /// <summary>
    ///     지정된 타입의 서비스를 해결합니다.
    /// </summary>
    /// <typeparam name="T">해결할 서비스 타입</typeparam>
    /// <returns>해결된 서비스 인스턴스</returns>
    T Resolve<T>() where T : notnull;

    /// <summary>
    ///     이름이 지정된 서비스를 해결합니다.
    /// </summary>
    /// <param name="serviceType">해결할 서비스 타입</param>
    /// <param name="name">서비스 이름</param>
    /// <returns>해결된 서비스 인스턴스</returns>
    object Resolve(Type serviceType, string name);

    /// <summary>
    ///     이름이 지정된 서비스를 해결합니다.
    /// </summary>
    /// <typeparam name="T">해결할 서비스 타입</typeparam>
    /// <param name="name">서비스 이름</param>
    /// <returns>해결된 서비스 인스턴스</returns>
    T Resolve<T>(string name) where T : notnull;

    /// <summary>
    ///     파라미터와 함께 서비스를 해결합니다.
    /// </summary>
    /// <param name="serviceType">해결할 서비스 타입</param>
    /// <param name="parameters">생성자 파라미터</param>
    /// <returns>해결된 서비스 인스턴스</returns>
    object Resolve(Type serviceType, params (Type Type, object Instance)[] parameters);

    /// <summary>
    ///     파라미터와 함께 서비스를 해결합니다.
    /// </summary>
    /// <typeparam name="T">해결할 서비스 타입</typeparam>
    /// <param name="parameters">생성자 파라미터</param>
    /// <returns>해결된 서비스 인스턴스</returns>
    T Resolve<T>(params (Type Type, object Instance)[] parameters) where T : notnull;
}
