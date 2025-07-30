using DuckDB.NET.Data;
using JiraConnector.Configuration;
using JiraConnector.Models;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;
using System.Text.Json;

namespace JiraConnector.Data;

/// <summary>
/// DuckDB implementation of the database service
/// </summary>
public class DuckDbService : IDatabaseService, IDisposable
{
    private readonly DatabaseConfiguration _config;
    private readonly ILogger<DuckDbService> _logger;
    private DuckDBConnection? _connection;
    private bool _disposed;

    public DuckDbService(DatabaseConfiguration config, ILogger<DuckDbService> logger)
    {
        _config = config;
        _logger = logger;
    }

    private async Task<DuckDBConnection> GetConnectionAsync()
    {
        if (_connection == null)
        {
            var connectionString = $"Data Source={_config.FilePath}";
            _connection = new DuckDBConnection(connectionString);
            await _connection.OpenAsync();

            // Configure DuckDB settings
            if (_config.EnableWalMode)
            {
                await ExecuteNonQueryAsync("PRAGMA journal_mode=WAL");
            }

            await ExecuteNonQueryAsync($"SET memory_limit='{_config.MemoryLimitMB}MB'");
        }

        return _connection;
    }

    public async Task InitializeDatabaseAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing database schema");

        var connection = await GetConnectionAsync();
        var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "DatabaseSchema.sql");

        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException($"Database schema file not found: {schemaPath}");
        }

        var schemaSql = await File.ReadAllTextAsync(schemaPath, cancellationToken);
        var statements = schemaSql.Split(new[] { ";\r\n", ";\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var statement in statements)
        {
            if (!string.IsNullOrWhiteSpace(statement))
            {
                await ExecuteNonQueryAsync(statement.Trim());
            }
        }

        _logger.LogInformation("Database schema initialized successfully");
    }

    public async Task UpsertIssuesAsync(IEnumerable<JiraIssue> issues, CancellationToken cancellationToken = default)
    {
        var issueList = issues.ToList();
        if (!issueList.Any())
        {
            return;
        }

        _logger.LogInformation("Upserting {Count} issues", issueList.Count);

        var connection = await GetConnectionAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            foreach (var issue in issueList)
            {
                await UpsertIssueAsync(issue, connection, transaction);
            }

            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Successfully upserted {Count} issues", issueList.Count);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to upsert issues");
            throw;
        }
    }

    private async Task UpsertIssueAsync(JiraIssue issue, DuckDBConnection connection, DuckDBTransaction transaction)
    {
        // Upsert related entities first
        if (issue.Fields.Project != null)
        {
            await UpsertProjectAsync(issue.Fields.Project, connection, transaction);
        }

        if (issue.Fields.IssueType != null)
        {
            await UpsertIssueTypeAsync(issue.Fields.IssueType, connection, transaction);
        }

        if (issue.Fields.Status != null)
        {
            await UpsertStatusAsync(issue.Fields.Status, connection, transaction);
        }

        if (issue.Fields.Priority != null)
        {
            await UpsertPriorityAsync(issue.Fields.Priority, connection, transaction);
        }

        if (issue.Fields.Assignee != null)
        {
            await UpsertUserAsync(issue.Fields.Assignee, connection, transaction);
        }

        if (issue.Fields.Reporter != null)
        {
            await UpsertUserAsync(issue.Fields.Reporter, connection, transaction);
        }

        // Upsert the main issue
        var sql = @"
            INSERT INTO issues (
                issue_id, issue_key, project_id, issue_type_id, status_id, priority_id,
                assignee_id, reporter_id, summary, description, created_date, updated_date,
                resolved_date, resolution, labels, last_synced
            ) VALUES (
                $1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14, $15, $16
            )
            ON CONFLICT (issue_id) DO UPDATE SET
                status_id = EXCLUDED.status_id,
                assignee_id = EXCLUDED.assignee_id,
                summary = EXCLUDED.summary,
                description = EXCLUDED.description,
                updated_date = EXCLUDED.updated_date,
                resolved_date = EXCLUDED.resolved_date,
                resolution = EXCLUDED.resolution,
                labels = EXCLUDED.labels,
                last_synced = EXCLUDED.last_synced";

        using var command = new DuckDBCommand(sql, connection);
        command.Transaction = transaction;
        command.Parameters.Add(new DuckDBParameter("$1", issue.Id));
        command.Parameters.Add(new DuckDBParameter("$2", issue.Key));
        command.Parameters.Add(new DuckDBParameter("$3", issue.Fields.Project?.Id ?? string.Empty));
        command.Parameters.Add(new DuckDBParameter("$4", issue.Fields.IssueType?.Id ?? string.Empty));
        command.Parameters.Add(new DuckDBParameter("$5", issue.Fields.Status?.Id ?? string.Empty));
        command.Parameters.Add(new DuckDBParameter("$6", issue.Fields.Priority?.Id));
        command.Parameters.Add(new DuckDBParameter("$7", issue.Fields.Assignee?.AccountId));
        command.Parameters.Add(new DuckDBParameter("$8", issue.Fields.Reporter?.AccountId));
        command.Parameters.Add(new DuckDBParameter("$9", issue.Fields.Summary));
        command.Parameters.Add(new DuckDBParameter("$10", issue.Fields.Description));
        command.Parameters.Add(new DuckDBParameter("$11", issue.Fields.Created));
        command.Parameters.Add(new DuckDBParameter("$12", issue.Fields.Updated));
        command.Parameters.Add(new DuckDBParameter("$13", issue.Fields.ResolutionDate));
        command.Parameters.Add(new DuckDBParameter("$14", DBNull.Value)); // resolution - would need to be added to model
        command.Parameters.Add(new DuckDBParameter("$15", issue.Fields.Labels.ToArray()));
        command.Parameters.Add(new DuckDBParameter("$16", DateTime.UtcNow));

        await command.ExecuteNonQueryAsync();
    }

    private async Task UpsertProjectAsync(JiraProject project, DuckDBConnection connection, DuckDBTransaction transaction)
    {
        var sql = @"
            INSERT INTO projects (project_id, project_key, project_name, updated_date)
            VALUES ($1, $2, $3, $4)
            ON CONFLICT (project_id) DO UPDATE SET
                project_name = EXCLUDED.project_name,
                updated_date = EXCLUDED.updated_date";

        using var command = new DuckDBCommand(sql, connection);
        command.Transaction = transaction;
        command.Parameters.Add(new DuckDBParameter("$1", project.Id));
        command.Parameters.Add(new DuckDBParameter("$2", project.Key));
        command.Parameters.Add(new DuckDBParameter("$3", project.Name));
        command.Parameters.Add(new DuckDBParameter("$4", DateTime.UtcNow));

        await command.ExecuteNonQueryAsync();
    }

    private async Task UpsertIssueTypeAsync(JiraIssueType issueType, DuckDBConnection connection, DuckDBTransaction transaction)
    {
        var sql = @"
            INSERT INTO issue_types (issue_type_id, issue_type_name, icon_url)
            VALUES ($1, $2, $3)
            ON CONFLICT (issue_type_id) DO UPDATE SET
                issue_type_name = EXCLUDED.issue_type_name,
                icon_url = EXCLUDED.icon_url";

        using var command = new DuckDBCommand(sql, connection);
        command.Transaction = transaction;
        command.Parameters.Add(new DuckDBParameter("$1", issueType.Id));
        command.Parameters.Add(new DuckDBParameter("$2", issueType.Name));
        command.Parameters.Add(new DuckDBParameter("$3", issueType.IconUrl));

        await command.ExecuteNonQueryAsync();
    }

    private async Task UpsertStatusAsync(JiraStatus status, DuckDBConnection connection, DuckDBTransaction transaction)
    {
        // First upsert the status category if it exists
        if (status.StatusCategory != null)
        {
            var categorySql = @"
                INSERT INTO status_categories (status_category_id, category_name, category_key)
                VALUES ($1, $2, $3)
                ON CONFLICT (status_category_id) DO UPDATE SET
                    category_name = EXCLUDED.category_name,
                    category_key = EXCLUDED.category_key";

            using var categoryCommand = new DuckDBCommand(categorySql, connection);
            categoryCommand.Transaction = transaction;
            categoryCommand.Parameters.Add(new DuckDBParameter("$1", status.StatusCategory.Id));
            categoryCommand.Parameters.Add(new DuckDBParameter("$2", status.StatusCategory.Name));
            categoryCommand.Parameters.Add(new DuckDBParameter("$3", status.StatusCategory.Key));

            await categoryCommand.ExecuteNonQueryAsync();
        }

        var sql = @"
            INSERT INTO statuses (status_id, status_name, status_category_id)
            VALUES ($1, $2, $3)
            ON CONFLICT (status_id) DO UPDATE SET
                status_name = EXCLUDED.status_name,
                status_category_id = EXCLUDED.status_category_id";

        using var command = new DuckDBCommand(sql, connection);
        command.Transaction = transaction;
        command.Parameters.Add(new DuckDBParameter("$1", status.Id));
        command.Parameters.Add(new DuckDBParameter("$2", status.Name));
        command.Parameters.Add(new DuckDBParameter("$3", status.StatusCategory?.Id));

        await command.ExecuteNonQueryAsync();
    }

    private async Task UpsertPriorityAsync(JiraPriority priority, DuckDBConnection connection, DuckDBTransaction transaction)
    {
        var sql = @"
            INSERT INTO priorities (priority_id, priority_name, icon_url)
            VALUES ($1, $2, $3)
            ON CONFLICT (priority_id) DO UPDATE SET
                priority_name = EXCLUDED.priority_name,
                icon_url = EXCLUDED.icon_url";

        using var command = new DuckDBCommand(sql, connection);
        command.Transaction = transaction;
        command.Parameters.Add(new DuckDBParameter("$1", priority.Id));
        command.Parameters.Add(new DuckDBParameter("$2", priority.Name));
        command.Parameters.Add(new DuckDBParameter("$3", priority.IconUrl));

        await command.ExecuteNonQueryAsync();
    }

    private async Task UpsertUserAsync(JiraUser user, DuckDBConnection connection, DuckDBTransaction transaction)
    {
        var sql = @"
            INSERT INTO users (user_id, username, display_name, email_address, updated_date)
            VALUES ($1, $2, $3, $4, $5)
            ON CONFLICT (user_id) DO UPDATE SET
                username = EXCLUDED.username,
                display_name = EXCLUDED.display_name,
                email_address = EXCLUDED.email_address,
                updated_date = EXCLUDED.updated_date";

        using var command = new DuckDBCommand(sql, connection);
        command.Transaction = transaction;
        command.Parameters.Add(new DuckDBParameter("$1", user.AccountId ?? user.Name ?? "unknown"));
        command.Parameters.Add(new DuckDBParameter("$2", user.Name));
        command.Parameters.Add(new DuckDBParameter("$3", user.DisplayName));
        command.Parameters.Add(new DuckDBParameter("$4", user.EmailAddress));
        command.Parameters.Add(new DuckDBParameter("$5", DateTime.UtcNow));

        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<JiraIssue>> GetIssuesByKeysAsync(IEnumerable<string> issueKeys, CancellationToken cancellationToken = default)
    {
        var keyList = issueKeys.ToList();
        if (!keyList.Any())
        {
            return new List<JiraIssue>();
        }

        var connection = await GetConnectionAsync();
        var placeholders = string.Join(",", keyList.Select((_, i) => $"${i + 1}"));
        var sql = $"SELECT issue_id, issue_key, summary FROM issues WHERE issue_key IN ({placeholders})";

        using var command = new DuckDBCommand(sql, connection);
        for (int i = 0; i < keyList.Count; i++)
        {
            command.Parameters.Add(new DuckDBParameter($"${i + 1}", keyList[i]));
        }

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var issues = new List<JiraIssue>();

        while (await reader.ReadAsync(cancellationToken))
        {
            issues.Add(new JiraIssue
            {
                Id = reader.GetString("issue_id"),
                Key = reader.GetString("issue_key"),
                Fields = new JiraIssueFields
                {
                    Summary = reader.GetString("summary")
                }
            });
        }

        return issues;
    }

    public async Task<DateTime?> GetLastSyncTimestampAsync(CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync();
        var sql = "SELECT MAX(end_time) as last_sync FROM sync_history WHERE status = 'completed'";

        using var command = new DuckDBCommand(sql, connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result == DBNull.Value ? null : (DateTime?)result;
    }

    public async Task UpdateLastSyncTimestampAsync(DateTime timestamp, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync();
        var syncId = Guid.NewGuid().ToString();
        var sql = @"
            INSERT INTO sync_history (sync_id, sync_type, start_time, end_time, status, records_processed)
            VALUES ($1, $2, $3, $4, $5, $6)";

        using var command = new DuckDBCommand(sql, connection);
        command.Parameters.Add(new DuckDBParameter("$1", syncId));
        command.Parameters.Add(new DuckDBParameter("$2", "incremental"));
        command.Parameters.Add(new DuckDBParameter("$3", timestamp));
        command.Parameters.Add(new DuckDBParameter("$4", timestamp));
        command.Parameters.Add(new DuckDBParameter("$5", "completed"));
        command.Parameters.Add(new DuckDBParameter("$6", 0));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<List<ProjectSummary>> GetProjectSummariesAsync(CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync();
        var sql = "SELECT * FROM project_summary ORDER BY project_name";

        using var command = new DuckDBCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var summaries = new List<ProjectSummary>();

        while (await reader.ReadAsync(cancellationToken))
        {
            summaries.Add(new ProjectSummary
            {
                ProjectId = reader.GetString("project_id"),
                ProjectKey = reader.GetString("project_key"),
                ProjectName = reader.GetString("project_name"),
                TotalIssues = reader.GetInt32("total_issues"),
                ResolvedIssues = reader.GetInt32("resolved_issues"),
                OpenIssues = reader.GetInt32("open_issues"),
                InProgressIssues = reader.GetInt32("in_progress_issues"),
                AverageResolutionDays = reader.IsDBNull("avg_resolution_days") ? null : reader.GetDouble("avg_resolution_days"),
                FirstIssueDate = reader.IsDBNull("first_issue_date") ? null : reader.GetDateTime("first_issue_date"),
                LastUpdatedDate = reader.IsDBNull("last_updated_date") ? null : reader.GetDateTime("last_updated_date")
            });
        }

        return summaries;
    }

    public async Task<List<IssueMetric>> GetIssueMetricsAsync(string? projectKey = null, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync();
        var sql = projectKey != null 
            ? "SELECT * FROM issue_metrics WHERE project_key = $1 ORDER BY updated_date DESC"
            : "SELECT * FROM issue_metrics ORDER BY updated_date DESC LIMIT 1000";

        using var command = new DuckDBCommand(sql, connection);
        if (projectKey != null)
        {
            command.Parameters.Add(new DuckDBParameter("$1", projectKey));
        }

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var metrics = new List<IssueMetric>();

        while (await reader.ReadAsync(cancellationToken))
        {
            metrics.Add(new IssueMetric
            {
                IssueId = reader.GetString("issue_id"),
                IssueKey = reader.GetString("issue_key"),
                ProjectKey = reader.GetString("project_key"),
                ProjectName = reader.GetString("project_name"),
                IssueTypeName = reader.GetString("issue_type_name"),
                StatusName = reader.GetString("status_name"),
                StatusCategory = reader.GetString("status_category"),
                PriorityName = reader.IsDBNull("priority_name") ? null : reader.GetString("priority_name"),
                AssigneeName = reader.IsDBNull("assignee_name") ? null : reader.GetString("assignee_name"),
                ReporterName = reader.IsDBNull("reporter_name") ? null : reader.GetString("reporter_name"),
                Summary = reader.GetString("summary"),
                CreatedDate = reader.GetDateTime("created_date"),
                UpdatedDate = reader.GetDateTime("updated_date"),
                ResolvedDate = reader.IsDBNull("resolved_date") ? null : reader.GetDateTime("resolved_date"),
                AgeDays = reader.GetInt32("age_days"),
                ResolutionDays = reader.IsDBNull("resolution_days") ? null : reader.GetInt32("resolution_days"),
                Labels = new List<string>(), // Would need to parse array
                HoursSpent = reader.IsDBNull("hours_spent") ? null : reader.GetDouble("hours_spent"),
                HoursEstimated = reader.IsDBNull("hours_estimated") ? null : reader.GetDouble("hours_estimated")
            });
        }

        return metrics;
    }

    public async Task<List<Dictionary<string, object>>> ExecuteAnalyticalQueryAsync(string sql, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync();
        using var command = new DuckDBCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<Dictionary<string, object>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
            }
            results.Add(row);
        }

        return results;
    }

    public async Task<DatabaseHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await GetConnectionAsync();
            var status = new DatabaseHealthStatus
            {
                IsHealthy = true,
                DatabasePath = _config.FilePath
            };

            // Get database file size
            if (File.Exists(_config.FilePath))
            {
                status.DatabaseSizeBytes = new FileInfo(_config.FilePath).Length;
            }

            // Get record counts
            using var command = new DuckDBCommand("SELECT COUNT(*) FROM issues", connection);
            status.TotalIssues = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));

            using var projectCommand = new DuckDBCommand("SELECT COUNT(*) FROM projects", connection);
            status.TotalProjects = Convert.ToInt32(await projectCommand.ExecuteScalarAsync(cancellationToken));

            // Get last sync time
            status.LastSyncTime = await GetLastSyncTimestampAsync(cancellationToken);

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database health status");
            return new DatabaseHealthStatus
            {
                IsHealthy = false,
                DatabasePath = _config.FilePath,
                Issues = new List<string> { ex.Message }
            };
        }
    }

    private async Task<int> ExecuteNonQueryAsync(string sql)
    {
        var connection = await GetConnectionAsync();
        using var command = new DuckDBCommand(sql, connection);
        return await command.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Dispose();
            _disposed = true;
        }
    }
}