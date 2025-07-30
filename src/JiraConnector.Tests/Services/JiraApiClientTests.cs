using FluentAssertions;
using JiraConnector.Configuration;
using JiraConnector.Models;
using JiraConnector.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace JiraConnector.Tests.Services;

public class JiraApiClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<ILogger<JiraApiClient>> _loggerMock;
    private readonly JiraConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly JiraApiClient _client;

    public JiraApiClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = new Mock<ILogger<JiraApiClient>>();
        _config = new JiraConfiguration
        {
            BaseUrl = "https://jira.example.com",
            PersonalAccessToken = "test-token",
            Username = "testuser",
            TimeoutSeconds = 30,
            MaxResultsPerRequest = 100
        };

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _client = new JiraApiClient(_httpClient, _config, _loggerMock.Object);
    }

    [Fact]
    public async Task SearchIssuesAsync_ValidRequest_ReturnsSearchResult()
    {
        // Arrange
        var request = new JiraSearchRequest
        {
            Jql = "project = TEST",
            StartAt = 0,
            MaxResults = 50
        };

        var searchResult = new JiraSearchResult
        {
            StartAt = 0,
            MaxResults = 50,
            Total = 1,
            Issues = new List<JiraIssue>
            {
                new JiraIssue
                {
                    Id = "123",
                    Key = "TEST-1",
                    Fields = new JiraIssueFields
                    {
                        Summary = "Test Issue"
                    }
                }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(searchResult);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse)
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.SearchIssuesAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Total.Should().Be(1);
        result.Issues.Should().HaveCount(1);
        result.Issues[0].Key.Should().Be("TEST-1");
        result.Issues[0].Fields.Summary.Should().Be("Test Issue");
    }

    [Fact]
    public async Task GetIssueAsync_ExistingIssue_ReturnsIssue()
    {
        // Arrange
        var issueKey = "TEST-123";
        var issue = new JiraIssue
        {
            Id = "123",
            Key = issueKey,
            Fields = new JiraIssueFields
            {
                Summary = "Test Issue",
                Description = "Test Description"
            }
        };

        var jsonResponse = JsonSerializer.Serialize(issue);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse)
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.GetIssueAsync(issueKey);

        // Assert
        result.Should().NotBeNull();
        result!.Key.Should().Be(issueKey);
        result.Fields.Summary.Should().Be("Test Issue");
    }

    [Fact]
    public async Task GetIssueAsync_NonExistentIssue_ReturnsNull()
    {
        // Arrange
        var issueKey = "NONEXISTENT-123";
        var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.GetIssueAsync(issueKey);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task TestConnectionAsync_SuccessfulConnection_ReturnsTrue()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"version\":\"8.0.0\"}")
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.TestConnectionAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnectionAsync_FailedConnection_ReturnsFalse()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.TestConnectionAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetIssuesUpdatedSinceAsync_ValidDate_ReturnsIssues()
    {
        // Arrange
        var since = DateTime.Now.AddDays(-7);
        var projectKeys = new[] { "TEST", "PROJ" };

        var searchResult = new JiraSearchResult
        {
            StartAt = 0,
            MaxResults = 100,
            Total = 2,
            Issues = new List<JiraIssue>
            {
                new JiraIssue { Id = "1", Key = "TEST-1" },
                new JiraIssue { Id = "2", Key = "PROJ-1" }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(searchResult);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse)
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.GetIssuesUpdatedSinceAsync(since, projectKeys);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(i => i.Key == "TEST-1");
        result.Should().Contain(i => i.Key == "PROJ-1");
    }

    [Fact]
    public void Constructor_SetsUpHttpClientCorrectly()
    {
        // Act & Assert
        _httpClient.BaseAddress.Should().Be(new Uri(_config.BaseUrl));
        _httpClient.Timeout.Should().Be(TimeSpan.FromSeconds(_config.TimeoutSeconds));
        _httpClient.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        _httpClient.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Basic");
    }
}