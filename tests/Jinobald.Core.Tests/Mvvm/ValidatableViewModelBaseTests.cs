using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using Jinobald.Core.Mvvm;

namespace Jinobald.Core.Tests.Mvvm;

public partial class ValidatableViewModelBaseTests
{
    #region Test ViewModels

    public partial class TestValidatableViewModel : ValidatableViewModelBase
    {
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Name is required")]
        [MinLength(2, ErrorMessage = "Name must be at least 2 characters")]
        private string _name = string.Empty;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        private string _email = string.Empty;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(0, 150, ErrorMessage = "Age must be between 0 and 150")]
        private int _age;

        public void SetCustomError(string propertyName, string message)
        {
            AddError(propertyName, message);
        }

        public void RemoveCustomError(string propertyName)
        {
            ClearCustomErrors(propertyName);
        }

        public void RemoveAllCustomErrors()
        {
            ClearAllCustomErrors();
        }
    }

    #endregion

    [Fact]
    public void HasErrors_ShouldBeFalseInitially()
    {
        // Arrange & Act
        var viewModel = new TestValidatableViewModel();

        // Assert
        Assert.False(viewModel.HasErrors);
    }

    [Fact]
    public void ValidateAllProperties_ShouldDetectErrors()
    {
        // Arrange
        var viewModel = new TestValidatableViewModel
        {
            Name = "", // Required violation
            Email = "invalid-email" // Email format violation
        };

        // Act
        viewModel.ValidateAllProperties();

        // Assert
        Assert.True(viewModel.HasErrors);
    }

    [Fact]
    public void SetProperty_WithInvalidValue_ShouldSetError()
    {
        // Arrange
        var viewModel = new TestValidatableViewModel();

        // Act - Set empty name (required violation)
        viewModel.Name = "";
        viewModel.ValidateAllProperties(); // Trigger validation

        // Assert
        Assert.True(viewModel.HasErrors);
        var errors = viewModel.GetErrorMessages(nameof(TestValidatableViewModel.Name));
        Assert.Contains("Name is required", errors);
    }

    [Fact]
    public void SetProperty_WithValidValue_ShouldClearError()
    {
        // Arrange
        var viewModel = new TestValidatableViewModel { Name = "" };
        viewModel.ValidateAllProperties(); // Trigger validation for error state

        // Act
        viewModel.Name = "John";
        viewModel.ValidateAllProperties(); // Trigger validation again

        // Assert
        var errors = viewModel.GetErrorMessages(nameof(TestValidatableViewModel.Name));
        Assert.Empty(errors);
    }

    [Fact]
    public void AddError_ShouldAddCustomError()
    {
        // Arrange
        var viewModel = new TestValidatableViewModel();

        // Act
        viewModel.SetCustomError("Name", "Custom error message");

        // Assert
        Assert.True(viewModel.HasErrors);
        var errors = viewModel.GetErrorMessages("Name");
        Assert.Contains("Custom error message", errors);
    }

    [Fact]
    public void ClearCustomErrors_ShouldRemoveCustomError()
    {
        // Arrange
        var viewModel = new TestValidatableViewModel();
        viewModel.SetCustomError("Name", "Custom error message");

        // Act
        viewModel.RemoveCustomError("Name");

        // Assert
        var errors = viewModel.GetErrorMessages("Name");
        Assert.DoesNotContain("Custom error message", errors);
    }

    [Fact]
    public void ClearAllCustomErrors_ShouldRemoveAllCustomErrors()
    {
        // Arrange
        var viewModel = new TestValidatableViewModel();
        viewModel.SetCustomError("Name", "Error 1");
        viewModel.SetCustomError("Email", "Error 2");

        // Act
        viewModel.RemoveAllCustomErrors();

        // Assert
        Assert.Empty(viewModel.GetErrorMessages("Name").Where(e => e == "Error 1"));
        Assert.Empty(viewModel.GetErrorMessages("Email").Where(e => e == "Error 2"));
    }

    [Fact]
    public void GetAllErrorMessages_ShouldReturnAllErrors()
    {
        // Arrange
        var viewModel = new TestValidatableViewModel
        {
            Name = "", // Required error
            Email = "invalid" // Email format error
        };
        viewModel.ValidateAllProperties();
        viewModel.SetCustomError("Age", "Custom age error");

        // Act
        var allErrors = viewModel.GetAllErrorMessages();

        // Assert
        Assert.True(allErrors.ContainsKey("Name"));
        Assert.True(allErrors.ContainsKey("Email"));
        Assert.True(allErrors.ContainsKey("Age"));
    }

    [Fact]
    public void ErrorsChanged_ShouldFireWhenErrorAdded()
    {
        // Arrange
        var viewModel = new TestValidatableViewModel();
        var errorsChangedFired = false;
        string? changedProperty = null;
        viewModel.ErrorsChanged += (_, e) =>
        {
            errorsChangedFired = true;
            changedProperty = e.PropertyName;
        };

        // Act
        viewModel.SetCustomError("Name", "Test error");

        // Assert
        Assert.True(errorsChangedFired);
        Assert.Equal("Name", changedProperty);
    }

    [Fact]
    public void MultipleErrors_ShouldAllBeReturned()
    {
        // Arrange
        var viewModel = new TestValidatableViewModel
        {
            Name = "A" // MinLength violation (2 chars required) but Required is satisfied
        };

        // Act
        viewModel.ValidateAllProperties();
        viewModel.SetCustomError("Name", "Custom error");

        // Assert
        var errors = viewModel.GetErrorMessages("Name");
        Assert.Contains("Name must be at least 2 characters", errors);
        Assert.Contains("Custom error", errors);
    }

    [Fact]
    public void Range_ShouldValidateAge()
    {
        // Arrange
        var viewModel = new TestValidatableViewModel { Age = 200 }; // Out of range

        // Act
        viewModel.ValidateAllProperties();

        // Assert
        var errors = viewModel.GetErrorMessages(nameof(TestValidatableViewModel.Age));
        Assert.Contains("Age must be between 0 and 150", errors);
    }

    [Fact]
    public void Dispose_ShouldClearCustomErrors()
    {
        // Arrange
        var viewModel = new TestValidatableViewModel();
        viewModel.SetCustomError("Name", "Error");

        // Act
        viewModel.Dispose();

        // Assert - custom errors should be cleared
        Assert.True(viewModel.IsDisposed);
    }

    [Fact]
    public void DuplicateError_ShouldNotBeAdded()
    {
        // Arrange
        var viewModel = new TestValidatableViewModel();

        // Act
        viewModel.SetCustomError("Name", "Same error");
        viewModel.SetCustomError("Name", "Same error"); // Duplicate

        // Assert
        var errors = viewModel.GetErrorMessages("Name");
        Assert.Single(errors.Where(e => e == "Same error"));
    }
}
