using Jinobald.Core.Ioc;
using Jinobald.Core.Services.Regions;

namespace Jinobald.Core.Application;

/// <summary>
///     애플리케이션 라이프사이클 인터페이스
///     Avalonia와 WPF ApplicationBase가 구현합니다.
/// </summary>
public interface IApplicationLifecycle
{
    /// <summary>
    ///     DI 컨테이너에 서비스를 등록합니다.
    /// </summary>
    void RegisterTypes(IContainerRegistry containerRegistry);

    /// <summary>
    ///     Region에 기본 View를 등록합니다.
    /// </summary>
    void ConfigureRegions(IRegionManager regionManager);

    /// <summary>
    ///     애플리케이션별 초기화 로직 (스플래시 없는 버전)
    /// </summary>
    Task OnInitializeAsync();

    /// <summary>
    ///     애플리케이션별 초기화 로직 (스플래시 있는 버전 - 진행률 보고)
    /// </summary>
    /// <param name="progress">진행률 보고 콜백 (메시지, 퍼센트 0-100)</param>
    Task OnInitializeAsync(IProgress<InitializationProgress> progress);
}

/// <summary>
///     초기화 진행률 정보
/// </summary>
/// <param name="Message">진행 상태 메시지</param>
/// <param name="Percent">진행률 (0-100), null이면 무한(indeterminate) 진행 표시</param>
public readonly record struct InitializationProgress(string Message, int? Percent);
