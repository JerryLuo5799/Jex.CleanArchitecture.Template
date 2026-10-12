-- Creates the application database on first start (SQL Server has no POSTGRES_DB equivalent).
IF DB_ID(N'TPLNAMELOWER') IS NULL
BEGIN
    CREATE DATABASE [TPLNAMELOWER];
END;
GO
