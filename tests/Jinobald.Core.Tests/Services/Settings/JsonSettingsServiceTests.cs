using Jinobald.Core.Services.Settings;

namespace Jinobald.Core.Tests.Services.Settings;

public class JsonSettingsServiceTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly JsonSettingsService _service;

    public JsonSettingsServiceTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"jinobald_test_{Guid.NewGuid()}.json");
        _service = new JsonSettingsService(_testFilePath);
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
            File.Delete(_testFilePath);
    }

    [Fact]
    public void Set_ShouldStoreValue()
    {
        // Arrange & Act
        _service.Set("key1", "value1");

        // Assert
        Assert.True(_service.Contains("key1"));
    }

    [Fact]
    public void Get_ShouldReturnStoredStringValue()
    {
        // Arrange
        _service.Set("message", "Hello World");

        // Act
        var result = _service.Get<string>("message");

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void Get_ShouldReturnStoredIntValue()
    {
        // Arrange
        _service.Set("count", 42);

        // Act
        var result = _service.Get<int>("count");

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void Get_ShouldReturnStoredBoolValue()
    {
        // Arrange
        _service.Set("enabled", true);

        // Act
        var result = _service.Get<bool>("enabled");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Get_ShouldReturnDefaultForMissingKey()
    {
        // Arrange & Act
        var result = _service.Get("missing", "default");

        // Assert
        Assert.Equal("default", result);
    }

    [Fact]
    public void Get_ShouldReturnDefaultValueWhenKeyNotFound()
    {
        // Arrange & Act
        var result = _service.Get("nonexistent", 100);

        // Assert
        Assert.Equal(100, result);
    }

    [Fact]
    public void Contains_ShouldReturnTrueForExistingKey()
    {
        // Arrange
        _service.Set("exists", "value");

        // Act & Assert
        Assert.True(_service.Contains("exists"));
    }

    [Fact]
    public void Contains_ShouldReturnFalseForMissingKey()
    {
        // Act & Assert
        Assert.False(_service.Contains("missing"));
    }

    [Fact]
    public void Remove_ShouldDeleteKey()
    {
        // Arrange
        _service.Set("toRemove", "value");

        // Act
        var result = _service.Remove("toRemove");

        // Assert
        Assert.True(result);
        Assert.False(_service.Contains("toRemove"));
    }

    [Fact]
    public void Remove_ShouldReturnFalseForMissingKey()
    {
        // Act
        var result = _service.Remove("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Clear_ShouldRemoveAllKeys()
    {
        // Arrange
        _service.Set("key1", "value1");
        _service.Set("key2", "value2");
        _service.Set("key3", "value3");

        // Act
        _service.Clear();

        // Assert
        Assert.Empty(_service.GetAllKeys());
    }

    [Fact]
    public void GetAllKeys_ShouldReturnAllStoredKeys()
    {
        // Arrange
        _service.Set("key1", "value1");
        _service.Set("key2", "value2");

        // Act
        var keys = _service.GetAllKeys().ToList();

        // Assert
        Assert.Contains("key1", keys);
        Assert.Contains("key2", keys);
    }

    [Fact]
    public void Set_ShouldOverwriteExistingKey()
    {
        // Arrange
        _service.Set("key", "original");

        // Act
        _service.Set("key", "updated");

        // Assert
        Assert.Equal("updated", _service.Get<string>("key"));
    }

    [Fact]
    public void SettingChanged_ShouldBeRaisedOnSet()
    {
        // Arrange
        string? changedKey = null;
        object? changedValue = null;
        _service.SettingChanged += (key, value) =>
        {
            changedKey = key;
            changedValue = value;
        };

        // Act
        _service.Set("eventKey", "eventValue");

        // Assert
        Assert.Equal("eventKey", changedKey);
        Assert.Equal("eventValue", changedValue);
    }

    [Fact]
    public void SettingChanged_ShouldBeRaisedOnRemove()
    {
        // Arrange
        _service.Set("toRemove", "value");
        string? changedKey = null;
        _service.SettingChanged += (key, _) => changedKey = key;

        // Act
        _service.Remove("toRemove");

        // Assert
        Assert.Equal("toRemove", changedKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Get_ShouldThrowForInvalidKey(string? key)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.Get<string>(key!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Set_ShouldThrowForInvalidKey(string? key)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.Set(key!, "value"));
    }
}
