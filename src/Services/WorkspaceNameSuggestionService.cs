using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Convalonia.Services;

/// <summary>
/// Suggests workspace names based on user's task description
/// </summary>
public static class WorkspaceNameSuggestionService
{
    private static readonly Dictionary<string, string[]> _keywordMappings = new()
    {
        // Authentication & Security
        { "auth", new[] { "Auth", "Authentication", "Login" } },
        { "login", new[] { "Login", "Auth" } },
        { "signup", new[] { "Signup", "Registration" } },
        { "user", new[] { "UserMgmt", "Users" } },

        // API & Backend
        { "api", new[] { "API", "Backend" } },
        { "backend", new[] { "Backend", "API" } },
        { "endpoint", new[] { "API", "Endpoints" } },
        { "rest", new[] { "RestAPI", "API" } },
        { "graphql", new[] { "GraphQL", "API" } },

        // Frontend & UI
        { "ui", new[] { "UI", "Interface" } },
        { "frontend", new[] { "Frontend", "UI" } },
        { "component", new[] { "Components", "UI" } },
        { "page", new[] { "Pages", "UI" } },
        { "view", new[] { "Views", "UI" } },
        { "theme", new[] { "Theme", "Styling" } },
        { "dark mode", new[] { "DarkMode", "Theme" } },

        // Database
        { "database", new[] { "Database", "DB" } },
        { "migration", new[] { "Migration", "DB" } },
        { "schema", new[] { "Schema", "DB" } },

        // Features
        { "search", new[] { "Search", "SearchFeature" } },
        { "filter", new[] { "Filter", "Filtering" } },
        { "export", new[] { "Export", "DataExport" } },
        { "import", new[] { "Import", "DataImport" } },
        { "chat", new[] { "Chat", "Messaging" } },
        { "notification", new[] { "Notifications", "Alerts" } },
        { "payment", new[] { "Payment", "Billing" } },

        // Operations
        { "fix", new[] { "Bugfix", "Fix" } },
        { "bug", new[] { "Bugfix", "Fix" } },
        { "refactor", new[] { "Refactor", "Cleanup" } },
        { "optimize", new[] { "Optimization", "Performance" } },
        { "performance", new[] { "Performance", "Optimization" } },
        { "test", new[] { "Testing", "Tests" } },

        // Infrastructure
        { "deploy", new[] { "Deployment", "Deploy" } },
        { "docker", new[] { "Docker", "Containerization" } },
        { "ci/cd", new[] { "CICD", "Pipeline" } },
        { "monitoring", new[] { "Monitoring", "Observability" } }
    };

    /// <summary>
    /// Suggests a workspace name based on the user's first message
    /// Returns null if no good suggestion can be made
    /// </summary>
    public static string? SuggestName(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            return null;

        var message = userMessage.ToLowerInvariant();

        // Try to find matching keywords
        foreach (var (keyword, suggestions) in _keywordMappings)
        {
            if (message.Contains(keyword))
            {
                return suggestions.First();
            }
        }

        // Try to extract action + noun pattern (e.g., "add user authentication")
        var actionMatch = ExtractActionNounPattern(message);
        if (actionMatch != null)
            return actionMatch;

        return null;
    }

    /// <summary>
    /// Extracts action + noun pattern and creates a camel case name
    /// Example: "add user authentication" -> "AddUserAuth"
    /// </summary>
    private static string? ExtractActionNounPattern(string message)
    {
        // Common actions in software development
        var actions = new[] { "add", "create", "implement", "build", "fix", "update", "remove", "delete", "refactor" };

        foreach (var action in actions)
        {
            // Look for pattern: [action] [words]
            var pattern = $@"\b{action}\s+([a-z\s]+)";
            var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var phrase = match.Groups[1].Value.Trim();

                // Limit to first 3 words
                var words = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(3);

                // Convert to PascalCase
                var name = string.Join("", words.Select(w =>
                    char.ToUpper(w[0]) + w.Substring(1).ToLower()
                ));

                // Add action prefix
                var actionPrefix = char.ToUpper(action[0]) + action.Substring(1).ToLower();

                return $"{actionPrefix}{name}";
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if the suggested name is significantly different from the current name
    /// to avoid unnecessary renames
    /// </summary>
    public static bool ShouldRename(string currentName, string suggestedName)
    {
        if (string.IsNullOrWhiteSpace(suggestedName))
            return false;

        // Don't rename if current name is already meaningful (not a random city/animal name)
        var randomNames = new[]
        {
            "Montreal", "Tokyo", "Paris", "London", "Berlin", "Sydney", "Mumbai",
            "Bengal", "Falcon", "Lynx", "Phoenix", "Dragon", "Tiger", "Eagle",
            "Swift", "Bright", "Bold", "Calm", "Epic", "Happy", "Quiet"
        };

        var isRandomName = randomNames.Any(rn =>
            currentName.Contains(rn, StringComparison.OrdinalIgnoreCase));

        return isRandomName;
    }
}
