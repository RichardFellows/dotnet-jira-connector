using JiraConnector.Models;

namespace JiraConnector.Services;

/// <summary>
/// Interface for JIRA API client operations
/// </summary>
public interface IJiraApiClient
{
    /// <summary>
    /// Searches for issues using JQL
    /// </summary>
    /// <param name="request">Search parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results containing matching issues</returns>
    Task<JiraSearchResult> SearchIssuesAsync(JiraSearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific issue by key
    /// </summary>
    /// <param name="issueKey">Issue key (e.g., PROJECT-123)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The requested issue or null if not found</returns>
    Task<JiraIssue?> GetIssueAsync(string issueKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets issues updated since a specific date
    /// </summary>
    /// <param name="since">Date to check for updates since</param>
    /// <param name="projectKeys">Optional project keys to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Issues updated since the specified date</returns>
    Task<List<JiraIssue>> GetIssuesUpdatedSinceAsync(DateTime since, IEnumerable<string>? projectKeys = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all issues for specified projects
    /// </summary>
    /// <param name="projectKeys">Project keys to retrieve issues for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All issues from the specified projects</returns>
    Task<List<JiraIssue>> GetAllIssuesAsync(IEnumerable<string> projectKeys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to JIRA server
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection is successful</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}