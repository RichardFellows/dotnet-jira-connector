using JiraConnector.Models;

namespace JiraConnector.Data;

/// <summary>
/// Interface for database operations related to JIRA data
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// Initializes the database schema
    /// </summary>
    Task InitializeDatabaseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates a batch of issues
    /// </summary>
    Task UpsertIssuesAsync(IEnumerable<JiraIssue> issues, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets issues by their keys
    /// </summary>
    Task<List<JiraIssue>> GetIssuesByKeysAsync(IEnumerable<string> issueKeys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last sync timestamp for incremental updates
    /// </summary>
    Task<DateTime?> GetLastSyncTimestampAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last sync timestamp
    /// </summary>
    Task UpdateLastSyncTimestampAsync(DateTime timestamp, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets project statistics
    /// </summary>
    Task<List<ProjectSummary>> GetProjectSummariesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets issue metrics for analysis
    /// </summary>
    Task<List<IssueMetric>> GetIssueMetricsAsync(string? projectKey = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a custom analytical query
    /// </summary>
    Task<List<Dictionary<string, object>>> ExecuteAnalyticalQueryAsync(string sql, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets database health status
    /// </summary>
    Task<DatabaseHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Project summary for analytics
/// </summary>
public class ProjectSummary
{
    public string ProjectId { get; set; } = string.Empty;
    public string ProjectKey { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public int TotalIssues { get; set; }
    public int ResolvedIssues { get; set; }
    public int OpenIssues { get; set; }
    public int InProgressIssues { get; set; }
    public double? AverageResolutionDays { get; set; }
    public DateTime? FirstIssueDate { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
}

/// <summary>
/// Issue metrics for analytics
/// </summary>
public class IssueMetric
{
    public string IssueId { get; set; } = string.Empty;
    public string IssueKey { get; set; } = string.Empty;
    public string ProjectKey { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string IssueTypeName { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string StatusCategory { get; set; } = string.Empty;
    public string? PriorityName { get; set; }
    public string? AssigneeName { get; set; }
    public string? ReporterName { get; set; }
    public string Summary { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public int AgeDays { get; set; }
    public int? ResolutionDays { get; set; }
    public List<string> Labels { get; set; } = new();
    public double? HoursSpent { get; set; }
    public double? HoursEstimated { get; set; }
}

/// <summary>
/// Database health status
/// </summary>
public class DatabaseHealthStatus
{
    public bool IsHealthy { get; set; }
    public string DatabasePath { get; set; } = string.Empty;
    public long DatabaseSizeBytes { get; set; }
    public int TotalIssues { get; set; }
    public int TotalProjects { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public List<string> Issues { get; set; } = new();
}