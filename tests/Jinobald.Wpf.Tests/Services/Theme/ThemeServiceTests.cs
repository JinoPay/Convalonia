using System.Windows;
using Jinobald.Core.Services.Settings;
using Jinobald.Wpf.Services.Theme;

namespace Jinobald.Wpf.Tests.Services.Theme;

public class ThemeServiceTests
{
    private readonly ISettingsService _settingsService;

    public ThemeServiceTests()
    {
        _settingsService = Substitute.For<ISettingsService>();
    }

    [Fact]
    public void Constructor_ShouldThrowForNullSettingsService()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ThemeService(null!));
    }

    [Fact]
    public void Constructor_ShouldInitializeWithEmptyThemes()
    {
        // Arrange & Act
        var service = new ThemeService(_settingsService);

        // Assert
        Assert.Empty(service.AvailableThemes);
    }

    [Fact]
    public void RegisterTheme_ShouldAddTheme()
    {
        // Arrange
        var service = new ThemeService(_settingsService);
        var theme = new ResourceDictionary();

        // Act
        service.RegisterTheme("Light", theme);

        // Assert
        Assert.Single(service.AvailableThemes);
        Assert.Contains("Light", service.AvailableThemes);
    }

    [Fact]
    public void RegisterTheme_ShouldThrowForNullThemeName()
    {
        // Arrange
        var service = new ThemeService(_settingsService);
        var theme = new ResourceDictionary();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.RegisterTheme(null!, theme));
    }

    [Fact]
    public void RegisterTheme_ShouldThrowForEmptyThemeName()
    {
        // Arrange
        var service = new ThemeService(_settingsService);
        var theme = new ResourceDictionary();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.RegisterTheme("", theme));
    }

    [Fact]
    public void RegisterTheme_ShouldThrowForNonResourceDictionary()
    {
        // Arrange
        var service = new ThemeService(_settingsService);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.RegisterTheme("Invalid", new object()));
    }

    [Fact]
    public void RegisterTheme_ShouldOverwriteExistingTheme()
    {
        // Arrange
        var service = new ThemeService(_settingsService);
        var theme1 = new ResourceDictionary();
        var theme2 = new ResourceDictionary();

        // Act
        service.RegisterTheme("Light", theme1);
        service.RegisterTheme("Light", theme2);

        // Assert
        Assert.Single(service.AvailableThemes);
    }

    [Fact]
    public void CurrentTheme_ShouldDefaultToLight()
    {
        // Arrange & Act
        var service = new ThemeService(_settingsService);

        // Assert
        Assert.Equal("Light", service.CurrentTheme);
    }

    [Fact]
    public void SetTheme_ShouldThrowForNullThemeName()
    {
        // Arrange
        var service = new ThemeService(_settingsService);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.SetTheme(null!));
    }

    [Fact]
    public void ApplySavedTheme_ShouldNotThrowWhenNoThemesRegistered()
    {
        // Arrange
        var service = new ThemeService(_settingsService);

        // Act & Assert (should not throw)
        var exception = Record.Exception(() => service.ApplySavedTheme());
        Assert.Null(exception);
    }

    [Fact]
    public void GetThemeColor_ShouldReturnNullForEmptyKey()
    {
        // Arrange
        var service = new ThemeService(_settingsService);

        // Act
        var result = service.GetThemeColor("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetThemeColor_ShouldReturnNullForNullKey()
    {
        // Arrange
        var service = new ThemeService(_settingsService);

        // Act
        var result = service.GetThemeColor(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetThemeResource_ShouldReturnNullForEmptyKey()
    {
        // Arrange
        var service = new ThemeService(_settingsService);

        // Act
        var result = service.GetThemeResource("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetThemeResource_ShouldReturnNullForNullKey()
    {
        // Arrange
        var service = new ThemeService(_settingsService);

        // Act
        var result = service.GetThemeResource(null!);

        // Assert
        Assert.Null(result);
    }
}
