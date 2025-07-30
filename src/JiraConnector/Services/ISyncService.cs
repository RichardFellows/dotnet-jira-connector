namespace JiraConnector.Services;

/// <summary>
/// Interface for data synchronization between JIRA and local database
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Performs a full synchronization of all accessible JIRA data
    /// </summary>
    Task<SyncResult> PerformFullSyncAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs an incremental sync of data changed since last sync
    /// </summary>
    Task<SyncResult> PerformIncrementalSyncAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to JIRA and database
    /// </summary>
    Task<bool> TestConnectionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of the last synchronization
    /// </summary>
    Task<SyncStatus> GetLastSyncStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a synchronization operation
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public int IssuesProcessed { get; set; }
    public int IssuesInserted { get; set; }
    public int IssuesUpdated { get; set; }
    public int IssuesSkipped { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = new();
    public SyncType SyncType { get; set; }
}

/// <summary>
/// Status of synchronization
/// </summary>
public class SyncStatus
{
    public DateTime? LastFullSync { get; set; }
    public DateTime? LastIncrementalSync { get; set; }
    public bool IsHealthy { get; set; }
    public int TotalIssues { get; set; }
    public int TotalProjects { get; set; }
    public string? LastError { get; set; }
}

/// <summary>
/// Type of synchronization
/// </summary>
public enum SyncType
{
    Full,
    Incremental
}