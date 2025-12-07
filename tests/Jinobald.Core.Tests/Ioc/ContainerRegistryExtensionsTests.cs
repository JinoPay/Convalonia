using Jinobald.Core.Ioc;
using Jinobald.Core.Services.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Jinobald.Core.Tests.Ioc;

/// <summary>
///     테스트용 설정 클래스
/// </summary>
public class TestAppSettings
{
    public string Theme { get; set; } = "Light";
    public int FontSize { get; set; } = 14;
    public DatabaseSettings Database { get; set; } = new();
}

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = "localhost";
    public int Timeout { get; set; } = 30;
}

public class ContainerRegistryExtensionsTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly List<string> _createdFiles = new();

    public ContainerRegistryExtensionsTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"jinobald_ext_test_{Guid.NewGuid()}.json");
    }

    public void Dispose()
    {
        // 테스트 중 생성된 모든 파일 삭제
        foreach (var file in _createdFiles)
        {
            if (File.Exists(file))
                File.Delete(file);
        }

        if (File.Exists(_testFilePath))
            File.Delete(_testFilePath);

        // 기본 경로 파일도 삭제
        var defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Jinobald",
            "testappsettings.json");
        if (File.Exists(defaultPath))
            File.Delete(defaultPath);
    }

    [Fact]
    public void RegisterSettings_ShouldRegisterTypedSettingsService()
    {
        // Arrange
        var services = new ServiceCollection();
        var containerExtension = new MicrosoftDependencyInjectionExtension(services);

        // Act
        containerExtension.RegisterSettings<TestAppSettings>();
        var provider = services.BuildServiceProvider();
        var settingsService = provider.GetService<ITypedSettingsService<TestAppSettings>>();

        // Assert
        Assert.NotNull(settingsService);
        Assert.IsType<JsonTypedSettingsService<TestAppSettings>>(settingsService);
    }

    [Fact]
    public void RegisterSettings_ShouldRegisterAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var containerExtension = new MicrosoftDependencyInjectionExtension(services);

        // Act
        containerExtension.RegisterSettings<TestAppSettings>();
        var provider = services.BuildServiceProvider();

        var instance1 = provider.GetService<ITypedSettingsService<TestAppSettings>>();
        var instance2 = provider.GetService<ITypedSettingsService<TestAppSettings>>();

        // Assert
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void RegisterSettings_WithCustomPath_ShouldUseCustomPath()
    {
        // Arrange
        var services = new ServiceCollection();
        var containerExtension = new MicrosoftDependencyInjectionExtension(services);
        _createdFiles.Add(_testFilePath);

        // Act
        containerExtension.RegisterSettings<TestAppSettings>(_testFilePath);
        var provider = services.BuildServiceProvider();
        var settingsService = provider.GetRequiredService<ITypedSettingsService<TestAppSettings>>();

        // 설정 업데이트하여 파일 생성
        settingsService.Update(s => s.Theme = "Dark");

        // 저장 대기 (debounce)
        Thread.Sleep(600);

        // Assert
        Assert.True(File.Exists(_testFilePath));
        var json = File.ReadAllText(_testFilePath);
        Assert.Contains("Dark", json);
    }

    [Fact]
    public void RegisterSettings_ShouldProvideDefaultValues()
    {
        // Arrange
        var services = new ServiceCollection();
        var containerExtension = new MicrosoftDependencyInjectionExtension(services);

        // Act
        containerExtension.RegisterSettings<TestAppSettings>();
        var provider = services.BuildServiceProvider();
        var settingsService = provider.GetRequiredService<ITypedSettingsService<TestAppSettings>>();

        // Assert
        Assert.Equal("Light", settingsService.Value.Theme);
        Assert.Equal(14, settingsService.Value.FontSize);
        Assert.Equal("localhost", settingsService.Value.Database.ConnectionString);
        Assert.Equal(30, settingsService.Value.Database.Timeout);
    }

    [Fact]
    public void RegisterSettings_ShouldAllowUpdates()
    {
        // Arrange
        var services = new ServiceCollection();
        var containerExtension = new MicrosoftDependencyInjectionExtension(services);
        containerExtension.RegisterSettings<TestAppSettings>();
        var provider = services.BuildServiceProvider();
        var settingsService = provider.GetRequiredService<ITypedSettingsService<TestAppSettings>>();

        // Act
        settingsService.Update(s =>
        {
            s.Theme = "Dark";
            s.FontSize = 20;
            s.Database.ConnectionString = "server=production";
        });

        // Assert
        Assert.Equal("Dark", settingsService.Value.Theme);
        Assert.Equal(20, settingsService.Value.FontSize);
        Assert.Equal("server=production", settingsService.Value.Database.ConnectionString);
    }

    [Fact]
    public void RegisterSettings_MultipleDifferentTypes_ShouldWorkIndependently()
    {
        // Arrange
        var services = new ServiceCollection();
        var containerExtension = new MicrosoftDependencyInjectionExtension(services);

        // Act - 두 가지 다른 설정 타입 등록
        containerExtension.RegisterSettings<TestAppSettings>();
        containerExtension.RegisterSettings<AnotherSettings>();
        var provider = services.BuildServiceProvider();

        var appSettings = provider.GetRequiredService<ITypedSettingsService<TestAppSettings>>();
        var anotherSettings = provider.GetRequiredService<ITypedSettingsService<AnotherSettings>>();

        // Assert
        Assert.NotNull(appSettings);
        Assert.NotNull(anotherSettings);
        Assert.Equal("Light", appSettings.Value.Theme);
        Assert.Equal("Default", anotherSettings.Value.Name);
    }

    [Fact]
    public void RegisterSettings_ShouldRaiseSettingsChangedEvent()
    {
        // Arrange
        var services = new ServiceCollection();
        var containerExtension = new MicrosoftDependencyInjectionExtension(services);
        containerExtension.RegisterSettings<TestAppSettings>();
        var provider = services.BuildServiceProvider();
        var settingsService = provider.GetRequiredService<ITypedSettingsService<TestAppSettings>>();

        TestAppSettings? changedSettings = null;
        settingsService.SettingsChanged += s => changedSettings = s;

        // Act
        settingsService.Update(s => s.Theme = "Dark");

        // Assert
        Assert.NotNull(changedSettings);
        Assert.Equal("Dark", changedSettings!.Theme);
    }
}

/// <summary>
///     다중 설정 타입 테스트용
/// </summary>
public class AnotherSettings
{
    public string Name { get; set; } = "Default";
    public bool Enabled { get; set; } = true;
}
