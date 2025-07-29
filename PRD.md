# Product Requirements Document (PRD)
## JIRA Data Synchronization Tool

**Version**: 1.0  
**Date**: 2025-07-29  
**Status**: Draft

---

## 1. Executive Summary

### Purpose
Develop a .NET application that synchronizes issue data from an internal JIRA Server instance to a local DuckDB database for offline analysis and reporting. The tool will maintain an up-to-date offline replica of selected JIRA projects with full issue history and custom field support.

### Success Criteria
- Complete offline replica of JIRA project data in DuckDB
- Incremental synchronization with change tracking
- Full custom field and changelog capture
- Python-friendly data structure for downstream analytics
- Reliable, automated sync process

---

## 2. Business Requirements

### Target User
- **You** - Primary user needing offline JIRA data for analysis
- **Python Analytics** - Downstream consumers via notebooks

### Business Goals
- Enable offline JIRA data analysis without API rate limits
- Capture complete issue lifecycle including transitions
- Support custom business intelligence and reporting
- Provide fast, local data access for Python notebooks
- Maintain historical data integrity

---

## 3. Functional Requirements

### 3.1 Data Synchronization

#### Initial Sync
- **Project Selection**: Configure 2 primary projects with extensibility for additional projects
- **Full Data Pull**: Download all issues from configured projects
- **Custom Field Mapping**: Identify and capture organization-specific custom fields
- **Changelog Extraction**: Retrieve complete issue transition history
- **Database Population**: Create and populate DuckDB tables with normalized data

#### Incremental Sync
- **Change Detection**: Identify modified/new issues since last sync
- **Delta Processing**: Update only changed records to minimize API calls
- **Conflict Resolution**: Handle concurrent changes appropriately  
- **Sync Scheduling**: Support manual and automated sync operations
- **Progress Tracking**: Monitor sync progress and handle interruptions

#### Data Capture
- **Standard Fields**: All JIRA standard issue fields (key, summary, description, status, etc.)
- **Custom Fields**: Organization-specific fields with proper type handling
- **Issue Transitions**: Complete changelog with status changes, field updates, timestamps
- **Metadata**: Created/updated dates, assignee history, reporter information
- **Relationships**: Issue links, subtasks, parent relationships

### 3.2 Database Management

#### DuckDB Integration
- **Schema Design**: Optimized table structure for analytical queries
- **Data Types**: Proper mapping of JIRA field types to DuckDB types
- **Indexing**: Performance indexes for common query patterns
- **Compression**: Efficient storage of historical data
- **Python Compatibility**: Schema designed for easy Python/pandas integration

#### Data Integrity
- **Referential Integrity**: Maintain relationships between issues and transitions
- **Duplicate Prevention**: Handle re-sync scenarios without data corruption
- **Backup/Recovery**: Database backup and restoration capabilities
- **Migration Support**: Schema evolution for future enhancements

### 3.3 Configuration & Extensibility

#### Project Configuration
- **Multi-Project Support**: Configure NDS and CRRF projects initially, allow easy addition of more
- **Custom Field Selection**: Explicitly configure which custom fields to capture per project
- **Sync Policies**: Define sync frequency and scope per project
- **Authentication**: Personal Access Token (PAT) based authentication
- **Database Path**: Configurable DuckDB file location

#### Extensibility
- **Plugin Architecture**: Allow custom field processors and transformers
- **Export Formats**: Support additional output formats beyond DuckDB
- **Custom Queries**: Pre-defined queries for common analysis patterns

---

## 4. Non-Functional Requirements

### 4.1 Performance
- **Sync Speed**: Complete initial sync of ~10,000 issues within 30 minutes
- **Incremental Updates**: Process incremental changes within 5 minutes
- **Database Performance**: Sub-second query response for typical analytics
- **Memory Usage**: Efficient memory usage during large data processing

### 4.2 Reliability
- **Sync Reliability**: Handle network interruptions and resume gracefully
- **Error Handling**: Clear error messages with actionable guidance
- **Rate Limiting**: Respect JIRA Server API limits to avoid blocking
- **Data Consistency**: Ensure database remains consistent during failed syncs

### 4.3 Security
- **Credential Protection**: Secure storage of JIRA authentication credentials
- **Local Data**: Database file should be accessible only to authorized users
- **Network Security**: Use HTTPS for all JIRA API communication
- **Audit Trail**: Log all sync operations for troubleshooting

### 4.4 Maintainability
- **Test Coverage**: 90%+ code coverage focusing on sync logic and data integrity
- **Configuration**: Clear, documented configuration files
- **Logging**: Comprehensive logging for sync operations and errors
- **Documentation**: Setup and usage documentation for future maintenance

---

## 5. Technical Specifications

### 5.1 Architecture
- **Target Framework**: .NET 8.0 (Console Application)
- **Database**: DuckDB with .NET bindings
- **HTTP Client**: HttpClient with connection pooling for JIRA API
- **Serialization**: System.Text.Json for JIRA API responses
- **Resilience**: Polly for retry policies and rate limiting
- **Configuration**: Microsoft.Extensions.Configuration (JSON/YAML) for projects, fields, and paths
- **Authentication**: Personal Access Token (PAT) for JIRA Server
- **Logging**: Serilog with structured logging
- **Scheduling**: Optional background service for automated sync

### 5.2 Database Schema Design
```sql
-- Core Issues Table
CREATE TABLE issues (
    issue_key VARCHAR PRIMARY KEY,
    project_key VARCHAR,
    summary TEXT,
    description TEXT,
    status VARCHAR,
    priority VARCHAR,
    issue_type VARCHAR,
    created_date TIMESTAMP,
    updated_date TIMESTAMP,
    resolved_date TIMESTAMP,
    assignee VARCHAR,
    reporter VARCHAR,
    -- Custom fields as JSON or separate columns
    custom_fields JSON,
    last_sync_timestamp TIMESTAMP
);

-- Issue Transitions/Changelog
CREATE TABLE issue_transitions (
    id BIGINT PRIMARY KEY,
    issue_key VARCHAR,
    field_name VARCHAR,
    from_value VARCHAR,
    to_value VARCHAR,
    changed_date TIMESTAMP,
    author VARCHAR,
    FOREIGN KEY (issue_key) REFERENCES issues(issue_key)
);

-- Sync Metadata
CREATE TABLE sync_history (
    id BIGINT PRIMARY KEY,
    project_key VARCHAR,
    sync_start TIMESTAMP,
    sync_end TIMESTAMP,
    issues_processed INTEGER,
    sync_type VARCHAR, -- 'full' or 'incremental'
    last_updated_issue TIMESTAMP
);
```

### 5.3 Application Design
- **Console Application**: Command-line interface with arguments
- **Service Architecture**: Separate services for JIRA API, Database, and Sync logic
- **Configuration-Driven**: YAML/JSON configuration for projects and fields
- **Async/Await**: Full async support for I/O operations
- **Error Recovery**: Resume capability for interrupted syncs

### 5.4 Testing Strategy
- **Unit Tests**: xUnit with FluentAssertions and Moq
- **Integration Tests**: In-memory DuckDB and mock JIRA responses
- **End-to-End Tests**: Real JIRA dev instance (if available)
- **Data Integrity Tests**: Verify sync accuracy and consistency

---

## 6. User Stories & Acceptance Criteria

### Epic 1: Initial Data Synchronization
**As a user, I want to create an offline copy of my JIRA project data**

#### Story 1.1: Configure Projects
```
Given I have a Personal Access Token for JIRA Server
When I configure the sync tool with NDS and CRRF projects
And specify which custom fields to capture per project
And set the DuckDB file path
Then the tool should validate connectivity and permissions
And confirm configured custom fields exist
```

#### Story 1.2: Perform Initial Sync
```
Given valid project configuration
When I run the initial sync command
Then all issues from configured projects should be downloaded
And stored in a local DuckDB database with complete field data
And issue transition history should be captured
```

### Epic 2: Incremental Updates
**As a user, I want to keep my offline data current with minimal overhead**

#### Story 2.1: Detect Changes
```
Given an existing synchronized database
When I run an incremental sync
Then only issues modified since last sync should be processed
And new issues should be added to the database
```

#### Story 2.2: Update Transitions
```
Given issues with new transitions since last sync
When incremental sync processes these issues
Then new changelog entries should be added to the transitions table
And existing transition history should remain unchanged
```

### Epic 3: Data Access for Analytics
**As a Python developer, I want to easily access the synchronized data**

#### Story 3.1: Python-Friendly Schema
```
Given a populated DuckDB database
When I connect from a Python notebook using DuckDB connector
Then I should be able to query issues and transitions easily
And data types should map cleanly to pandas DataFrames
```

#### Story 3.2: Custom Field Access
```
Given issues with organization-specific custom fields
When I query the database from Python
Then custom field data should be accessible and properly typed
And I should be able to filter and analyze based on custom fields
```

---

## 7. Dependencies & Constraints

### External Dependencies
- **Internal JIRA Server**: Must have API access and appropriate permissions
- **DuckDB .NET Bindings**: Reliable .NET integration for DuckDB
- **JIRA REST API**: Compatible with JIRA Server version in use

### Technical Constraints
- **JIRA Server Limits**: Respect API rate limits to avoid service disruption
- **Network Connectivity**: Reliable connection to internal JIRA instance
- **Local Storage**: Sufficient disk space for database file
- **Custom Field Complexity**: Handle varying custom field types and structures

### Operational Constraints
- **Authentication**: Personal Access Token must have appropriate JIRA permissions
- **Project Access**: PAT must have read access to NDS and CRRF projects and their histories
- **Maintenance Windows**: Sync should handle JIRA maintenance periods gracefully

---

## 8. Success Metrics

### Functional Success
- **Data Completeness**: 100% of issues synchronized with all required fields
- **Sync Accuracy**: Zero data inconsistencies between JIRA and local database
- **Custom Field Capture**: All organization-specific fields properly mapped and stored
- **Python Integration**: Seamless data access from Python notebooks

### Performance Success
- **Initial Sync**: Complete within 30 minutes for ~10,000 issues
- **Incremental Sync**: Complete within 5 minutes for typical daily changes
- **Database Queries**: Sub-second response for analytical queries
- **Reliability**: 99%+ successful sync completion rate

### Usability Success
- **Configuration**: Easy setup and project configuration
- **Error Recovery**: Graceful handling of interruptions and errors
- **Monitoring**: Clear visibility into sync status and data freshness

---

## 9. Configuration Structure

### Sample Configuration File (appsettings.json)
```json
{
  "JiraSettings": {
    "BaseUrl": "https://your-jira-server.com",
    "PersonalAccessToken": "your-pat-here"
  },
  "DatabaseSettings": {
    "FilePath": "./data/jira-sync.duckdb"
  },
  "Projects": [
    {
      "Key": "NDS",
      "Name": "Network Development System",
      "CustomFields": [
        "customfield_10001",
        "customfield_10002"
      ]
    },
    {
      "Key": "CRRF", 
      "Name": "Customer Requirements and Reporting Framework",
      "CustomFields": [
        "customfield_10003",
        "customfield_10004"
      ]
    }
  ],
  "SyncSettings": {
    "BatchSize": 100,
    "MaxRetries": 3,
    "DelayBetweenRequests": 100
  }
}
```

## 10. Remaining Questions

1. **Historical Data**: How far back should we sync issue history on initial load?
2. **Sync Frequency**: What's the optimal balance between data freshness and system load?
3. **Error Handling**: How should we handle issues that fail to sync (skip, retry, alert)?
4. **Schema Evolution**: How should we handle JIRA schema changes over time?
5. **Security**: Should we encrypt the local DuckDB file or store PAT securely?

---

## 11. Implementation Phases

### Phase 1: Core Infrastructure (Week 1)
- Project setup with TDD framework
- JIRA API client with authentication
- Basic DuckDB integration and schema creation
- Configuration system for projects

### Phase 2: Data Synchronization (Week 2)
- Issue fetching and parsing logic
- Database insert/update operations
- Custom field discovery and mapping
- Basic incremental sync logic

### Phase 3: Change Tracking (Week 3)
- Changelog/transition capture
- Incremental sync optimization
- Error handling and retry logic
- Progress monitoring and logging

### Phase 4: Production Readiness (Week 4)
- Performance optimization
- Comprehensive testing
- Documentation and setup guides
- Python integration validation

---

*This PRD will be updated as requirements evolve and stakeholder feedback is incorporated.*