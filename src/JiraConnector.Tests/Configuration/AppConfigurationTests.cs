using FluentAssertions;
using JiraConnector.Configuration;
using Microsoft.Extensions.Configuration;

namespace JiraConnector.Tests.Configuration;

public class AppConfigurationTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var config = new AppConfiguration();

        // Assert
        config.Jira.Should().NotBeNull();
        config.Database.Should().NotBeNull();
        config.Sync.Should().NotBeNull();
    }

    [Fact]
    public void Validate_ValidConfiguration_ReturnsNoErrors()
    {
        // Arrange
        var config = new AppConfiguration
        {
            Jira = new JiraConfiguration
            {
                BaseUrl = "https://jira.example.com",
                PersonalAccessToken = "valid-token",
                Username = "testuser"
            },
            Database = new DatabaseConfiguration
            {
                FilePath = "test.duckdb"
            },
            Sync = new SyncConfiguration()
        };

        // Act
        var results = config.Validate();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_InvalidJiraConfig_ReturnsErrors()
    {
        // Arrange
        var config = new AppConfiguration
        {
            Jira = new JiraConfiguration
            {
                BaseUrl = "", // Invalid
                PersonalAccessToken = "valid-token",
                Username = "testuser"
            }
        };

        // Act
        var results = config.Validate();

        // Assert
        results.Should().NotBeEmpty();
    }

    [Fact]
    public void LoadAndValidate_ValidConfiguration_ReturnsAppConfig()
    {
        // Arrange
        var configDict = new Dictionary<string, string?>
        {
            ["Jira:BaseUrl"] = "https://jira.example.com",
            ["Jira:PersonalAccessToken"] = "valid-token",
            ["Jira:Username"] = "testuser",
            ["Database:FilePath"] = "test.duckdb",
            ["Sync:IntervalMinutes"] = "60"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        // Act
        var appConfig = AppConfiguration.LoadAndValidate(configuration);

        // Assert
        appConfig.Should().NotBeNull();
        appConfig.Jira.BaseUrl.Should().Be("https://jira.example.com");
        appConfig.Jira.Username.Should().Be("testuser");
        appConfig.Database.FilePath.Should().Be("test.duckdb");
        appConfig.Sync.IntervalMinutes.Should().Be(60);
    }

    [Fact]
    public void LoadAndValidate_InvalidConfiguration_ThrowsException()
    {
        // Arrange
        var configDict = new Dictionary<string, string?>
        {
            ["Jira:BaseUrl"] = "", // Invalid
            ["Jira:PersonalAccessToken"] = "valid-token",
            ["Jira:Username"] = "testuser"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        // Act & Assert
        var act = () => AppConfiguration.LoadAndValidate(configuration);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Configuration validation failed:*");
    }
}