---
layout: default
title: Database Setup
parent: Getting Started
nav_order: 3
has_children: true
---

# Choose a Database Technology

OWS uses repository interfaces and can be used with multiple Database technologies.  At this time there are implementations for PostgreSQL, MSSQL, and MySQL.  **PostgeSQL is the default.**  Support my vary by database technology.

## Requirements  
* If you are using PostgreSQL (the default), download one of the following SQL Management tools:
  * [pgAdmin](https://www.pgadmin.org/)
  * [dBeaver](https://dbeaver.io/download/)
* If you are using MSSQL, download and install one of the following SQL Management tools:
  * [SQL Server Management Studio](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms?view=sql-server-ver15) (Windows Only)  
  * [Azure Data Studio](https://docs.microsoft.com/en-us/sql/azure-data-studio/download-azure-data-studio?view=sql-server-ver15) (Windows/macOS/Linux)

> **Warning**
> Only PostgeSQL and MSSQL are officially supported.  All other database technologies are community supported and may or may not work at any given time.  Community supported database technologies may also be removed at any time without notice if the community stops supporting them.
