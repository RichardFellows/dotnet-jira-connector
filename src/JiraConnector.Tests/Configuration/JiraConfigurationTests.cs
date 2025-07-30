using FluentAssertions;
using JiraConnector.Configuration;
using System.ComponentModel.DataAnnotations;

namespace JiraConnector.Tests.Configuration;

public class JiraConfigurationTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var config = new JiraConfiguration();

        // Assert
        config.BaseUrl.Should().BeEmpty();
        config.PersonalAccessToken.Should().BeEmpty();
        config.Username.Should().BeEmpty();
        config.ProjectKeys.Should().BeEmpty();
        config.CustomFields.Should().BeEmpty();
        config.TimeoutSeconds.Should().Be(30);
        config.MaxResultsPerRequest.Should().Be(100);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-url")]
    public void Validation_InvalidBaseUrl_ReturnsError(string baseUrl)
    {
        // Arrange
        var config = new JiraConfiguration
        {
            BaseUrl = baseUrl,
            PersonalAccessToken = "valid-token",
            Username = "testuser"
        };

        // Act
        var results = ValidateObject(config);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.ErrorMessage!.Contains("BaseUrl"));
    }

    [Fact]
    public void Validation_ValidConfiguration_PassesValidation()
    {
        // Arrange
        var config = new JiraConfiguration
        {
            BaseUrl = "https://jira.example.com",
            PersonalAccessToken = "valid-token",
            Username = "testuser",
            TimeoutSeconds = 60,
            MaxResultsPerRequest = 200
        };

        // Act
        var results = ValidateObject(config);

        // Assert
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(301)]
    public void Validation_InvalidTimeoutSeconds_ReturnsError(int timeout)
    {
        // Arrange
        var config = new JiraConfiguration
        {
            BaseUrl = "https://jira.example.com",
            PersonalAccessToken = "valid-token",
            Username = "testuser",
            TimeoutSeconds = timeout
        };

        // Act
        var results = ValidateObject(config);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.ErrorMessage!.Contains("TimeoutSeconds"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void Validation_InvalidMaxResultsPerRequest_ReturnsError(int maxResults)
    {
        // Arrange
        var config = new JiraConfiguration
        {
            BaseUrl = "https://jira.example.com",
            PersonalAccessToken = "valid-token",
            Username = "testuser",
            MaxResultsPerRequest = maxResults
        };

        // Act
        var results = ValidateObject(config);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.ErrorMessage!.Contains("MaxResultsPerRequest"));
    }

    private static List<ValidationResult> ValidateObject(object obj)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(obj);
        Validator.TryValidateObject(obj, context, results, true);
        return results;
    }
}