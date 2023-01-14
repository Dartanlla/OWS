---
layout: default
title: Postgres
parent: Database Setup
grand_parent: Getting Started
nav_order: 1
---

# Postgres

[PostgreSQL](https://www.postgresql.org/) is a powerful, open source object-relational database system with over 30 years of active development that has earned it a strong reputation for reliability, feature robustness, and performance.

## Using Postgres as an alternative to MSSQL
1. Open `src/.env.`
2. Replace the `OWSDBConnectionString` with `Host=host.docker.internal;Port=5432;Database=openworldserver;Username=postgres;Password=yourStrong(!)Password;`
3. Replace the value in **OWSDBBackend** with **postgres**

## Update database version

1. Open PGAdmin3/4 and connect to localhost using the following credentials:

    <dl>
        <dt>Username</dt>
        <dd>postgres</dd>
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

1. Open PGAdmin3/4  and connect  to localhost using the following credentials:

    <dl>
        <dt>Username</dt>
        <dd>postgres</dd>
        <dt>Password</dt>
        <dd>yourStrong(!)Password</dd>
    </dl>

2. Run the following SQL statment against the Open World Server database.

   (Optional): Replace NULL with a single-quoted UUID. For example '00000000-0000-0000-0000-000000000000'::uuid

   ```sql
    CALL AddNewCustomer ('CustomerName', 'FirstName', 'LastName', 'Email', 'Password', NULL);
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
   SELECT CustomerGUID FROM Customers LIMIT 1;
   ```

[Next: OWS Starter Project](starter-project){: .btn .btn-outline }