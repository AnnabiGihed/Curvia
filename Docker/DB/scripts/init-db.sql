-- ============================================
-- SQL Server Database Initialization Script
-- ============================================
-- This script is idempotent - safe to run multiple times
-- ============================================

USE [master];
GO

-- Create Keycloak database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'Keycloak')
BEGIN
    PRINT 'Creating Keycloak database...';
    CREATE DATABASE [Keycloak];
    PRINT 'Keycloak database created successfully.';
END
ELSE
BEGIN
    PRINT 'Keycloak database already exists.';
END
GO

-- Optional: Set database options
ALTER DATABASE [Keycloak] SET RECOVERY SIMPLE;
GO

-- Verify database was created
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'Keycloak')
BEGIN
    PRINT 'SUCCESS: Keycloak database is ready.';
END
ELSE
BEGIN
    RAISERROR('ERROR: Keycloak database was not created!', 16, 1);
END
GO