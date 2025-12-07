using Jinobald.Core.Services.Settings;

namespace Jinobald.Core.Tests.Services.Settings;

/// <summary>
///     테스트용 설정 클래스
/// </summary>
public class TestSettings
{
    public string Theme { get; set; } = "Light";
    public int FontSize { get; set; } = 14;
    public bool AutoSave { get; set; } = true;
    public NestedSettings Nested { get; set; } = new();
}

public class NestedSettings
{
    public string Name { get; set; } = "Default";
    public double Value { get; set; } = 1.5;
}

public class JsonTypedSettingsServiceTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly JsonTypedSettingsService<TestSettings> _service;

    public JsonTypedSettingsServiceTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"jinobald_typed_test_{Guid.NewGuid()}.json");
        _service = new JsonTypedSettingsService<TestSettings>(_testFilePath);
    }

    public void Dispose()
    {
        _service.Dispose();
        if (File.Exists(_testFilePath))
            File.Delete(_testFilePath);
    }

    [Fact]
    public void Value_ShouldReturnDefaultSettings()
    {
        // Act
        var settings = _service.Value;

        // Assert
        Assert.Equal("Light", settings.Theme);
        Assert.Equal(14, settings.FontSize);
        Assert.True(settings.AutoSave);
    }

    [Fact]
    public void Value_ShouldReturnNestedDefaultSettings()
    {
        // Act
        var settings = _service.Value;

        // Assert
        Assert.Equal("Default", settings.Nested.Name);
        Assert.Equal(1.5, settings.Nested.Value);
    }

    [Fact]
    public void Update_ShouldModifySettings()
    {
        // Act
        _service.Update(s => s.Theme = "Dark");

        // Assert
        Assert.Equal("Dark", _service.Value.Theme);
    }

    [Fact]
    public void Update_ShouldModifyNestedSettings()
    {
        // Act
        _service.Update(s =>
        {
            s.Nested.Name = "Modified";
            s.Nested.Value = 3.14;
        });

        // Assert
        Assert.Equal("Modified", _service.Value.Nested.Name);
        Assert.Equal(3.14, _service.Value.Nested.Value);
    }

    [Fact]
    public void Update_ShouldRaiseSettingsChangedEvent()
    {
        // Arrange
        TestSettings? changedSettings = null;
        _service.SettingsChanged += s => changedSettings = s;

        // Act
        _service.Update(s => s.FontSize = 20);

        // Assert
        Assert.NotNull(changedSettings);
        Assert.Equal(20, changedSettings!.FontSize);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifySettingsAsync()
    {
        // Act
        await _service.UpdateAsync(async s =>
        {
            await Task.Delay(10); // 시뮬레이션
            s.Theme = "Dark";
        });

        // Assert
        Assert.Equal("Dark", _service.Value.Theme);
    }

    [Fact]
    public void Reset_ShouldRestoreDefaultSettings()
    {
        // Arrange
        _service.Update(s =>
        {
            s.Theme = "Dark";
            s.FontSize = 20;
            s.AutoSave = false;
        });

        // Act
        _service.Reset();

        // Assert
        Assert.Equal("Light", _service.Value.Theme);
        Assert.Equal(14, _service.Value.FontSize);
        Assert.True(_service.Value.AutoSave);
    }

    [Fact]
    public void Reset_ShouldRaiseSettingsChangedEvent()
    {
        // Arrange
        _service.Update(s => s.Theme = "Dark");
        TestSettings? changedSettings = null;
        _service.SettingsChanged += s => changedSettings = s;

        // Act
        _service.Reset();

        // Assert
        Assert.NotNull(changedSettings);
        Assert.Equal("Light", changedSettings!.Theme);
    }

    [Fact]
    public async Task SaveAndReload_ShouldPersistSettings()
    {
        // Arrange
        _service.Update(s =>
        {
            s.Theme = "Dark";
            s.FontSize = 24;
            s.Nested.Name = "Persisted";
        });

        // Act - 명시적 저장 후 새 인스턴스로 로드
        await _service.SaveAsync();

        using var newService = new JsonTypedSettingsService<TestSettings>(_testFilePath);

        // Assert
        Assert.Equal("Dark", newService.Value.Theme);
        Assert.Equal(24, newService.Value.FontSize);
        Assert.Equal("Persisted", newService.Value.Nested.Name);
    }

    [Fact]
    public async Task ReloadAsync_ShouldLoadSettingsFromDisk()
    {
        // Arrange - 파일에 직접 설정 저장
        var json = """
        {
            "theme": "Dark",
            "fontSize": 32,
            "autoSave": false,
            "nested": {
                "name": "FromFile",
                "value": 9.99
            }
        }
        """;
        await File.WriteAllTextAsync(_testFilePath, json);

        // Act
        await _service.ReloadAsync();

        // Assert
        Assert.Equal("Dark", _service.Value.Theme);
        Assert.Equal(32, _service.Value.FontSize);
        Assert.False(_service.Value.AutoSave);
        Assert.Equal("FromFile", _service.Value.Nested.Name);
        Assert.Equal(9.99, _service.Value.Nested.Value);
    }

    [Fact]
    public async Task ReloadAsync_ShouldRaiseSettingsChangedEvent()
    {
        // Arrange
        var json = """{ "theme": "Dark" }""";
        await File.WriteAllTextAsync(_testFilePath, json);

        TestSettings? changedSettings = null;
        _service.SettingsChanged += s => changedSettings = s;

        // Act
        await _service.ReloadAsync();

        // Assert
        Assert.NotNull(changedSettings);
        Assert.Equal("Dark", changedSettings!.Theme);
    }

    [Fact]
    public void Update_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.Update(null!));
    }

    [Fact]
    public async Task UpdateAsync_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateAsync(null!));
    }

    [Fact]
    public void MultipleUpdates_ShouldWorkCorrectly()
    {
        // Act
        _service.Update(s => s.Theme = "Dark");
        _service.Update(s => s.FontSize = 18);
        _service.Update(s => s.AutoSave = false);

        // Assert
        Assert.Equal("Dark", _service.Value.Theme);
        Assert.Equal(18, _service.Value.FontSize);
        Assert.False(_service.Value.AutoSave);
    }
}
