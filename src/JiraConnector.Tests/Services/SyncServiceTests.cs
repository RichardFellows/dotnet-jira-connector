using FluentAssertions;
using JiraConnector.Configuration;
using JiraConnector.Data;
using JiraConnector.Models;
using JiraConnector.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiraConnector.Tests.Services;

public class SyncServiceTests
{
    private readonly Mock<IJiraApiClient> _jiraClientMock;
    private readonly Mock<IDatabaseService> _databaseServiceMock;
    private readonly Mock<ILogger<SyncService>> _loggerMock;
    private readonly SyncConfiguration _syncConfig;
    private readonly JiraConfiguration _jiraConfig;
    private readonly SyncService _syncService;

    public SyncServiceTests()
    {
        _jiraClientMock = new Mock<IJiraApiClient>();
        _databaseServiceMock = new Mock<IDatabaseService>();
        _loggerMock = new Mock<ILogger<SyncService>>();

        _syncConfig = new SyncConfiguration
        {
            IntervalMinutes = 30,
            BatchSize = 10,
            LookbackDays = 7,
            FullSyncOnStartup = false
        };

        _jiraConfig = new JiraConfiguration
        {
            BaseUrl = "https://test-jira.com",
            Username = "testuser",
            PersonalAccessToken = "test-token",
            ProjectKeys = new List<string> { "TEST", "PROJ" }
        };

        _syncService = new SyncService(
            _jiraClientMock.Object,
            _databaseServiceMock.Object,
            _syncConfig,
            _jiraConfig,
            _loggerMock.Object);
    }

    [Fact]
    public async Task PerformFullSyncAsync_SuccessfulSync_ReturnsSuccessResult()
    {
        // Arrange
        var testIssues = new List<JiraIssue>
        {
            CreateTestIssue("TEST-1", "First Issue"),
            CreateTestIssue("TEST-2", "Second Issue")
        };

        _jiraClientMock.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);
        
        _databaseServiceMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(new DatabaseHealthStatus { IsHealthy = true });
        
        _databaseServiceMock.Setup(x => x.InitializeDatabaseAsync(It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask);

        _jiraClientMock.Setup(x => x.GetAllIssuesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(testIssues);

        _databaseServiceMock.Setup(x => x.UpsertIssuesAsync(It.IsAny<IEnumerable<JiraIssue>>(), It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask);

        _databaseServiceMock.Setup(x => x.UpdateLastSyncTimestampAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask);

        // Act
        var result = await _syncService.PerformFullSyncAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.SyncType.Should().Be(SyncType.Full);
        result.IssuesProcessed.Should().Be(2);
        result.ErrorMessage.Should().BeNull();
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);

        // Verify calls
        _jiraClientMock.Verify(x => x.GetAllIssuesAsync(
            It.Is<IEnumerable<string>>(keys => keys.Contains("TEST") && keys.Contains("PROJ")),
            It.IsAny<CancellationToken>()), Times.Once);

        _databaseServiceMock.Verify(x => x.UpsertIssuesAsync(
            It.Is<IEnumerable<JiraIssue>>(issues => issues.Count() == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PerformFullSyncAsync_JiraConnectionFails_ReturnsFailureResult()
    {
        // Arrange
        _jiraClientMock.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(false);

        _databaseServiceMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(new DatabaseHealthStatus { IsHealthy = true });

        // Act
        var result = await _syncService.PerformFullSyncAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Connection test failed");
        result.IssuesProcessed.Should().Be(0);
    }

    [Fact]
    public async Task PerformIncrementalSyncAsync_WithPreviousSync_SyncsOnlyUpdatedIssues()
    {
        // Arrange
        var lastSync = DateTime.UtcNow.AddHours(-2);
        var updatedIssues = new List<JiraIssue>
        {
            CreateTestIssue("TEST-1", "Updated Issue")
        };

        _jiraClientMock.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);

        _databaseServiceMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(new DatabaseHealthStatus { IsHealthy = true });

        _databaseServiceMock.Setup(x => x.GetLastSyncTimestampAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(lastSync);

        _jiraClientMock.Setup(x => x.GetIssuesUpdatedSinceAsync(
                It.IsAny<DateTime>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedIssues);

        _databaseServiceMock.Setup(x => x.UpsertIssuesAsync(It.IsAny<IEnumerable<JiraIssue>>(), It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask);

        _databaseServiceMock.Setup(x => x.UpdateLastSyncTimestampAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask);

        // Act
        var result = await _syncService.PerformIncrementalSyncAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.SyncType.Should().Be(SyncType.Incremental);
        result.IssuesProcessed.Should().Be(1);

        // Verify it called the incremental sync method
        _jiraClientMock.Verify(x => x.GetIssuesUpdatedSinceAsync(
            It.Is<DateTime>(date => date <= lastSync),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PerformIncrementalSyncAsync_NoLastSync_PerformsFullSync()
    {
        // Arrange
        _jiraClientMock.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);

        _databaseServiceMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(new DatabaseHealthStatus { IsHealthy = true });

        _databaseServiceMock.Setup(x => x.GetLastSyncTimestampAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync((DateTime?)null);

        _databaseServiceMock.Setup(x => x.InitializeDatabaseAsync(It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask);

        _jiraClientMock.Setup(x => x.GetAllIssuesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<JiraIssue>());

        _databaseServiceMock.Setup(x => x.UpdateLastSyncTimestampAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask);

        // Act
        var result = await _syncService.PerformIncrementalSyncAsync();

        // Assert
        result.SyncType.Should().Be(SyncType.Full);
        
        // Verify it called the full sync method instead
        _jiraClientMock.Verify(x => x.GetAllIssuesAsync(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TestConnectionsAsync_BothHealthy_ReturnsTrue()
    {
        // Arrange
        _jiraClientMock.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);

        _databaseServiceMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(new DatabaseHealthStatus { IsHealthy = true });

        // Act
        var result = await _syncService.TestConnectionsAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnectionsAsync_DatabaseUnhealthy_ReturnsFalse()
    {
        // Arrange
        _jiraClientMock.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);

        _databaseServiceMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(new DatabaseHealthStatus 
                           { 
                               IsHealthy = false,
                               Issues = new List<string> { "Database connection failed" }
                           });

        // Act
        var result = await _syncService.TestConnectionsAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetLastSyncStatusAsync_ReturnsCorrectStatus()
    {
        // Arrange
        var lastSync = DateTime.UtcNow.AddHours(-1);
        var healthStatus = new DatabaseHealthStatus
        {
            IsHealthy = true,
            TotalIssues = 100,
            TotalProjects = 5
        };

        _databaseServiceMock.Setup(x => x.GetLastSyncTimestampAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(lastSync);

        _databaseServiceMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(healthStatus);

        // Act
        var status = await _syncService.GetLastSyncStatusAsync();

        // Assert
        status.LastIncrementalSync.Should().Be(lastSync);
        status.IsHealthy.Should().BeTrue();
        status.TotalIssues.Should().Be(100);
        status.TotalProjects.Should().Be(5);
        status.LastError.Should().BeNull();
    }

    [Fact]
    public async Task PerformFullSyncAsync_BatchProcessing_ProcessesInBatches()
    {
        // Arrange
        var manyIssues = Enumerable.Range(1, 25)
            .Select(i => CreateTestIssue($"TEST-{i}", $"Issue {i}"))
            .ToList();

        _jiraClientMock.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);

        _databaseServiceMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(new DatabaseHealthStatus { IsHealthy = true });

        _databaseServiceMock.Setup(x => x.InitializeDatabaseAsync(It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask);

        _jiraClientMock.Setup(x => x.GetAllIssuesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(manyIssues);

        _databaseServiceMock.Setup(x => x.UpsertIssuesAsync(It.IsAny<IEnumerable<JiraIssue>>(), It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask);

        _databaseServiceMock.Setup(x => x.UpdateLastSyncTimestampAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask);

        // Act
        var result = await _syncService.PerformFullSyncAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.IssuesProcessed.Should().Be(25);

        // Verify it was called in batches (25 issues / 10 batch size = 3 calls)
        _databaseServiceMock.Verify(x => x.UpsertIssuesAsync(
            It.Is<IEnumerable<JiraIssue>>(batch => batch.Count() <= _syncConfig.BatchSize),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    private static JiraIssue CreateTestIssue(string key, string summary)
    {
        return new JiraIssue
        {
            Id = Guid.NewGuid().ToString(),
            Key = key,
            Fields = new JiraIssueFields
            {
                Summary = summary,
                Description = "Test description",
                Created = DateTime.UtcNow.AddDays(-5),
                Updated = DateTime.UtcNow.AddDays(-1),
                Project = new JiraProject
                {
                    Id = "1",
                    Key = "TEST",
                    Name = "Test Project"
                },
                IssueType = new JiraIssueType
                {
                    Id = "10001",
                    Name = "Task"
                },
                Status = new JiraStatus
                {
                    Id = "1",
                    Name = "Open"
                }
            }
        };
    }
}