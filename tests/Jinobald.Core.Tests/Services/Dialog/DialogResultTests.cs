using Jinobald.Core.Services.Dialog;
using Xunit;

namespace Jinobald.Core.Tests.Services.Dialog;

public class DialogResultTests
{
    #region DialogResult Tests

    [Fact]
    public void DialogResult_DefaultConstructor_ShouldHaveNoneResult()
    {
        // Arrange & Act
        var result = new DialogResult();

        // Assert
        Assert.Equal(ButtonResult.None, result.Result);
        Assert.NotNull(result.Parameters);
    }

    [Fact]
    public void DialogResult_WithResult_ShouldSetResult()
    {
        // Arrange & Act
        var result = new DialogResult(ButtonResult.OK);

        // Assert
        Assert.Equal(ButtonResult.OK, result.Result);
    }

    [Fact]
    public void DialogResult_WithResultAndParameters_ShouldSetBoth()
    {
        // Arrange
        var parameters = new DialogParameters();
        parameters.Add("key", "value");

        // Act
        var result = new DialogResult(ButtonResult.Yes, parameters);

        // Assert
        Assert.Equal(ButtonResult.Yes, result.Result);
        Assert.Same(parameters, result.Parameters);
    }

    [Fact]
    public void DialogResult_Ok_ShouldReturnOKResult()
    {
        // Act
        var result = DialogResult.Ok();

        // Assert
        Assert.Equal(ButtonResult.OK, result.Result);
    }

    [Fact]
    public void DialogResult_Cancel_ShouldReturnCancelResult()
    {
        // Act
        var result = DialogResult.Cancel();

        // Assert
        Assert.Equal(ButtonResult.Cancel, result.Result);
    }

    [Fact]
    public void DialogResult_Yes_ShouldReturnYesResult()
    {
        // Act
        var result = DialogResult.Yes();

        // Assert
        Assert.Equal(ButtonResult.Yes, result.Result);
    }

    [Fact]
    public void DialogResult_No_ShouldReturnNoResult()
    {
        // Act
        var result = DialogResult.No();

        // Assert
        Assert.Equal(ButtonResult.No, result.Result);
    }

    #endregion

    #region DialogResult<T> Tests

    [Fact]
    public void GenericDialogResult_DefaultConstructor_ShouldHaveNoneResultAndNullData()
    {
        // Arrange & Act
        var result = new DialogResult<string>();

        // Assert
        Assert.Equal(ButtonResult.None, result.Result);
        Assert.Null(result.Data);
    }

    [Fact]
    public void GenericDialogResult_WithResultAndData_ShouldSetBoth()
    {
        // Arrange & Act
        var result = new DialogResult<string>(ButtonResult.OK, "test data");

        // Assert
        Assert.Equal(ButtonResult.OK, result.Result);
        Assert.Equal("test data", result.Data);
    }

    [Fact]
    public void GenericDialogResult_Ok_ShouldReturnOKResultWithData()
    {
        // Act
        var result = DialogResult<int>.Ok(42);

        // Assert
        Assert.Equal(ButtonResult.OK, result.Result);
        Assert.Equal(42, result.Data);
    }

    [Fact]
    public void GenericDialogResult_Cancel_ShouldReturnCancelResultWithDefaultData()
    {
        // Act
        var result = DialogResult<int>.Cancel();

        // Assert
        Assert.Equal(ButtonResult.Cancel, result.Result);
        Assert.Equal(0, result.Data); // default(int)
    }

    [Fact]
    public void GenericDialogResult_Yes_ShouldReturnYesResultWithData()
    {
        // Act
        var result = DialogResult<bool>.Yes(true);

        // Assert
        Assert.Equal(ButtonResult.Yes, result.Result);
        Assert.True(result.Data);
    }

    [Fact]
    public void GenericDialogResult_No_ShouldReturnNoResultWithDefaultData()
    {
        // Act
        var result = DialogResult<bool>.No();

        // Assert
        Assert.Equal(ButtonResult.No, result.Result);
        Assert.False(result.Data); // default(bool)
    }

    [Fact]
    public void GenericDialogResult_WithComplexType_ShouldWork()
    {
        // Arrange
        var data = new TestData { Id = 1, Name = "Test" };

        // Act
        var result = DialogResult<TestData>.Ok(data);

        // Assert
        Assert.Equal(ButtonResult.OK, result.Result);
        Assert.Same(data, result.Data);
    }

    private class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #endregion

    #region Extension Methods Tests

    [Theory]
    [InlineData(ButtonResult.OK, true)]
    [InlineData(ButtonResult.Yes, true)]
    [InlineData(ButtonResult.Cancel, false)]
    [InlineData(ButtonResult.No, false)]
    [InlineData(ButtonResult.None, false)]
    public void IsSuccess_ShouldReturnCorrectValue(ButtonResult buttonResult, bool expected)
    {
        // Arrange
        var result = new DialogResult(buttonResult);

        // Act & Assert
        Assert.Equal(expected, result.IsSuccess());
    }

    [Theory]
    [InlineData(ButtonResult.Cancel, true)]
    [InlineData(ButtonResult.No, true)]
    [InlineData(ButtonResult.OK, false)]
    [InlineData(ButtonResult.Yes, false)]
    [InlineData(ButtonResult.None, false)]
    public void IsCancelled_ShouldReturnCorrectValue(ButtonResult buttonResult, bool expected)
    {
        // Arrange
        var result = new DialogResult(buttonResult);

        // Act & Assert
        Assert.Equal(expected, result.IsCancelled());
    }

    [Fact]
    public void AsTyped_WithTypedResult_ShouldReturnTypedResult()
    {
        // Arrange
        IDialogResult result = DialogResult<string>.Ok("test");

        // Act
        var typed = result.AsTyped<string>();

        // Assert
        Assert.NotNull(typed);
        Assert.Equal("test", typed.Data);
    }

    [Fact]
    public void AsTyped_WithNonTypedResult_ShouldReturnNull()
    {
        // Arrange
        IDialogResult result = DialogResult.Ok();

        // Act
        var typed = result.AsTyped<string>();

        // Assert
        Assert.Null(typed);
    }

    [Fact]
    public void GetData_WithTypedResult_ShouldReturnData()
    {
        // Arrange
        IDialogResult result = DialogResult<int>.Ok(42);

        // Act
        var data = result.GetData<int>();

        // Assert
        Assert.Equal(42, data);
    }

    [Fact]
    public void GetData_WithNonTypedResult_ShouldReturnDefault()
    {
        // Arrange
        IDialogResult result = DialogResult.Ok();

        // Act
        var data = result.GetData<int>();

        // Assert
        Assert.Equal(0, data);
    }

    [Fact]
    public void GetDataOrDefault_WithSuccessAndData_ShouldReturnData()
    {
        // Arrange
        IDialogResult result = DialogResult<string>.Ok("success");

        // Act
        var data = result.GetDataOrDefault<string>("default");

        // Assert
        Assert.Equal("success", data);
    }

    [Fact]
    public void GetDataOrDefault_WithCancelledResult_ShouldReturnDefault()
    {
        // Arrange
        IDialogResult result = DialogResult<string>.Cancel();

        // Act
        var data = result.GetDataOrDefault<string>("default");

        // Assert
        Assert.Equal("default", data);
    }

    [Fact]
    public void GetDataOrDefault_WithNonTypedResult_ShouldReturnDefault()
    {
        // Arrange
        IDialogResult result = DialogResult.Ok();

        // Act
        var data = result.GetDataOrDefault<string>("default");

        // Assert
        Assert.Equal("default", data);
    }

    #endregion

    #region Implicit Conversion Tests

    [Fact]
    public void ImplicitConversion_FromDialogResult_ToGenericDialogResult()
    {
        // Arrange
        var dialogResult = DialogResult.Ok();

        // Act
        DialogResult<string> genericResult = dialogResult;

        // Assert
        Assert.Equal(ButtonResult.OK, genericResult.Result);
        Assert.Null(genericResult.Data);
    }

    #endregion
}
