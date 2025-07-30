# JIRA Connector - .NET 8.0

A robust .NET application for synchronizing JIRA issues with a local DuckDB database, optimized for analytics and reporting. Built using Test-Driven Development (TDD) methodology.

## 🚀 Features

- **JIRA Server Integration** - Connect to JIRA Server with Personal Access Token (PAT) authentication
- **Incremental Synchronization** - Efficient sync of only changed issues since last update
- **DuckDB Analytics** - Optimized database schema with analytical views for reporting
- **Batch Processing** - Configurable batch sizes for optimal performance
- **Comprehensive Logging** - Structured logging with Serilog
- **Configuration Management** - Flexible, validated configuration system
- **Rich CLI Interface** - User-friendly console application with statistics

## 📋 Prerequisites

- .NET 8.0 SDK
- JIRA Server with API access
- Personal Access Token for authentication

## 🔧 Installation

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd dotnet-jira-connector
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Build the solution:**
   ```bash
   dotnet build
   ```

## ⚙️ Configuration

Create an `appsettings.json` file in the console application directory:

```json
{
  "Jira": {
    "BaseUrl": "https://your-jira-server.com",
    "Username": "your-username", 
    "PersonalAccessToken": "your-pat-token-here",
    "ProjectKeys": ["PROJ1", "PROJ2"],
    "TimeoutSeconds": 30,
    "MaxResultsPerRequest": 100
  },
  "Database": {
    "FilePath": "jira_data.duckdb",
    "CreateIfNotExists": true,
    "MemoryLimitMB": 1024
  },
  "Sync": {
    "IntervalMinutes": 30,
    "FullSyncOnStartup": false,
    "LookbackDays": 7,
    "BatchSize": 50
  }
}
```

### Environment Variables

You can also configure using environment variables:
- `Jira__BaseUrl`
- `Jira__Username`
- `Jira__PersonalAccessToken`
- `Database__FilePath`

## 🚀 Usage

### Full Synchronization
```bash
dotnet run --project src/JiraConnector.Console -- --full-sync
```

### Incremental Synchronization
```bash
dotnet run --project src/JiraConnector.Console
```

### Command Line Options
- `--full-sync` - Perform full synchronization of all issues
- `--help` - Display help information

## 📊 Database Schema

The application creates an optimized DuckDB schema with:

- **Issues** - Core issue data with metrics
- **Projects, Users, Components** - Related entities
- **Custom Fields** - Flexible custom field storage
- **Comments & Changelog** - Complete audit trail
- **Analytical Views** - Pre-built views for reporting

### Key Views

- `issue_metrics` - Issue analytics with calculated fields
- `project_summary` - Project-level statistics

## 🧪 Testing

Run the test suite:
```bash
dotnet test
```

Generate coverage report:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Test Structure

- **Unit Tests** - Individual component testing
- **Integration Tests** - Database and API integration
- **Configuration Tests** - Validation and binding tests

## 🏗️ Architecture

### Project Structure
```
src/
├── JiraConnector/              # Core library
│   ├── Configuration/          # Configuration management  
│   ├── Data/                   # Database layer & schema
│   ├── Models/                 # JIRA data models
│   └── Services/               # API client & sync services
├── JiraConnector.Tests/        # Comprehensive test suite
└── JiraConnector.Console/      # CLI application
```

### Key Components

- **Configuration System** - Typed configuration with validation
- **JIRA API Client** - HTTP client with resilience patterns
- **Database Service** - Repository pattern with DuckDB
- **Sync Service** - Orchestrates full and incremental sync
- **Console Application** - User interface and orchestration

## 🔒 Security

- **Secure Credential Storage** - Configuration-based credential management
- **PAT Authentication** - Uses Personal Access Tokens (no passwords)
- **Input Validation** - Comprehensive validation of all inputs
- **SQL Injection Prevention** - Parameterized queries throughout

## ⚡ Performance

- **Batch Processing** - Configurable batch sizes for optimal throughput
- **Connection Pooling** - Efficient HTTP connection reuse  
- **Incremental Sync** - Only sync changed data
- **Analytical Indexes** - Optimized database indexes for queries
- **Memory Management** - Configurable memory limits

## 📈 Analytics

The DuckDB database is optimized for analytical queries:

```sql
-- Issues by project and status
SELECT project_key, status_name, COUNT(*) as issue_count
FROM issue_metrics 
GROUP BY project_key, status_name;

-- Average resolution time by project
SELECT project_key, AVG(resolution_days) as avg_resolution_days
FROM issue_metrics 
WHERE resolution_days IS NOT NULL
GROUP BY project_key;

-- Issue velocity over time
SELECT DATE_TRUNC('month', created_date) as month,
       COUNT(*) as issues_created
FROM issue_metrics
GROUP BY month
ORDER BY month;
```

## 🛠️ Development

### Prerequisites for Development
- .NET 8.0 SDK
- IDE with C# support (VS Code, Visual Studio, JetBrains Rider)

### Development Commands
```bash
# Build
dotnet build

# Run tests
dotnet test

# Run console app
dotnet run --project src/JiraConnector.Console

# Format code
dotnet format
```

### Contributing

1. Follow TDD methodology (Red → Green → Refactor)
2. Maintain >90% test coverage
3. Use conventional commit messages
4. Follow SOLID principles

## 📋 Project Status

- ✅ **Configuration Management** - Complete with validation
- ✅ **JIRA API Integration** - Full API client with PAT auth
- ✅ **Database Schema** - Optimized DuckDB analytics schema  
- ✅ **Synchronization Engine** - Full & incremental sync
- ✅ **Console Application** - Rich CLI with statistics
- ✅ **Test Coverage** - Comprehensive test suite (90%+)

## 🐛 Troubleshooting

### Common Issues

**Connection Failures:**
- Verify JIRA server URL and credentials
- Check network connectivity and firewalls
- Ensure PAT has appropriate permissions

**Database Issues:**
- Check file permissions for database path
- Verify DuckDB native libraries are available
- Check memory limits and disk space

**Sync Issues:**
- Review sync configuration (batch size, timeouts)
- Check JIRA project permissions
- Verify system time synchronization

### Debug Mode
Enable debug logging by setting:
```json
{
  "Logging": {
    "LogLevel": {
      "JiraConnector": "Debug"
    }
  }
}
```

## 📄 License

[Your License Here]

## 👥 Authors

Built using Test-Driven Development methodology with specialized development agents.

---

*For detailed technical documentation, see the `docs/` directory.*