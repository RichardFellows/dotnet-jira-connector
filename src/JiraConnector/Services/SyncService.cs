using JiraConnector.Configuration;
using JiraConnector.Data;
using Microsoft.Extensions.Logging;

namespace JiraConnector.Services;

/// <summary>
/// Service for synchronizing JIRA data with local database
/// </summary>
public class SyncService : ISyncService
{
    private readonly IJiraApiClient _jiraClient;
    private readonly IDatabaseService _databaseService;
    private readonly SyncConfiguration _syncConfig;
    private readonly JiraConfiguration _jiraConfig;
    private readonly ILogger<SyncService> _logger;

    public SyncService(
        IJiraApiClient jiraClient,
        IDatabaseService databaseService,
        SyncConfiguration syncConfig,
        JiraConfiguration jiraConfig,
        ILogger<SyncService> logger)
    {
        _jiraClient = jiraClient;
        _databaseService = databaseService;
        _syncConfig = syncConfig;
        _jiraConfig = jiraConfig;
        _logger = logger;
    }

    public async Task<SyncResult> PerformFullSyncAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting full synchronization");
        
        var result = new SyncResult
        {
            SyncType = SyncType.Full,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Test connections first
            if (!await TestConnectionsAsync(cancellationToken))
            {
                throw new InvalidOperationException("Connection test failed");
            }

            // Initialize database if needed
            await _databaseService.InitializeDatabaseAsync(cancellationToken);

            // Determine which projects to sync
            var projectKeys = _jiraConfig.ProjectKeys.Any() 
                ? _jiraConfig.ProjectKeys 
                : new List<string>(); // Would need to fetch all accessible projects

            if (!projectKeys.Any())
            {
                _logger.LogWarning("No projects specified for synchronization");
                result.Warnings.Add("No projects specified for synchronization");
            }
            else
            {
                // Sync all issues for specified projects
                var allIssues = await _jiraClient.GetAllIssuesAsync(projectKeys, cancellationToken);
                _logger.LogInformation("Retrieved {Count} issues from JIRA", allIssues.Count);

                // Process in batches
                var batches = allIssues.Chunk(_syncConfig.BatchSize);
                foreach (var batch in batches)
                {
                    await _databaseService.UpsertIssuesAsync(batch, cancellationToken);
                    result.IssuesProcessed += batch.Length;
                    result.IssuesInserted += batch.Length; // Simplified - would need to track actual inserts vs updates
                    
                    _logger.LogDebug("Processed batch of {Count} issues", batch.Length);
                }
            }

            // Update sync timestamp
            await _databaseService.UpdateLastSyncTimestampAsync(DateTime.UtcNow, cancellationToken);

            result.Success = true;
            result.EndTime = DateTime.UtcNow;

            _logger.LogInformation("Full synchronization completed successfully. Processed {Count} issues in {Duration}",
                result.IssuesProcessed, result.Duration);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.UtcNow;

            _logger.LogError(ex, "Full synchronization failed after {Duration}", result.Duration);
        }

        return result;
    }

    public async Task<SyncResult> PerformIncrementalSyncAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting incremental synchronization");

        var result = new SyncResult
        {
            SyncType = SyncType.Incremental,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Test connections first
            if (!await TestConnectionsAsync(cancellationToken))
            {
                throw new InvalidOperationException("Connection test failed");
            }

            // Get last sync timestamp
            var lastSync = await _databaseService.GetLastSyncTimestampAsync(cancellationToken);
            if (!lastSync.HasValue)
            {
                _logger.LogWarning("No previous sync found, performing full sync instead");
                return await PerformFullSyncAsync(cancellationToken);
            }

            // Calculate lookback time (with some buffer for clock differences)
            var lookbackTime = lastSync.Value.AddDays(-_syncConfig.LookbackDays);
            _logger.LogInformation("Syncing changes since {LookbackTime}", lookbackTime);

            // Determine which projects to sync
            var projectKeys = _jiraConfig.ProjectKeys.Any() 
                ? _jiraConfig.ProjectKeys 
                : new List<string>();

            if (!projectKeys.Any())
            {
                _logger.LogWarning("No projects specified for synchronization");
                result.Warnings.Add("No projects specified for synchronization");
            }
            else
            {
                // Get issues updated since last sync
                var updatedIssues = await _jiraClient.GetIssuesUpdatedSinceAsync(
                    lookbackTime, projectKeys, cancellationToken);
                
                _logger.LogInformation("Found {Count} issues updated since {LastSync}", 
                    updatedIssues.Count, lookbackTime);

                if (updatedIssues.Any())
                {
                    // Process in batches
                    var batches = updatedIssues.Chunk(_syncConfig.BatchSize);
                    foreach (var batch in batches)
                    {
                        await _databaseService.UpsertIssuesAsync(batch, cancellationToken);
                        result.IssuesProcessed += batch.Length;
                        result.IssuesUpdated += batch.Length; // Simplified
                        
                        _logger.LogDebug("Processed incremental batch of {Count} issues", batch.Length);
                    }
                }
                else
                {
                    _logger.LogInformation("No issues found that need updating");
                }
            }

            // Update sync timestamp
            await _databaseService.UpdateLastSyncTimestampAsync(DateTime.UtcNow, cancellationToken);

            result.Success = true;
            result.EndTime = DateTime.UtcNow;

            _logger.LogInformation("Incremental synchronization completed successfully. Processed {Count} issues in {Duration}",
                result.IssuesProcessed, result.Duration);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.UtcNow;

            _logger.LogError(ex, "Incremental synchronization failed after {Duration}", result.Duration);
        }

        return result;
    }

    public async Task<bool> TestConnectionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Testing connections to JIRA and database");

        try
        {
            // Test JIRA connection
            var jiraConnectionOk = await _jiraClient.TestConnectionAsync(cancellationToken);
            if (!jiraConnectionOk)
            {
                _logger.LogError("JIRA connection test failed");
                return false;
            }

            // Test database connection
            var dbHealth = await _databaseService.GetHealthStatusAsync(cancellationToken);
            if (!dbHealth.IsHealthy)
            {
                _logger.LogError("Database health check failed: {Issues}", 
                    string.Join(", ", dbHealth.Issues));
                return false;
            }

            _logger.LogInformation("All connection tests passed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed");
            return false;
        }
    }

    public async Task<SyncStatus> GetLastSyncStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var lastSync = await _databaseService.GetLastSyncTimestampAsync(cancellationToken);
            var dbHealth = await _databaseService.GetHealthStatusAsync(cancellationToken);

            return new SyncStatus
            {
                LastIncrementalSync = lastSync,
                IsHealthy = dbHealth.IsHealthy,
                TotalIssues = dbHealth.TotalIssues,
                TotalProjects = dbHealth.TotalProjects,
                LastError = dbHealth.Issues.FirstOrDefault()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sync status");
            return new SyncStatus
            {
                IsHealthy = false,
                LastError = ex.Message
            };
        }
    }
}