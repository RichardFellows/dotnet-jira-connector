using System.Text.Json.Serialization;

namespace JiraConnector.Models;

/// <summary>
/// Represents a JIRA issue with essential fields
/// </summary>
public class JiraIssue
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("self")]
    public string Self { get; set; } = string.Empty;

    [JsonPropertyName("fields")]
    public JiraIssueFields Fields { get; set; } = new();
}

/// <summary>
/// JIRA issue fields
/// </summary>
public class JiraIssueFields
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("issuetype")]
    public JiraIssueType? IssueType { get; set; }

    [JsonPropertyName("status")]
    public JiraStatus? Status { get; set; }

    [JsonPropertyName("priority")]
    public JiraPriority? Priority { get; set; }

    [JsonPropertyName("assignee")]
    public JiraUser? Assignee { get; set; }

    [JsonPropertyName("reporter")]
    public JiraUser? Reporter { get; set; }

    [JsonPropertyName("project")]
    public JiraProject? Project { get; set; }

    [JsonPropertyName("created")]
    public DateTime Created { get; set; }

    [JsonPropertyName("updated")]
    public DateTime Updated { get; set; }

    [JsonPropertyName("resolutiondate")]
    public DateTime? ResolutionDate { get; set; }

    [JsonPropertyName("labels")]
    public List<string> Labels { get; set; } = new();

    [JsonPropertyName("components")]
    public List<JiraComponent> Components { get; set; } = new();

    [JsonPropertyName("fixVersions")]
    public List<JiraVersion> FixVersions { get; set; } = new();
}

public class JiraIssueType
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; set; }
}

public class JiraStatus
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("statusCategory")]
    public JiraStatusCategory? StatusCategory { get; set; }
}

public class JiraStatusCategory
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
}

public class JiraPriority
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; set; }
}

public class JiraUser
{
    [JsonPropertyName("accountId")]
    public string? AccountId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }
}

public class JiraProject
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class JiraComponent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class JiraVersion
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("released")]
    public bool Released { get; set; }
}