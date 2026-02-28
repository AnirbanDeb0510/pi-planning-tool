-- db/init-sqlserver.sql
-- ===========================================
-- Initial setup script for PI Planning Tool DB (SQL Server)
-- ===========================================
-- Run this script in SQL Server Management Studio (SSMS) or via sqlcmd
-- before deploying the application to IIS.
--
-- This creates the database that the application will use.
-- EF Core migrations will automatically create the tables on first run.
-- ===========================================

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'PIPlanningDB')
BEGIN
    CREATE DATABASE PIPlanningDB;
    PRINT 'Database PIPlanningDB created successfully.';
END
ELSE
BEGIN
    PRINT 'Database PIPlanningDB already exists.';
END
GO

-- Switch to the new database
USE PIPlanningDB;
GO

-- Create a placeholder table to test connectivity
-- (This table will be removed/replaced by EF Core migrations)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[VersionInfo]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[VersionInfo] (
        Id INT IDENTITY(1,1) PRIMARY KEY,     -- auto-increment in SQL Server
        Version NVARCHAR(50) NOT NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE()
    );
    
    -- Insert initial record
    INSERT INTO VersionInfo (Version)
    VALUES ('Init SQL Executed');
    
    PRINT 'VersionInfo table created and initialized.';
END
ELSE
BEGIN
    PRINT 'VersionInfo table already exists.';
END
GO

PRINT 'SQL Server database initialization complete.';
PRINT 'Connection string should use: Server=localhost;Database=PIPlanningDB;...';
GO
