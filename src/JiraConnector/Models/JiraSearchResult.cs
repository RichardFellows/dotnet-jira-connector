using System.Text.Json.Serialization;

namespace JiraConnector.Models;

/// <summary>
/// Represents the result of a JIRA search API call
/// </summary>
public class JiraSearchResult
{
    [JsonPropertyName("expand")]
    public string Expand { get; set; } = string.Empty;

    [JsonPropertyName("startAt")]
    public int StartAt { get; set; }

    [JsonPropertyName("maxResults")]
    public int MaxResults { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("issues")]
    public List<JiraIssue> Issues { get; set; } = new();
}

/// <summary>
/// Search request parameters for JIRA API
/// </summary>
public class JiraSearchRequest
{
    /// <summary>
    /// JQL (JIRA Query Language) query string
    /// </summary>
    public string Jql { get; set; } = string.Empty;

    /// <summary>
    /// Starting position for results (0-based)
    /// </summary>
    public int StartAt { get; set; } = 0;

    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int MaxResults { get; set; } = 50;

    /// <summary>
    /// Fields to include in the response
    /// </summary>
    public List<string> Fields { get; set; } = new();

    /// <summary>
    /// Additional properties to expand
    /// </summary>
    public List<string> Expand { get; set; } = new();
}