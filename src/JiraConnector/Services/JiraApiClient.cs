using JiraConnector.Configuration;
using JiraConnector.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;

namespace JiraConnector.Services;

/// <summary>
/// HTTP client for JIRA Server REST API with PAT authentication
/// </summary>
public class JiraApiClient : IJiraApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JiraConfiguration _config;
    private readonly ILogger<JiraApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public JiraApiClient(HttpClient httpClient, JiraConfiguration config, ILogger<JiraApiClient> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;

        // Configure HTTP client
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

        // Set up PAT authentication header
        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.Username}:{_config.PersonalAccessToken}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Configure JSON serialization
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<JiraSearchResult> SearchIssuesAsync(JiraSearchRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching issues with JQL: {Jql}", request.Jql);

        var queryParams = HttpUtility.ParseQueryString(string.Empty);
        queryParams["jql"] = request.Jql;
        queryParams["startAt"] = request.StartAt.ToString();
        queryParams["maxResults"] = request.MaxResults.ToString();

        if (request.Fields.Count > 0)
        {
            queryParams["fields"] = string.Join(",", request.Fields);
        }

        if (request.Expand.Count > 0)
        {
            queryParams["expand"] = string.Join(",", request.Expand);
        }

        var requestUri = $"/rest/api/2/search?{queryParams}";

        try
        {
            var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var searchResult = JsonSerializer.Deserialize<JiraSearchResult>(jsonContent, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize search result");

            _logger.LogInformation("Retrieved {Count} issues out of {Total} total", 
                searchResult.Issues.Count, searchResult.Total);

            return searchResult;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to search issues with JQL: {Jql}", request.Jql);
            throw;
        }
    }

    public async Task<JiraIssue?> GetIssueAsync(string issueKey, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving issue: {IssueKey}", issueKey);

        var requestUri = $"/rest/api/2/issue/{issueKey}";

        try
        {
            var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Issue not found: {IssueKey}", issueKey);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var issue = JsonSerializer.Deserialize<JiraIssue>(jsonContent, _jsonOptions);

            _logger.LogInformation("Successfully retrieved issue: {IssueKey}", issueKey);
            return issue;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to retrieve issue: {IssueKey}", issueKey);
            throw;
        }
    }

    public async Task<List<JiraIssue>> GetIssuesUpdatedSinceAsync(DateTime since, IEnumerable<string>? projectKeys = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving issues updated since: {Since}", since);

        var jql = new StringBuilder($"updated >= '{since:yyyy-MM-dd HH:mm}'");

        if (projectKeys?.Any() == true)
        {
            var projectList = string.Join(", ", projectKeys.Select(k => $"'{k}'"));
            jql.Append($" AND project IN ({projectList})");
        }

        var request = new JiraSearchRequest
        {
            Jql = jql.ToString(),
            MaxResults = _config.MaxResultsPerRequest
        };

        var allIssues = new List<JiraIssue>();
        var startAt = 0;

        do
        {
            request.StartAt = startAt;
            var result = await SearchIssuesAsync(request, cancellationToken);
            allIssues.AddRange(result.Issues);
            startAt += result.Issues.Count;

            if (result.Issues.Count < request.MaxResults)
                break;

        } while (startAt < 10000); // JIRA limit

        _logger.LogInformation("Retrieved {Count} issues updated since {Since}", allIssues.Count, since);
        return allIssues;
    }

    public async Task<List<JiraIssue>> GetAllIssuesAsync(IEnumerable<string> projectKeys, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all issues for projects: {Projects}", string.Join(", ", projectKeys));

        var projectList = string.Join(", ", projectKeys.Select(k => $"'{k}'"));
        var jql = $"project IN ({projectList}) ORDER BY updated DESC";

        var request = new JiraSearchRequest
        {
            Jql = jql,
            MaxResults = _config.MaxResultsPerRequest
        };

        var allIssues = new List<JiraIssue>();
        var startAt = 0;

        do
        {
            request.StartAt = startAt;
            var result = await SearchIssuesAsync(request, cancellationToken);
            allIssues.AddRange(result.Issues);
            startAt += result.Issues.Count;

            if (result.Issues.Count < request.MaxResults)
                break;

        } while (startAt < 10000); // JIRA limit

        _logger.LogInformation("Retrieved {Count} total issues for projects: {Projects}", 
            allIssues.Count, string.Join(", ", projectKeys));
        return allIssues;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Testing connection to JIRA server");

        try
        {
            var response = await _httpClient.GetAsync("/rest/api/2/serverInfo", cancellationToken);
            var success = response.IsSuccessStatusCode;

            if (success)
            {
                _logger.LogInformation("Successfully connected to JIRA server");
            }
            else
            {
                _logger.LogWarning("Failed to connect to JIRA server. Status: {StatusCode}", response.StatusCode);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection to JIRA server");
            return false;
        }
    }
}