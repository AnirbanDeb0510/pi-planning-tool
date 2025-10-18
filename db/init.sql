-- db/init.sql
-- ===========================================
-- Initial setup script for PI Planning Tool DB
-- ===========================================

-- Wait for SQL Server to start up
WAITFOR DELAY '00:00:10';

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'PIPlanningDB')
BEGIN
    CREATE DATABASE PIPlanningDB;
END
GO

USE PIPlanningDB;
GO

-- Optional: create schema (for clarity)
CREATE SCHEMA pi;
GO

-- (Optional) create a placeholder table so you can test connectivity
CREATE TABLE pi.VersionInfo (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Version VARCHAR(50) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

INSERT INTO pi.VersionInfo (Version)
VALUES ('Init SQL Executed');
GO
