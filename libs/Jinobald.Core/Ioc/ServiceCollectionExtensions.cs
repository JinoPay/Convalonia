using Microsoft.Extensions.DependencyInjection;

namespace Jinobald.Core.Ioc;

/// <summary>
///     IServiceCollection에 대한 확장 메서드
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     IServiceCollection을 IContainerExtension으로 변환합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>컨테이너 확장</returns>
    public static IContainerExtension AsContainerExtension(this IServiceCollection services)
    {
        return new MicrosoftDependencyInjectionExtension(services);
    }

    /// <summary>
    ///     IServiceCollection에서 컨테이너를 생성하고 ContainerLocator에 설정합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>컨테이너 확장</returns>
    public static IContainerExtension BuildContainer(this IServiceCollection services)
    {
        var container = services.AsContainerExtension();
        container.FinalizeExtension();
        ContainerLocator.SetContainerExtension(container);
        return container;
    }
}
