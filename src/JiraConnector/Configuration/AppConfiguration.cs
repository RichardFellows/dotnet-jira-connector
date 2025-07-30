using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;

namespace JiraConnector.Configuration;

/// <summary>
/// Root application configuration containing all subsystem configurations
/// </summary>
public class AppConfiguration
{
    public JiraConfiguration Jira { get; set; } = new();
    public DatabaseConfiguration Database { get; set; } = new();
    public SyncConfiguration Sync { get; set; } = new();

    /// <summary>
    /// Validates all configuration sections
    /// </summary>
    /// <returns>List of validation errors, empty if valid</returns>
    public List<ValidationResult> Validate()
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(this);

        // Validate Jira configuration
        var jiraContext = new ValidationContext(Jira);
        Validator.TryValidateObject(Jira, jiraContext, results, true);

        // Validate Database configuration
        var dbContext = new ValidationContext(Database);
        Validator.TryValidateObject(Database, dbContext, results, true);

        // Validate Sync configuration
        var syncContext = new ValidationContext(Sync);
        Validator.TryValidateObject(Sync, syncContext, results, true);

        return results;
    }

    /// <summary>
    /// Loads and validates configuration from IConfiguration
    /// </summary>
    public static AppConfiguration LoadAndValidate(IConfiguration configuration)
    {
        var appConfig = new AppConfiguration();

        // Bind configuration sections
        configuration.GetSection(JiraConfiguration.SectionName).Bind(appConfig.Jira);
        configuration.GetSection(DatabaseConfiguration.SectionName).Bind(appConfig.Database);
        configuration.GetSection(SyncConfiguration.SectionName).Bind(appConfig.Sync);

        // Validate configuration
        var validationResults = appConfig.Validate();
        if (validationResults.Count > 0)
        {
            var errors = string.Join(", ", validationResults.Select(r => r.ErrorMessage));
            throw new InvalidOperationException($"Configuration validation failed: {errors}");
        }

        return appConfig;
    }
}

/// <summary>
/// Extension methods for registering configuration services
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Registers configuration services with the DI container
    /// </summary>
    public static IServiceCollection AddAppConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var appConfig = AppConfiguration.LoadAndValidate(configuration);

        services.AddSingleton(appConfig);
        services.AddSingleton(appConfig.Jira);
        services.AddSingleton(appConfig.Database);
        services.AddSingleton(appConfig.Sync);

        return services;
    }
}