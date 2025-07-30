using System.ComponentModel.DataAnnotations;

namespace JiraConnector.Configuration;

/// <summary>
/// Configuration settings for data synchronization
/// </summary>
public class SyncConfiguration
{
    public const string SectionName = "Sync";

    /// <summary>
    /// How often to perform incremental sync (in minutes)
    /// </summary>
    [Range(1, 1440)]
    public int IntervalMinutes { get; set; } = 30;

    /// <summary>
    /// Whether to perform full sync on startup
    /// </summary>
    public bool FullSyncOnStartup { get; set; } = false;

    /// <summary>
    /// Number of days to look back for changes during incremental sync
    /// </summary>
    [Range(1, 90)]
    public int LookbackDays { get; set; } = 7;

    /// <summary>
    /// Batch size for processing issues
    /// </summary>
    [Range(1, 1000)]
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// Whether to sync issue comments
    /// </summary>
    public bool SyncComments { get; set; } = true;

    /// <summary>
    /// Whether to sync issue attachments metadata
    /// </summary>
    public bool SyncAttachments { get; set; } = true;

    /// <summary>
    /// Whether to sync issue changelog/history
    /// </summary>
    public bool SyncChangelog { get; set; } = true;
}