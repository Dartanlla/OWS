---
layout: default
title: MySQL
parent: Database Setup
grand_parent: Getting Started
nav_order: 1
---

# MySQL

[MySQL](https://www.mysql.com/de/) is the world's most popular open source database system.

## Using MySQL/MariaDB as an alternative to MSSQL
1. Open `src/.env.`
2. Replace the `OWSDBConnectionString` with `server=host.docker.internal;user=root;database=openworldserver;port=3306;password=yourStrong(!)Password;Allow User Variables=True;SslMode=None`
3. Replace the value in **OWSDBBackend** with **mysql**

## Update database version

1. Open MySQL Workbench and connect to localhost using the following credentials:

    <dl>
        <dt>Username</dt>
        <dd>root</dd>
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

1. Open MySQL Workbench and connect to localhost using the following credentials:

    <dl>
        <dt>Username</dt>
        <dd>root</dd>
        <dt>Password</dt>
        <dd>yourStrong(!)Password</dd>
    </dl>

2. Run the following SQL statment against the Open World Server database.

   (Optional): A specific GUID can be entered as an optional final parameter, after password, in the format of '00000000-0000-0000-0000-000000000000'.

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

## Additional
If you are using an external MySQL/MariaDB, you need to ensure that the `ENCRYPT` function works as is available. Consult your database administrator if unsure.

Secondly you need will need to run the following as a SUPER user, or grant your SUPER access to your user.

```sql
SET GLOBAL log_bin_trust_function_creators = 1;
```

[Next: OWS Starter Project](starter-project){: .btn .btn-outline }