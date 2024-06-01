---
layout: default
title: MSSQL
parent: Database Setup
grand_parent: Getting Started
nav_order: 1
---

# MSSQL

[MSSQL](https://www.microsoft.com/de-de/sql-server/sql-server-2019) is a suite of database software published by Microsoft. It includes a relational database engine, which stores data in tables, columns and rows.

## Using MSSQL as an alternative to PostgreSQL
Open src/.env.
Rem out this line using # like this:
```
# DATABASE_CONNECTION_STRING="Host=host.docker.internal;Port=5432;Database=openworldserver;Username=postgres;Password=${DATABASE_PASSWORD};"
```
Unrem out this line by removing the # in front of the line like this:
```
DATABASE_CONNECTION_STRING="Server=host.docker.internal;Database=OpenWorldServer;User Id=SA;Password=${DATABASE_PASSWORD};ConnectRetryCount=0"
```
Replace the value in DATABASE with mssql like this:
```
DATABASE='mssql'
```

## Update database version

1. Open SQL Server Management Studio or Azure Data Studio and connect to localhost with the following credentials.

    <dl>
        <dt>Username</dt>
        <dd>sa</dd>
        <dt>Password</dt>
        <dd>yourStrong(!)Password</dd>
    </dl>

2. Run the following SQL statement against the Open World Server database.
   
   ```sql
   SELECT * FROM OWSVersion
   ```

3. Compare the **OWSDBVersion** returned with the SQL update scripts in the `Databases\MSSQL\UpdateScriptsFolder` to see which scripts to run on your database.
   
4. The update scripts have **From[SomeVersion]To[SomeVersion]** in the name. Run them in order starting with the **From[SomeVersion]** that matches your **OWSDBVersion** from the previous step. To run the script open the file, copy the contents from it and run it as a SQL statement against the Open World Server database like you did in step 2. If you are not familiar with SQL please refer to the [SQL Tutorial](https://www.w3schools.com/sql/) to learn more about it.

## Create API Key

1. Open SQL Server Management Studio or Azure Data Studio and connect to localhost with the following credentials.

    <dl>
        <dt>Username</dt>
        <dd>sa</dd>
        <dt>Password</dt>
        <dd>yourStrong(!)Password</dd>
    </dl>

2. Run the following SQL statment against the Open World Server database.

   (Optional): A specific GUID can be entered as an optional final parameter, after password, in the format of '00000000-0000-0000-0000-000000000000'.

   ```sql
   EXEC [dbo].[AddNewCustomer] 'CustomerName', 'FirstName', 'LastName', 'Email', 'Password'
   ```
   
   Be sure to replace the values:

    <dl>
        <dt>CustomerName</dt>
        <dd>The nickname or profile name of the customer.</dd>
        <dt>FirstName</dt>
        <dd>The firstname of the customer.</dd>
        <dt>LastName</dt>
        <dd>The lastname of the customer.</dd>
        <dt>Email</dt>
        <dd>Enter the mail address of the customer.</dd>
        <dt>Password</dt>
        <dd>Enter a secure password.</dd>
    </dl>
    
3. Run the following SQL statment against the Open World Server database to get your **API key**. Save the key for later.
   
   ```sql
   SELECT TOP 1 CustomerGUID FROM Customers
   ```

[Next: OWS Starter Project](starter-project){: .btn .btn-outline }
