using System.ComponentModel.DataAnnotations;

namespace JiraConnector.Configuration;

/// <summary>
/// Configuration settings for DuckDB database
/// </summary>
public class DatabaseConfiguration
{
    public const string SectionName = "Database";

    /// <summary>
    /// Path to the DuckDB database file
    /// </summary>
    [Required]
    public string FilePath { get; set; } = "jira_data.duckdb";

    /// <summary>
    /// Whether to create the database if it doesn't exist
    /// </summary>
    public bool CreateIfNotExists { get; set; } = true;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    [Range(1, 300)]
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to enable WAL mode for better performance
    /// </summary>
    public bool EnableWalMode { get; set; } = true;

    /// <summary>
    /// Memory limit for DuckDB operations (in MB)
    /// </summary>
    [Range(64, 32768)]
    public int MemoryLimitMB { get; set; } = 1024;
}