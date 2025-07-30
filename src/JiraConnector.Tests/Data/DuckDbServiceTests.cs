using FluentAssertions;
using JiraConnector.Configuration;
using JiraConnector.Data;
using JiraConnector.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiraConnector.Tests.Data;

public class DuckDbServiceTests : IDisposable
{
    private readonly Mock<ILogger<DuckDbService>> _loggerMock;
    private readonly DatabaseConfiguration _config;
    private readonly DuckDbService _service;
    private readonly string _testDbPath;

    public DuckDbServiceTests()
    {
        _loggerMock = new Mock<ILogger<DuckDbService>>();
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_jira_{Guid.NewGuid()}.duckdb");

        _config = new DatabaseConfiguration
        {
            FilePath = _testDbPath,
            CreateIfNotExists = true,
            ConnectionTimeoutSeconds = 30,
            EnableWalMode = false, // Disable for tests
            MemoryLimitMB = 256
        };

        _service = new DuckDbService(_config, _loggerMock.Object);
    }

    [Fact]
    public async Task InitializeDatabaseAsync_CreatesDatabase_Successfully()
    {
        // Act
        await _service.InitializeDatabaseAsync();

        // Assert
        File.Exists(_testDbPath).Should().BeTrue();

        // Verify some key tables exist
        var healthStatus = await _service.GetHealthStatusAsync();
        healthStatus.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task UpsertIssuesAsync_SingleIssue_InsertsSuccessfully()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();

        var issue = CreateTestIssue("TEST-1", "Test Issue");

        // Act
        await _service.UpsertIssuesAsync(new[] { issue });

        // Assert
        var retrievedIssues = await _service.GetIssuesByKeysAsync(new[] { "TEST-1" });
        retrievedIssues.Should().HaveCount(1);
        retrievedIssues[0].Key.Should().Be("TEST-1");
        retrievedIssues[0].Fields.Summary.Should().Be("Test Issue");
    }

    [Fact]
    public async Task UpsertIssuesAsync_MultipleIssues_ProcessesAllSuccessfully()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();

        var issues = new[]
        {
            CreateTestIssue("TEST-1", "First Issue"),
            CreateTestIssue("TEST-2", "Second Issue"),
            CreateTestIssue("PROJ-1", "Third Issue")
        };

        // Act
        await _service.UpsertIssuesAsync(issues);

        // Assert
        var retrievedIssues = await _service.GetIssuesByKeysAsync(new[] { "TEST-1", "TEST-2", "PROJ-1" });
        retrievedIssues.Should().HaveCount(3);
        retrievedIssues.Should().Contain(i => i.Key == "TEST-1");
        retrievedIssues.Should().Contain(i => i.Key == "TEST-2");
        retrievedIssues.Should().Contain(i => i.Key == "PROJ-1");
    }

    [Fact]
    public async Task UpsertIssuesAsync_UpdateExistingIssue_UpdatesSuccessfully()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();

        var originalIssue = CreateTestIssue("TEST-1", "Original Summary");
        await _service.UpsertIssuesAsync(new[] { originalIssue });

        var updatedIssue = CreateTestIssue("TEST-1", "Updated Summary");
        updatedIssue.Fields.Updated = DateTime.UtcNow;

        // Act
        await _service.UpsertIssuesAsync(new[] { updatedIssue });

        // Assert
        var retrievedIssues = await _service.GetIssuesByKeysAsync(new[] { "TEST-1" });
        retrievedIssues.Should().HaveCount(1);
        retrievedIssues[0].Fields.Summary.Should().Be("Updated Summary");
    }

    [Fact]
    public async Task GetIssuesByKeysAsync_NonExistentKeys_ReturnsEmptyList()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();

        // Act
        var result = await _service.GetIssuesByKeysAsync(new[] { "NONEXISTENT-1", "MISSING-2" });

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLastSyncTimestampAsync_NoSyncHistory_ReturnsNull()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();

        // Act
        var result = await _service.GetLastSyncTimestampAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateLastSyncTimestampAsync_ValidTimestamp_UpdatesSuccessfully()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        var timestamp = DateTime.UtcNow;

        // Act
        await _service.UpdateLastSyncTimestampAsync(timestamp);

        // Assert
        var result = await _service.GetLastSyncTimestampAsync();
        result.Should().NotBeNull();
        result.Should().BeCloseTo(timestamp, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetProjectSummariesAsync_WithData_ReturnsCorrectSummaries()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();

        var issues = new[]
        {
            CreateTestIssue("PROJ1-1", "Issue 1", "PROJ1", "Project One"),
            CreateTestIssue("PROJ1-2", "Issue 2", "PROJ1", "Project One"),
            CreateTestIssue("PROJ2-1", "Issue 3", "PROJ2", "Project Two")
        };

        await _service.UpsertIssuesAsync(issues);

        // Act
        var summaries = await _service.GetProjectSummariesAsync();

        // Assert
        summaries.Should().HaveCount(2);
        summaries.Should().Contain(s => s.ProjectKey == "PROJ1" && s.TotalIssues == 2);
        summaries.Should().Contain(s => s.ProjectKey == "PROJ2" && s.TotalIssues == 1);
    }

    [Fact]
    public async Task GetIssueMetricsAsync_WithProjectFilter_ReturnsFilteredResults()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();

        var issues = new[]
        {
            CreateTestIssue("PROJ1-1", "Issue 1", "PROJ1", "Project One"),
            CreateTestIssue("PROJ2-1", "Issue 2", "PROJ2", "Project Two")
        };

        await _service.UpsertIssuesAsync(issues);

        // Act
        var metrics = await _service.GetIssueMetricsAsync("PROJ1");

        // Assert
        metrics.Should().HaveCount(1);
        metrics[0].ProjectKey.Should().Be("PROJ1");
        metrics[0].IssueKey.Should().Be("PROJ1-1");
    }

    [Fact]
    public async Task GetHealthStatusAsync_HealthyDatabase_ReturnsHealthyStatus()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        await _service.UpsertIssuesAsync(new[] { CreateTestIssue("TEST-1", "Test Issue") });

        // Act
        var status = await _service.GetHealthStatusAsync();

        // Assert
        status.IsHealthy.Should().BeTrue();
        status.DatabasePath.Should().Be(_testDbPath);
        status.TotalIssues.Should().BeGreaterThan(0);
        status.Issues.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAnalyticalQueryAsync_ValidQuery_ReturnsResults()
    {
        // Arrange
        await _service.InitializeDatabaseAsync();
        await _service.UpsertIssuesAsync(new[] { CreateTestIssue("TEST-1", "Test Issue") });

        // Act
        var results = await _service.ExecuteAnalyticalQueryAsync("SELECT COUNT(*) as issue_count FROM issues");

        // Assert
        results.Should().HaveCount(1);
        results[0].Should().ContainKey("issue_count");
        Convert.ToInt32(results[0]["issue_count"]).Should().BeGreaterThan(0);
    }

    private static JiraIssue CreateTestIssue(string key, string summary, string projectKey = "TEST", string projectName = "Test Project")
    {
        return new JiraIssue
        {
            Id = Guid.NewGuid().ToString(),
            Key = key,
            Fields = new JiraIssueFields
            {
                Summary = summary,
                Description = "Test description",
                Created = DateTime.UtcNow.AddDays(-10),
                Updated = DateTime.UtcNow.AddDays(-1),
                Project = new JiraProject
                {
                    Id = Guid.NewGuid().ToString(),
                    Key = projectKey,
                    Name = projectName
                },
                IssueType = new JiraIssueType
                {
                    Id = "1",
                    Name = "Task"
                },
                Status = new JiraStatus
                {
                    Id = "1",
                    Name = "Open",
                    StatusCategory = new JiraStatusCategory
                    {
                        Id = 1,
                        Name = "To Do",
                        Key = "new"
                    }
                },
                Priority = new JiraPriority
                {
                    Id = "3",
                    Name = "Medium"
                },
                Reporter = new JiraUser
                {
                    AccountId = "reporter-id",
                    DisplayName = "Test Reporter"
                }
            }
        };
    }

    public void Dispose()
    {
        _service?.Dispose();

        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}