using System.ComponentModel.DataAnnotations;

namespace JiraConnector.Configuration;

/// <summary>
/// Configuration settings for JIRA Server connection
/// </summary>
public class JiraConfiguration
{
    public const string SectionName = "Jira";

    /// <summary>
    /// JIRA Server base URL (e.g., https://jira.company.com)
    /// </summary>
    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Personal Access Token for authentication
    /// </summary>
    [Required]
    [MinLength(1)]
    public string PersonalAccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Username for the API calls
    /// </summary>
    [Required]
    [MinLength(1)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Projects to synchronize (empty means all accessible projects)
    /// </summary>
    public List<string> ProjectKeys { get; set; } = new();

    /// <summary>
    /// Custom fields to retrieve and store
    /// </summary>
    public List<string> CustomFields { get; set; } = new();

    /// <summary>
    /// HTTP timeout in seconds
    /// </summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of issues to fetch per API call
    /// </summary>
    [Range(1, 1000)]
    public int MaxResultsPerRequest { get; set; } = 100;
}