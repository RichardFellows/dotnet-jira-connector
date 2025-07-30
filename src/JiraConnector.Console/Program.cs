using JiraConnector.Configuration;
using JiraConnector.Data;
using JiraConnector.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace JiraConnector.Console;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/jira-connector-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            System.Console.WriteLine("🚀 JIRA Connector Starting...");

            var host = CreateHostBuilder(args).Build();
            
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;

            var syncService = services.GetRequiredService<ISyncService>();
            var logger = services.GetRequiredService<ILogger<Program>>();
            var appConfig = services.GetRequiredService<AppConfiguration>();

            // Display configuration summary
            DisplayConfiguration(appConfig);

            // Test connections
            System.Console.WriteLine("\n🔍 Testing connections...");
            var connectionsOk = await syncService.TestConnectionsAsync();
            
            if (!connectionsOk)
            {
                System.Console.WriteLine("❌ Connection tests failed. Please check your configuration.");
                return 1;
            }

            System.Console.WriteLine("✅ All connections successful!");

            // Determine sync type from command line args
            var shouldPerformFullSync = args.Contains("--full-sync") || appConfig.Sync.FullSyncOnStartup;

            // Perform synchronization
            System.Console.WriteLine($"\n📥 Starting {(shouldPerformFullSync ? "full" : "incremental")} synchronization...");
            
            var result = shouldPerformFullSync 
                ? await syncService.PerformFullSyncAsync()
                : await syncService.PerformIncrementalSyncAsync();

            // Display results
            DisplaySyncResults(result);

            if (!result.Success)
            {
                return 1;
            }

            // Show database statistics
            await DisplayDatabaseStats(services);

            System.Console.WriteLine("\n🎉 JIRA Connector completed successfully!");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            System.Console.WriteLine($"💥 Fatal error: {ex.Message}");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                      .AddEnvironmentVariables()
                      .AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Register configuration
                services.AddAppConfiguration(context.Configuration);

                // Register HTTP client for JIRA API
                services.AddHttpClient<IJiraApiClient, JiraApiClient>();

                // Register database service
                services.AddSingleton<IDatabaseService, DuckDbService>();

                // Register sync service
                services.AddTransient<ISyncService, SyncService>();
            });

    static void DisplayConfiguration(AppConfiguration config)
    {
        System.Console.WriteLine("\n⚙️  Configuration Summary:");
        System.Console.WriteLine($"   JIRA Server: {config.Jira.BaseUrl}");
        System.Console.WriteLine($"   Username: {config.Jira.Username}");
        System.Console.WriteLine($"   Database: {config.Database.FilePath}");
        System.Console.WriteLine($"   Projects: {(config.Jira.ProjectKeys.Any() ? string.Join(", ", config.Jira.ProjectKeys) : "All accessible")}");
        System.Console.WriteLine($"   Sync Interval: {config.Sync.IntervalMinutes} minutes");
        System.Console.WriteLine($"   Batch Size: {config.Sync.BatchSize}");
    }

    static void DisplaySyncResults(SyncResult result)
    {
        System.Console.WriteLine($"\n📊 Synchronization Results:");
        System.Console.WriteLine($"   Status: {(result.Success ? "✅ Success" : "❌ Failed")}");
        System.Console.WriteLine($"   Duration: {result.Duration:mm\\:ss}");
        System.Console.WriteLine($"   Issues Processed: {result.IssuesProcessed:N0}");
        System.Console.WriteLine($"   Issues Inserted: {result.IssuesInserted:N0}");
        System.Console.WriteLine($"   Issues Updated: {result.IssuesUpdated:N0}");
        
        if (result.Warnings.Any())
        {
            System.Console.WriteLine($"   Warnings: {result.Warnings.Count}");
            foreach (var warning in result.Warnings)
            {
                System.Console.WriteLine($"     ⚠️  {warning}");
            }
        }

        if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
        {
            System.Console.WriteLine($"   Error: {result.ErrorMessage}");
        }
    }

    static async Task DisplayDatabaseStats(IServiceProvider services)
    {
        try
        {
            var databaseService = services.GetRequiredService<IDatabaseService>();
            var summaries = await databaseService.GetProjectSummariesAsync();

            if (summaries.Any())
            {
                System.Console.WriteLine("\n📈 Project Statistics:");
                System.Console.WriteLine($"{"Project",-15} {"Total",-8} {"Open",-6} {"InProg",-7} {"Done",-6} {"Avg Days",-9}");
                System.Console.WriteLine(new string('-', 65));

                foreach (var summary in summaries.Take(10)) // Show top 10
                {
                    System.Console.WriteLine($"{summary.ProjectKey,-15} {summary.TotalIssues,-8:N0} " +
                                           $"{summary.OpenIssues,-6:N0} {summary.InProgressIssues,-7:N0} " +
                                           $"{summary.ResolvedIssues,-6:N0} {summary.AverageResolutionDays,-9:F1}");
                }

                if (summaries.Count > 10)
                {
                    System.Console.WriteLine($"... and {summaries.Count - 10} more projects");
                }

                var totalIssues = summaries.Sum(s => s.TotalIssues);
                var totalResolved = summaries.Sum(s => s.ResolvedIssues);
                System.Console.WriteLine($"\n📊 Overall: {totalIssues:N0} total issues, {totalResolved:N0} resolved ({(double)totalResolved/totalIssues*100:F1}%)");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"⚠️  Could not retrieve database statistics: {ex.Message}");
        }
    }
}