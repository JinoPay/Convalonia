using Jinobald.Core.Services.Dialog;

namespace Jinobald.Core.Tests.Services.Dialog;

public class DialogParametersTests
{
    [Fact]
    public void Add_ShouldStoreValue()
    {
        // Arrange
        var parameters = new DialogParameters();

        // Act
        parameters.Add("key1", "value1");

        // Assert
        Assert.True(parameters.ContainsKey("key1"));
    }

    [Fact]
    public void GetValue_ShouldReturnStoredValue()
    {
        // Arrange
        var parameters = new DialogParameters();
        parameters.Add("message", "Hello World");

        // Act
        var result = parameters.GetValue<string>("message");

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void GetValue_ShouldReturnDefaultForMissingKey()
    {
        // Arrange
        var parameters = new DialogParameters();

        // Act
        var result = parameters.GetValue<string>("missing");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetValue_ShouldReturnDefaultForTypeMismatch()
    {
        // Arrange
        var parameters = new DialogParameters();
        parameters.Add("number", "not a number");

        // Act
        var result = parameters.GetValue<int>("number");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetValue_ShouldWorkWithComplexTypes()
    {
        // Arrange
        var testObject = new TestData { Id = 123, Name = "Test" };
        var parameters = new DialogParameters();
        parameters.Add("data", testObject);

        // Act
        var result = parameters.GetValue<TestData>("data");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(123, result.Id);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public void Constructor_WithTuples_ShouldInitializeParameters()
    {
        // Arrange & Act
        var parameters = new DialogParameters(
            ("key1", "value1"),
            ("key2", 42),
            ("key3", true)
        );

        // Assert
        Assert.Equal("value1", parameters.GetValue<string>("key1"));
        Assert.Equal(42, parameters.GetValue<int>("key2"));
        Assert.True(parameters.GetValue<bool>("key3"));
    }

    [Fact]
    public void ContainsKey_ShouldReturnFalseForMissingKey()
    {
        // Arrange
        var parameters = new DialogParameters();

        // Act & Assert
        Assert.False(parameters.ContainsKey("missing"));
    }

    [Fact]
    public void Add_ShouldOverwriteExistingKey()
    {
        // Arrange
        var parameters = new DialogParameters();
        parameters.Add("key", "original");

        // Act
        parameters.Add("key", "updated");

        // Assert
        Assert.Equal("updated", parameters.GetValue<string>("key"));
    }

    private class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
