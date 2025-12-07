using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Convalonia.Models;

namespace Convalonia.Services;

/// <summary>
/// Interface for Claude API service
/// </summary>
public interface IClaudeApiService
{
    Task<ClaudeResponse> SendMessageAsync(
        List<ClaudeMessage> messages,
        string model = "claude-sonnet-4-5-20250929",
        int maxTokens = 4096,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ClaudeStreamEvent> StreamMessageAsync(
        List<ClaudeMessage> messages,
        string model = "claude-sonnet-4-5-20250929",
        int maxTokens = 4096);
}

/// <summary>
/// Handles communication with Claude API
/// </summary>
public class ClaudeApiService : IClaudeApiService
{
    private readonly HttpClient _httpClient;
    private const string ApiBaseUrl = "https://api.anthropic.com/v1";

    /// <summary>
    /// Creates a new ClaudeApiService with IHttpClientFactory (recommended)
    /// </summary>
    public ClaudeApiService(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.BaseAddress = new Uri(ApiBaseUrl);
        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    /// <summary>
    /// Creates a new ClaudeApiService (for backward compatibility)
    /// Note: Consider using IHttpClientFactory for proper connection pooling
    /// </summary>
    [Obsolete("Use the constructor with HttpClient parameter for proper connection pooling")]
    public ClaudeApiService(string apiKey) : this(new HttpClient(), apiKey)
    {
    }

    /// <summary>
    /// Sends a message to Claude and gets a response
    /// </summary>
    public async Task<ClaudeResponse> SendMessageAsync(
        List<ClaudeMessage> messages,
        string model = "claude-sonnet-4-5-20250929",
        int maxTokens = 4096,
        CancellationToken cancellationToken = default)
    {
        var request = new ClaudeRequest
        {
            Model = model,
            MaxTokens = maxTokens,
            Messages = messages
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/messages",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ClaudeResponse>(
            cancellationToken: cancellationToken);

        return result ?? throw new Exception("Failed to parse Claude API response");
    }

    /// <summary>
    /// Streams a message response from Claude
    /// </summary>
    public async IAsyncEnumerable<ClaudeStreamEvent> StreamMessageAsync(
        List<ClaudeMessage> messages,
        string model = "claude-sonnet-4-5-20250929",
        int maxTokens = 4096)
    {
        var request = new ClaudeRequest
        {
            Model = model,
            MaxTokens = maxTokens,
            Messages = messages,
            Stream = true
        };

        var response = await _httpClient.PostAsJsonAsync("/messages", request);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                continue;

            var data = line.Substring(6);
            if (data == "[DONE]")
                break;

            var streamEvent = JsonSerializer.Deserialize<ClaudeStreamEvent>(data);
            if (streamEvent != null)
                yield return streamEvent;
        }
    }
}

// API Request/Response models
public class ClaudeRequest
{
    public string Model { get; set; } = string.Empty;
    public int MaxTokens { get; set; }
    public List<ClaudeMessage> Messages { get; set; } = new();
    public bool Stream { get; set; }
}

public class ClaudeMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class ClaudeResponse
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<ContentBlock> Content { get; set; } = new();
    public string Model { get; set; } = string.Empty;
    public string StopReason { get; set; } = string.Empty;
    public Usage Usage { get; set; } = new();
}

public class ContentBlock
{
    public string Type { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class Usage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
}

public class ClaudeStreamEvent
{
    public string Type { get; set; } = string.Empty;
    public ContentBlock? Delta { get; set; }
}
