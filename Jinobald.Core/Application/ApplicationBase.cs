using Jinobald.Core.Ioc;
using Jinobald.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Jinobald.Core.Application;

/// <summary>
///     플랫폼 독립적인 애플리케이션 기본 클래스
///     WPF와 Avalonia 애플리케이션이 이 클래스를 상속받아 구현합니다.
/// </summary>
public abstract class ApplicationBase
{
    /// <summary>
    ///     DI 컨테이너
    /// </summary>
    protected IContainerExtension? Container { get; private set; }

    /// <summary>
    ///     스플래시 화면
    /// </summary>
    protected ISplashScreen? SplashScreen { get; private set; }

    /// <summary>
    ///     모듈 카탈로그
    /// </summary>
    protected IModuleCatalog? ModuleCatalog { get; private set; }

    /// <summary>
    ///     모듈 매니저
    /// </summary>
    protected IModuleManager? ModuleManager { get; private set; }

    /// <summary>
    ///     로거
    /// </summary>
    protected ILogger Logger { get; }

    protected ApplicationBase()
    {
        Logger = Log.ForContext(GetType());
        ConfigureExceptionHandling();
    }

    /// <summary>
    ///     애플리케이션 초기화
    ///     이 메서드는 플랫폼별 애플리케이션 시작점에서 호출되어야 합니다.
    /// </summary>
    public async Task InitializeAsync()
    {
        Logger.Information("애플리케이션 초기화 시작");

        try
        {
            // 1. 로거 설정
            ConfigureLogging();

            // 2. 스플래시 화면 생성 및 표시
            SplashScreen = CreateSplashScreen();
            if (SplashScreen == null)
                throw new InvalidOperationException("스플래시 화면을 생성할 수 없습니다. CreateSplashScreen()을 구현하세요.");

            SplashScreen.Show();
            SplashScreen.UpdateProgress("서비스 초기화 중...", null);

            // 3. DI 컨테이너 생성 및 설정
            var services = new ServiceCollection();
            ConfigureServices(services);
            SplashScreen.UpdateProgress("서비스 등록 중...", 30);

            // 4. 컨테이너 빌드
            Container = services.AsContainerExtension();
            Container.FinalizeExtension();
            ContainerLocator.SetContainerExtension(Container);
            SplashScreen.UpdateProgress("서비스 구성 완료", 40);

            // 5. 모듈 카탈로그 구성
            ModuleCatalog = CreateModuleCatalog();
            ConfigureModuleCatalog(ModuleCatalog);
            SplashScreen.UpdateProgress("모듈 카탈로그 구성 완료", 50);

            // 6. 모듈 초기화
            ModuleManager = CreateModuleManager();
            InitializeModules();
            SplashScreen.UpdateProgress("모듈 초기화 완료", 60);

            // 7. 애플리케이션별 초기화
            await OnInitializeAsync();
            SplashScreen.UpdateProgress("애플리케이션 구성 중...", 70);

            // 8. 메인 윈도우 생성
            await CreateAndShowMainWindowAsync();
            SplashScreen.UpdateProgress("메인 화면 로드 중...", 90);

            // 9. 스플래시 화면 닫기
            await Task.Delay(500); // 사용자가 진행 상황을 볼 수 있도록 짧은 지연
            SplashScreen.Close();

            Logger.Information("애플리케이션 초기화 완료");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "애플리케이션 초기화 중 오류 발생");
            SplashScreen?.Close();
            throw;
        }
    }

    /// <summary>
    ///     로거 설정
    ///     파생 클래스에서 Serilog 설정을 커스터마이즈할 수 있습니다.
    /// </summary>
    protected virtual void ConfigureLogging()
    {
        if (Log.Logger == Serilog.Core.Logger.None)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/jinobald-.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
    }

    /// <summary>
    ///     DI 컨테이너에 서비스를 등록합니다.
    ///     파생 클래스에서 반드시 구현해야 합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    protected abstract void ConfigureServices(IServiceCollection services);

    /// <summary>
    ///     스플래시 화면을 생성합니다.
    ///     파생 클래스에서 반드시 구현해야 합니다.
    /// </summary>
    /// <returns>스플래시 화면 인스턴스</returns>
    protected abstract ISplashScreen CreateSplashScreen();

    /// <summary>
    ///     메인 윈도우를 생성하고 표시합니다.
    ///     파생 클래스에서 반드시 구현해야 합니다.
    /// </summary>
    protected abstract Task CreateAndShowMainWindowAsync();

    /// <summary>
    ///     애플리케이션별 초기화 로직
    ///     파생 클래스에서 필요시 오버라이드합니다.
    /// </summary>
    protected virtual Task OnInitializeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     전역 예외 처리 설정
    /// </summary>
    protected virtual void ConfigureExceptionHandling()
    {
        // AppDomain 예외 처리
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // Task 예외 처리
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    /// <summary>
    ///     처리되지 않은 예외 발생 시 호출
    /// </summary>
    protected virtual void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Logger.Fatal(ex, "처리되지 않은 예외 발생");

            // 파생 클래스에서 추가 처리 가능 (예: 사용자에게 알림)
            HandleUnhandledException(ex, e.IsTerminating);
        }
    }

    /// <summary>
    ///     관찰되지 않은 Task 예외 발생 시 호출
    /// </summary>
    protected virtual void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Logger.Error(e.Exception, "관찰되지 않은 Task 예외 발생");

        // 예외를 관찰된 것으로 표시 (앱 종료 방지)
        e.SetObserved();

        // 파생 클래스에서 추가 처리 가능
        HandleUnobservedTaskException(e.Exception);
    }

    /// <summary>
    ///     파생 클래스에서 처리되지 않은 예외 처리를 커스터마이즈합니다.
    /// </summary>
    /// <param name="exception">발생한 예외</param>
    /// <param name="isTerminating">앱이 종료 중인지 여부</param>
    protected virtual void HandleUnhandledException(Exception exception, bool isTerminating)
    {
        // 기본 구현: 로깅만 수행
        // 파생 클래스에서 사용자에게 에러 다이얼로그 표시 등 추가 가능
    }

    /// <summary>
    ///     파생 클래스에서 관찰되지 않은 Task 예외 처리를 커스터마이즈합니다.
    /// </summary>
    /// <param name="exception">발생한 예외</param>
    protected virtual void HandleUnobservedTaskException(Exception exception)
    {
        // 기본 구현: 로깅만 수행
        // 파생 클래스에서 추가 처리 가능
    }

    /// <summary>
    ///     모듈 카탈로그를 생성합니다.
    ///     파생 클래스에서 오버라이드하여 커스텀 카탈로그를 사용할 수 있습니다.
    /// </summary>
    /// <returns>모듈 카탈로그</returns>
    protected virtual IModuleCatalog CreateModuleCatalog()
    {
        return new ModuleCatalog();
    }

    /// <summary>
    ///     모듈 카탈로그에 모듈을 등록합니다.
    ///     파생 클래스에서 오버라이드하여 모듈을 추가합니다.
    /// </summary>
    /// <param name="moduleCatalog">모듈 카탈로그</param>
    protected virtual void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        // 기본 구현: 빈 카탈로그
        // 파생 클래스에서 모듈 등록:
        // moduleCatalog.AddModule<MyModule>();
    }

    /// <summary>
    ///     모듈 매니저를 생성합니다.
    ///     파생 클래스에서 오버라이드하여 커스텀 매니저를 사용할 수 있습니다.
    /// </summary>
    /// <returns>모듈 매니저</returns>
    protected virtual IModuleManager CreateModuleManager()
    {
        if (Container == null)
            throw new InvalidOperationException("Container must be initialized before creating ModuleManager.");

        if (ModuleCatalog == null)
            throw new InvalidOperationException("ModuleCatalog must be created before creating ModuleManager.");

        return new ModuleManager(ModuleCatalog, Container, Container);
    }

    /// <summary>
    ///     모듈을 초기화합니다.
    /// </summary>
    protected virtual void InitializeModules()
    {
        ModuleManager?.Run();
    }

    /// <summary>
    ///     애플리케이션 종료 시 호출
    /// </summary>
    public virtual void OnExit()
    {
        Logger.Information("애플리케이션 종료");

        // 이벤트 핸들러 제거
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

        Log.CloseAndFlush();
    }
}
