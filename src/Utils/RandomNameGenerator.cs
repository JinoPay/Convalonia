using System;
using System.Collections.Generic;

namespace Convalonia.Utils;

/// <summary>
/// Generates random workspace names using combinations of adjectives and nouns
/// </summary>
public static class RandomNameGenerator
{
    private static readonly Random _random = new();

    // City names for workspace naming
    private static readonly string[] _cities = new[]
    {
        "Montreal", "Tokyo", "Paris", "London", "Berlin",
        "Sydney", "Mumbai", "Seoul", "Toronto", "Barcelona",
        "Amsterdam", "Singapore", "Istanbul", "Dublin", "Prague",
        "Vienna", "Copenhagen", "Stockholm", "Oslo", "Helsinki",
        "Lisbon", "Athens", "Warsaw", "Budapest", "Brussels",
        "Zurich", "Geneva", "Milan", "Venice", "Florence"
    };

    // Animal names as alternative naming scheme
    private static readonly string[] _animals = new[]
    {
        "Bengal", "Falcon", "Lynx", "Phoenix", "Dragon",
        "Tiger", "Eagle", "Wolf", "Panda", "Leopard",
        "Cobra", "Hawk", "Raven", "Jaguar", "Orca",
        "Dolphin", "Cheetah", "Gorilla", "Penguin", "Koala"
    };

    // Adjectives for creative combinations
    private static readonly string[] _adjectives = new[]
    {
        "Swift", "Bright", "Bold", "Calm", "Epic",
        "Happy", "Quiet", "Noble", "Wild", "Wise",
        "Quick", "Brave", "Cool", "Smart", "Fast"
    };

    /// <summary>
    /// Generates a random workspace name using city names
    /// </summary>
    public static string GenerateCityName()
    {
        return _cities[_random.Next(_cities.Length)];
    }

    /// <summary>
    /// Generates a random workspace name using animal names
    /// </summary>
    public static string GenerateAnimalName()
    {
        return _animals[_random.Next(_animals.Length)];
    }

    /// <summary>
    /// Generates a random workspace name using adjective + animal combination
    /// Example: "SwiftFalcon", "BoldTiger"
    /// </summary>
    public static string GenerateAdjectiveAnimalName()
    {
        var adjective = _adjectives[_random.Next(_adjectives.Length)];
        var animal = _animals[_random.Next(_animals.Length)];
        return $"{adjective}{animal}";
    }

    /// <summary>
    /// Generates a random workspace name using the default strategy (city names)
    /// </summary>
    public static string Generate()
    {
        return GenerateCityName();
    }

    /// <summary>
    /// Generates a unique workspace name that doesn't exist in the provided list
    /// </summary>
    public static string GenerateUnique(IEnumerable<string> existingNames)
    {
        var existingSet = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);
        var allNames = new List<string>();

        // Add all possible names
        allNames.AddRange(_cities);
        allNames.AddRange(_animals);

        // Filter out existing names
        var availableNames = allNames.FindAll(name => !existingSet.Contains(name));

        if (availableNames.Count > 0)
        {
            return availableNames[_random.Next(availableNames.Count)];
        }

        // If all simple names are taken, use adjective + animal combinations
        for (int i = 0; i < 100; i++)
        {
            var name = GenerateAdjectiveAnimalName();
            if (!existingSet.Contains(name))
            {
                return name;
            }
        }

        // Last resort: append a number
        return $"{Generate()}{_random.Next(1000, 9999)}";
    }
}
