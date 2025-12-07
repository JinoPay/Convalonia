using Avalonia.Styling;
using Jinobald.Avalonia.Services.Theme;
using Jinobald.Core.Services.Settings;

namespace Jinobald.Avalonia.Tests.Services.Theme;

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
    public void Constructor_ShouldRegisterDefaultThemes()
    {
        // Arrange & Act
        var service = new ThemeService(_settingsService);

        // Assert
        Assert.Contains("Light", service.AvailableThemes);
        Assert.Contains("Dark", service.AvailableThemes);
        Assert.Contains("System", service.AvailableThemes);
    }

    [Fact]
    public void RegisterTheme_ShouldAddTheme()
    {
        // Arrange
        var service = new ThemeService(_settingsService);

        // Act
        service.RegisterTheme("Custom", ThemeVariant.Light);

        // Assert
        Assert.Contains("Custom", service.AvailableThemes);
    }

    [Fact]
    public void RegisterTheme_ShouldThrowForNullThemeName()
    {
        // Arrange
        var service = new ThemeService(_settingsService);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.RegisterTheme(null!, ThemeVariant.Light));
    }

    [Fact]
    public void RegisterTheme_ShouldThrowForEmptyThemeName()
    {
        // Arrange
        var service = new ThemeService(_settingsService);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.RegisterTheme("", ThemeVariant.Light));
    }

    [Fact]
    public void RegisterTheme_ShouldThrowForNullResourceDictionary()
    {
        // Arrange
        var service = new ThemeService(_settingsService);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.RegisterTheme("Custom", null!));
    }

    [Fact]
    public void RegisterTheme_ShouldThrowForInvalidType()
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
        var initialCount = service.AvailableThemes.Count();

        // Act
        service.RegisterTheme("Light", ThemeVariant.Dark);

        // Assert
        Assert.Equal(initialCount, service.AvailableThemes.Count());
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
