using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Convalonia.Models;

namespace Convalonia.ViewModels;

/// <summary>
/// Converter for FileChangeType to icon
/// </summary>
public class FileChangeTypeIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is FileChangeType changeType)
        {
            return changeType switch
            {
                FileChangeType.Added => "✚",
                FileChangeType.Modified => "●",
                FileChangeType.Deleted => "✖",
                FileChangeType.Renamed => "➔",
                FileChangeType.Copied => "⎘",
                _ => "●"
            };
        }

        return "●";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for DiffLineType to background color
/// </summary>
public class DiffLineBackgroundConverter : IMultiValueConverter
{
    public static readonly DiffLineBackgroundConverter Instance = new();

    public object? Convert(System.Collections.Generic.IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count > 0 && values[0] is DiffLineType lineType)
        {
            return lineType switch
            {
                DiffLineType.Added => new SolidColorBrush(Color.Parse("#1a4d2e")),     // Dark green
                DiffLineType.Deleted => new SolidColorBrush(Color.Parse("#5c1a1a")),   // Dark red
                DiffLineType.Context => Brushes.Transparent,
                _ => Brushes.Transparent
            };
        }

        return Brushes.Transparent;
    }
}
