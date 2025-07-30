-- JIRA Connector Database Schema for DuckDB
-- Optimized for analytical queries and Python integration

-- Projects table
CREATE TABLE IF NOT EXISTS projects (
    project_id VARCHAR PRIMARY KEY,
    project_key VARCHAR UNIQUE NOT NULL,
    project_name VARCHAR NOT NULL,
    project_type VARCHAR,
    lead VARCHAR,
    created_date TIMESTAMP,
    updated_date TIMESTAMP,
    INDEX idx_project_key (project_key)
);

-- Issue types table
CREATE TABLE IF NOT EXISTS issue_types (
    issue_type_id VARCHAR PRIMARY KEY,
    issue_type_name VARCHAR NOT NULL,
    icon_url VARCHAR,
    description VARCHAR,
    INDEX idx_issue_type_name (issue_type_name)
);

-- Status categories table
CREATE TABLE IF NOT EXISTS status_categories (
    status_category_id INTEGER PRIMARY KEY,
    category_name VARCHAR NOT NULL,
    category_key VARCHAR NOT NULL,
    INDEX idx_category_key (category_key)
);

-- Statuses table
CREATE TABLE IF NOT EXISTS statuses (
    status_id VARCHAR PRIMARY KEY,
    status_name VARCHAR NOT NULL,
    status_category_id INTEGER,
    description VARCHAR,
    FOREIGN KEY (status_category_id) REFERENCES status_categories(status_category_id),
    INDEX idx_status_name (status_name)
);

-- Priorities table
CREATE TABLE IF NOT EXISTS priorities (
    priority_id VARCHAR PRIMARY KEY,
    priority_name VARCHAR NOT NULL,
    icon_url VARCHAR,
    description VARCHAR,
    INDEX idx_priority_name (priority_name)
);

-- Users table
CREATE TABLE IF NOT EXISTS users (
    user_id VARCHAR PRIMARY KEY,
    username VARCHAR,
    display_name VARCHAR NOT NULL,
    email_address VARCHAR,
    active BOOLEAN DEFAULT true,
    created_date TIMESTAMP,
    updated_date TIMESTAMP,
    INDEX idx_username (username),
    INDEX idx_email (email_address)
);

-- Components table
CREATE TABLE IF NOT EXISTS components (
    component_id VARCHAR PRIMARY KEY,
    component_name VARCHAR NOT NULL,
    project_id VARCHAR NOT NULL,
    description VARCHAR,
    lead VARCHAR,
    FOREIGN KEY (project_id) REFERENCES projects(project_id),
    INDEX idx_component_project (project_id),
    INDEX idx_component_name (component_name)
);

-- Versions table
CREATE TABLE IF NOT EXISTS versions (
    version_id VARCHAR PRIMARY KEY,
    version_name VARCHAR NOT NULL,
    project_id VARCHAR NOT NULL,
    description VARCHAR,
    released BOOLEAN DEFAULT false,
    release_date DATE,
    start_date DATE,
    FOREIGN KEY (project_id) REFERENCES projects(project_id),
    INDEX idx_version_project (project_id),
    INDEX idx_version_name (version_name)
);

-- Main issues table - optimized for analytics
CREATE TABLE IF NOT EXISTS issues (
    issue_id VARCHAR PRIMARY KEY,
    issue_key VARCHAR UNIQUE NOT NULL,
    project_id VARCHAR NOT NULL,
    issue_type_id VARCHAR NOT NULL,
    status_id VARCHAR NOT NULL,
    priority_id VARCHAR,
    assignee_id VARCHAR,
    reporter_id VARCHAR,
    summary VARCHAR NOT NULL,
    description TEXT,
    environment TEXT,
    
    -- Dates for time-based analysis
    created_date TIMESTAMP NOT NULL,
    updated_date TIMESTAMP NOT NULL,
    resolved_date TIMESTAMP,
    due_date DATE,
    
    -- Metrics for analytics
    original_estimate_seconds INTEGER,
    remaining_estimate_seconds INTEGER,
    time_spent_seconds INTEGER,
    
    -- Labels as array for easy querying
    labels VARCHAR[],
    
    -- Resolution information
    resolution VARCHAR,
    resolution_date TIMESTAMP,
    
    -- Sync metadata
    last_synced TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign keys
    FOREIGN KEY (project_id) REFERENCES projects(project_id),
    FOREIGN KEY (issue_type_id) REFERENCES issue_types(issue_type_id),
    FOREIGN KEY (status_id) REFERENCES statuses(status_id),
    FOREIGN KEY (priority_id) REFERENCES priorities(priority_id),
    FOREIGN KEY (assignee_id) REFERENCES users(user_id),
    FOREIGN KEY (reporter_id) REFERENCES users(user_id),
    
    -- Optimized indexes for common queries
    INDEX idx_issue_key (issue_key),
    INDEX idx_project_id (project_id),
    INDEX idx_status_id (status_id),
    INDEX idx_assignee_id (assignee_id),
    INDEX idx_created_date (created_date),
    INDEX idx_updated_date (updated_date),
    INDEX idx_resolved_date (resolved_date),
    INDEX idx_last_synced (last_synced)
);

-- Issue components junction table
CREATE TABLE IF NOT EXISTS issue_components (
    issue_id VARCHAR NOT NULL,
    component_id VARCHAR NOT NULL,
    PRIMARY KEY (issue_id, component_id),
    FOREIGN KEY (issue_id) REFERENCES issues(issue_id) ON DELETE CASCADE,
    FOREIGN KEY (component_id) REFERENCES components(component_id),
    INDEX idx_issue_components_issue (issue_id),
    INDEX idx_issue_components_component (component_id)
);

-- Issue fix versions junction table
CREATE TABLE IF NOT EXISTS issue_fix_versions (
    issue_id VARCHAR NOT NULL,
    version_id VARCHAR NOT NULL,
    PRIMARY KEY (issue_id, version_id),
    FOREIGN KEY (issue_id) REFERENCES issues(issue_id) ON DELETE CASCADE,
    FOREIGN KEY (version_id) REFERENCES versions(version_id),
    INDEX idx_issue_versions_issue (issue_id),
    INDEX idx_issue_versions_version (version_id)
);

-- Custom fields table for flexible field storage
CREATE TABLE IF NOT EXISTS custom_fields (
    custom_field_id VARCHAR PRIMARY KEY,
    field_name VARCHAR NOT NULL,
    field_type VARCHAR NOT NULL, -- text, number, date, option, array
    description VARCHAR,
    project_id VARCHAR,
    FOREIGN KEY (project_id) REFERENCES projects(project_id),
    INDEX idx_custom_field_name (field_name),
    INDEX idx_custom_field_project (project_id)
);

-- Issue custom field values table
CREATE TABLE IF NOT EXISTS issue_custom_field_values (
    issue_id VARCHAR NOT NULL,
    custom_field_id VARCHAR NOT NULL,
    field_value TEXT, -- JSON for complex values
    PRIMARY KEY (issue_id, custom_field_id),
    FOREIGN KEY (issue_id) REFERENCES issues(issue_id) ON DELETE CASCADE,
    FOREIGN KEY (custom_field_id) REFERENCES custom_fields(custom_field_id),
    INDEX idx_custom_values_issue (issue_id),
    INDEX idx_custom_values_field (custom_field_id)
);

-- Comments table
CREATE TABLE IF NOT EXISTS comments (
    comment_id VARCHAR PRIMARY KEY,
    issue_id VARCHAR NOT NULL,
    author_id VARCHAR NOT NULL,
    body TEXT NOT NULL,
    created_date TIMESTAMP NOT NULL,
    updated_date TIMESTAMP NOT NULL,
    visibility_type VARCHAR, -- group, role, or null for public
    visibility_value VARCHAR,
    FOREIGN KEY (issue_id) REFERENCES issues(issue_id) ON DELETE CASCADE,
    FOREIGN KEY (author_id) REFERENCES users(user_id),
    INDEX idx_comments_issue (issue_id),
    INDEX idx_comments_author (author_id),
    INDEX idx_comments_created (created_date)
);

-- Issue changelog for tracking changes
CREATE TABLE IF NOT EXISTS issue_changelog (
    changelog_id VARCHAR PRIMARY KEY,
    issue_id VARCHAR NOT NULL,
    author_id VARCHAR NOT NULL,
    created_date TIMESTAMP NOT NULL,
    FOREIGN KEY (issue_id) REFERENCES issues(issue_id) ON DELETE CASCADE,
    FOREIGN KEY (author_id) REFERENCES users(user_id),
    INDEX idx_changelog_issue (issue_id),
    INDEX idx_changelog_created (created_date)
);

-- Individual change items within a changelog entry
CREATE TABLE IF NOT EXISTS changelog_items (
    item_id VARCHAR PRIMARY KEY,
    changelog_id VARCHAR NOT NULL,
    field_name VARCHAR NOT NULL,
    field_type VARCHAR, -- jira, system, custom
    from_value TEXT,
    to_value TEXT,
    from_display_value TEXT,
    to_display_value TEXT,
    FOREIGN KEY (changelog_id) REFERENCES issue_changelog(changelog_id) ON DELETE CASCADE,
    INDEX idx_changelog_items_changelog (changelog_id),
    INDEX idx_changelog_items_field (field_name)
);

-- Attachments metadata
CREATE TABLE IF NOT EXISTS attachments (
    attachment_id VARCHAR PRIMARY KEY,
    issue_id VARCHAR NOT NULL,
    filename VARCHAR NOT NULL,
    author_id VARCHAR NOT NULL,
    created_date TIMESTAMP NOT NULL,
    size_bytes INTEGER,
    mime_type VARCHAR,
    content_url VARCHAR,
    thumbnail_url VARCHAR,
    FOREIGN KEY (issue_id) REFERENCES issues(issue_id) ON DELETE CASCADE,
    FOREIGN KEY (author_id) REFERENCES users(user_id),
    INDEX idx_attachments_issue (issue_id),
    INDEX idx_attachments_author (author_id)
);

-- Sync status tracking
CREATE TABLE IF NOT EXISTS sync_history (
    sync_id VARCHAR PRIMARY KEY,
    sync_type VARCHAR NOT NULL, -- full, incremental
    start_time TIMESTAMP NOT NULL,
    end_time TIMESTAMP,
    status VARCHAR NOT NULL, -- running, completed, failed
    records_processed INTEGER DEFAULT 0,
    records_updated INTEGER DEFAULT 0,
    records_inserted INTEGER DEFAULT 0,
    error_message TEXT,
    INDEX idx_sync_history_start_time (start_time),
    INDEX idx_sync_history_status (status)
);

-- Views for common analytical queries

-- Issue metrics view
CREATE VIEW IF NOT EXISTS issue_metrics AS
SELECT 
    i.issue_id,
    i.issue_key,
    i.project_id,
    p.project_key,
    p.project_name,
    it.issue_type_name,
    s.status_name,
    sc.category_name as status_category,
    pr.priority_name,
    assignee.display_name as assignee_name,
    reporter.display_name as reporter_name,
    i.summary,
    i.created_date,
    i.updated_date,
    i.resolved_date,
    -- Calculate age in days
    DATE_DIFF('day', i.created_date, COALESCE(i.resolved_date, CURRENT_TIMESTAMP)) as age_days,
    -- Calculate time to resolution
    CASE 
        WHEN i.resolved_date IS NOT NULL 
        THEN DATE_DIFF('day', i.created_date, i.resolved_date)
        ELSE NULL 
    END as resolution_days,
    i.labels,
    i.time_spent_seconds / 3600.0 as hours_spent,
    i.original_estimate_seconds / 3600.0 as hours_estimated
FROM issues i
JOIN projects p ON i.project_id = p.project_id
JOIN issue_types it ON i.issue_type_id = it.issue_type_id
JOIN statuses s ON i.status_id = s.status_id
JOIN status_categories sc ON s.status_category_id = sc.status_category_id
LEFT JOIN priorities pr ON i.priority_id = pr.priority_id
LEFT JOIN users assignee ON i.assignee_id = assignee.user_id
LEFT JOIN users reporter ON i.reporter_id = reporter.user_id;

-- Project summary view
CREATE VIEW IF NOT EXISTS project_summary AS
SELECT 
    p.project_id,
    p.project_key,
    p.project_name,
    COUNT(i.issue_id) as total_issues,
    COUNT(CASE WHEN sc.category_key = 'done' THEN 1 END) as resolved_issues,
    COUNT(CASE WHEN sc.category_key = 'new' THEN 1 END) as open_issues,
    COUNT(CASE WHEN sc.category_key = 'indeterminate' THEN 1 END) as in_progress_issues,
    AVG(CASE 
        WHEN i.resolved_date IS NOT NULL 
        THEN DATE_DIFF('day', i.created_date, i.resolved_date)
        ELSE NULL 
    END) as avg_resolution_days,
    MIN(i.created_date) as first_issue_date,
    MAX(i.updated_date) as last_updated_date
FROM projects p
LEFT JOIN issues i ON p.project_id = i.project_id
LEFT JOIN statuses s ON i.status_id = s.status_id
LEFT JOIN status_categories sc ON s.status_category_id = sc.status_category_id
GROUP BY p.project_id, p.project_key, p.project_name;