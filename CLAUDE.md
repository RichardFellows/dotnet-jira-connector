# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
A .NET application for connecting to and interacting with JIRA APIs, developed using Test-Driven Development (TDD) methodology.

## Planned Architecture
- **Target Framework**: .NET 8.0
- **Testing Framework**: xUnit with FluentAssertions
- **Mocking**: Moq
- **HTTP Client**: HttpClient with Polly for resilience

## TDD Workflow
1. **Red**: Write a failing test
2. **Green**: Write minimal code to make test pass
3. **Refactor**: Improve code while keeping tests green

## Initial Setup Commands

### Create Project Structure
```bash
# Create new solution
dotnet new sln -n JiraConnector

# Create class library
dotnet new classlib -n JiraConnector -o src/JiraConnector

# Create test project
dotnet new xunit -n JiraConnector.Tests -o src/JiraConnector.Tests

# Add projects to solution
dotnet sln add src/JiraConnector/JiraConnector.csproj
dotnet sln add src/JiraConnector.Tests/JiraConnector.Tests.csproj

# Add project references
dotnet add src/JiraConnector.Tests/JiraConnector.Tests.csproj reference src/JiraConnector/JiraConnector.csproj

# Add essential NuGet packages
dotnet add src/JiraConnector/JiraConnector.csproj package Microsoft.Extensions.Http
dotnet add src/JiraConnector.Tests/JiraConnector.Tests.csproj package FluentAssertions
dotnet add src/JiraConnector.Tests/JiraConnector.Tests.csproj package Moq
```

### Development Commands (once project exists)
```bash
# Build solution
dotnet build

# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run tests in watch mode
dotnet watch test

# Format code
dotnet format

# Clean build artifacts
dotnet clean
```

## Testing Conventions
- Test class naming: `[ClassUnderTest]Tests`
- Test method naming: `[MethodUnderTest]_[Scenario]_[ExpectedResult]`
- Use AAA pattern: Arrange, Act, Assert
- Use FluentAssertions for readable assertions

## Specialized Agents
The following specialized agents are available in the `.claude/agents/` directory:

### Primary Interface
- `/tech-lead` - **Main orchestration agent** acting as tech lead/product owner. Use this as your primary interface for project management and coordination.

### Core Domain Agents
- `/duckdb-specialist` - **Database design and analytics optimization** for DuckDB schema, queries, and Python integration
- `/sync-expert` - **Data synchronization strategies** for incremental sync, change detection, and consistency
- `/config-expert` - **Configuration management** for project setup, custom fields, and credential handling
- `/api-client-expert` - **JIRA API integration** with PAT authentication and data retrieval

### Development Support Agents
- `/tdd-test-writer` - Writing unit tests following TDD methodology
- `/dotnet-refactor` - Refactoring code while maintaining test coverage  
- `/test-coverage-analyzer` - Analyzing and improving test coverage
- `/performance-optimizer` - Optimizing .NET application performance