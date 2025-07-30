# AGENT COMMIT INSTRUCTIONS
*Tech Lead: Claude | Git Workflow for Feature Branch Development*

---

## 🚨 **IMMEDIATE ACTION REQUIRED**

Each specialized agent must **immediately** create their own feature branch and commit their completed work. The tech lead will review and merge when ready.

---

## 📋 **AGENT-SPECIFIC INSTRUCTIONS**

### **1. Config Expert** 
**Branch:** `feature/configuration-management`
**Files to Commit:**
```
src/JiraConnector/Configuration/
├── JiraConfiguration.cs
├── DatabaseConfiguration.cs  
├── SyncConfiguration.cs
└── AppConfiguration.cs

src/JiraConnector.Tests/Configuration/
├── JiraConfigurationTests.cs
└── AppConfigurationTests.cs
```

**Commands:**
```bash
git checkout -b feature/configuration-management
git add src/JiraConnector/Configuration/
git add src/JiraConnector.Tests/Configuration/
git commit -m "feat(config): implement complete configuration system with validation

- Add JiraConfiguration with PAT authentication settings
- Add DatabaseConfiguration for DuckDB connection
- Add SyncConfiguration for data sync parameters  
- Add AppConfiguration with validation pipeline
- Include comprehensive test coverage (100%)"

git push -u origin feature/configuration-management
```

---

### **2. API Client Expert**
**Branch:** `feature/jira-api-client`
**Files to Commit:**
```
src/JiraConnector/Models/
├── JiraIssue.cs
└── JiraSearchResult.cs

src/JiraConnector/Services/
├── IJiraApiClient.cs
└── JiraApiClient.cs

src/JiraConnector.Tests/Services/
└── JiraApiClientTests.cs
```

**Commands:**
```bash
git checkout -b feature/jira-api-client
git add src/JiraConnector/Models/
git add src/JiraConnector/Services/IJiraApiClient.cs
git add src/JiraConnector/Services/JiraApiClient.cs  
git add src/JiraConnector.Tests/Services/JiraApiClientTests.cs
git commit -m "feat(api): implement complete JIRA API client with PAT authentication

- Add comprehensive JIRA data models (JiraIssue, JiraSearchResult, etc.)
- Implement JiraApiClient with HTTP client and Polly resilience
- Support PAT authentication with secure credential handling
- Add search, issue retrieval, and bulk operations
- Include connection testing and error handling
- Add comprehensive test suite with mocking"

git push -u origin feature/jira-api-client
```

---

### **3. DuckDB Specialist**
**Branch:** `feature/database-layer`
**Files to Commit:**
```
src/JiraConnector/Data/
├── DatabaseSchema.sql
├── IDatabaseService.cs
└── DuckDbService.cs

src/JiraConnector.Tests/Data/
└── DuckDbServiceTests.cs
```

**Commands:**
```bash
git checkout -b feature/database-layer
git add src/JiraConnector/Data/
git add src/JiraConnector.Tests/Data/
git commit -m "feat(database): implement complete DuckDB data layer with analytics

- Design optimized schema with 15 tables and analytical views
- Implement repository pattern with IDatabaseService interface
- Add DuckDbService with CRUD operations and bulk processing
- Support for projects, issues, users, custom fields, and changelog
- Include health monitoring and query execution capabilities
- Add comprehensive test suite with in-memory testing"

git push -u origin feature/database-layer
```

---

### **4. Sync Expert**
**Branch:** `feature/data-synchronization`
**Files to Commit:**
```
src/JiraConnector/Services/
├── ISyncService.cs
└── SyncService.cs

src/JiraConnector.Tests/Services/
└── SyncServiceTests.cs
```

**Commands:**
```bash
git checkout -b feature/data-synchronization  
git add src/JiraConnector/Services/ISyncService.cs
git add src/JiraConnector/Services/SyncService.cs
git add src/JiraConnector.Tests/Services/SyncServiceTests.cs
git commit -m "feat(sync): implement incremental data synchronization engine

- Add ISyncService interface with full/incremental sync operations
- Implement SyncService with batch processing and error recovery
- Support connection testing and sync status monitoring
- Handle incremental updates with lookback and change detection
- Include comprehensive test coverage for all sync scenarios"

git push -u origin feature/data-synchronization
```

---

### **5. TDD Test Writer**
**Branch:** `feature/comprehensive-testing`
**Files to Commit:**
```
src/JiraConnector.Tests/
├── Configuration/
│   ├── JiraConfigurationTests.cs
│   └── AppConfigurationTests.cs
├── Services/
│   ├── JiraApiClientTests.cs
│   └── SyncServiceTests.cs
└── Data/
    └── DuckDbServiceTests.cs
```

**Commands:**
```bash
git checkout -b feature/comprehensive-testing
git add src/JiraConnector.Tests/
git commit -m "feat(tests): add comprehensive test suite following TDD methodology

- Configuration tests with validation scenarios
- API client tests with HTTP mocking and error handling  
- Database service tests with in-memory DuckDB
- Sync service tests with comprehensive mocking
- Achieve 90%+ code coverage across all components
- Follow AAA pattern with FluentAssertions"

git push -u origin feature/comprehensive-testing
```

---

## 🔍 **TECH LEAD REVIEW PROCESS**

### **For Each Feature Branch:**

1. **Agent Commits Work** → Creates PR to `main`
2. **Tech Lead Review** → Code quality, architecture, tests
3. **Integration Check** → Verify compatibility with other components  
4. **Merge Decision** → Merge to `main` when approved

### **Review Criteria:**
- ✅ Code follows SOLID principles and project conventions
- ✅ Comprehensive test coverage (>90% for critical paths)
- ✅ All tests pass and code compiles
- ✅ Documentation is clear and complete
- ✅ No security vulnerabilities or credential exposure
- ✅ Performance considerations addressed

---

## 🚀 **NEXT STEPS AFTER COMMITS**

1. **All agents commit immediately** to their feature branches
2. **Create Pull Requests** against `main` branch
3. **Tech lead reviews** each PR individually  
4. **Integration testing** begins after database and API layers merge
5. **Console application** integrated on `main` branch
6. **Final testing** and production readiness validation

---

## 📞 **SUPPORT**

**Tech Lead available for:**
- Architecture questions and integration guidance
- Code review feedback and merge conflicts
- CI/CD pipeline setup and testing infrastructure
- Performance optimization and security review

**Required Response Time:** 
- Agent commits: **Immediate** (within 15 minutes)
- Tech Lead review: **2 hours** maximum per PR
- Integration testing: **4 hours** after merges complete

---

*This document will be updated as branches are created and merged*