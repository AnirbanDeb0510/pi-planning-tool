-- db/init.sql
-- ===========================================
-- Initial setup script for PI Planning Tool DB
-- ===========================================


-- Create a placeholder table to test connectivity
CREATE TABLE IF NOT EXISTS VersionInfo (
    Id SERIAL PRIMARY KEY,               -- auto-increment in Postgres
    Version VARCHAR(50) NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Insert initial record
INSERT INTO VersionInfo (Version)
VALUES ('Init SQL Executed');
