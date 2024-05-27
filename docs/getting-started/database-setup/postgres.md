---
layout: default
title: Postgres
parent: Database Setup
grand_parent: Getting Started
nav_order: 1
---

# Postgres

[PostgreSQL](https://www.postgresql.org/) is a powerful, open source object-relational database system with over 30 years of active development that has earned it a strong reputation for reliability, feature robustness, and performance.

## PostgreSQL is the now the default database technology for OWS
No configuration changes are required to use PostgreSQL

## Update database version
All database updates are applied by default now.  If you need to update an existing database, the update scripts are in the ./Databases/Postgres/UpdateScripts folder.

## Warning!  If you already have PostgreSQL installed on your development PC running on port 5432, then OWS 2 will not work  
Please adjust the OWS 2 configuration to run PostgreSQL on an alternate port.  Here is an example of what to change to switch OWS copy of PostgreSQL to port 15432:
1. Open the .env file and go to the Postgres connection string and edit it like this:
```
# Postgres
DATABASE_CONNECTION_STRING="Host=host.docker.internal;Port=15432;Database=openworldserver;Username=postgres;Password=${DATABASE_PASSWORD};"
```
2. Open databases.yml and edit it like this:
```
### PostgreSQL Database
  postgres:
    build:
      context: postgres/
    environment:
      - POSTGRES_PASSWORD=${DATABASE_PASSWORD}
    ports:
      - "15432:5432"
    volumes:
      - database:/var/lib/postgresql/data
    container_name: PostgreSQL
```
Notice that we ONLY change the first number to 15432 and not the second number.  The second number has to stay 5432.

## Create API Key

1. Open PGAdmin3/4 or dBeaver and connect  to localhost using the following credentials:

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
   The above query only works if this is a new OWS 2 installation.  A common issue during setup is when you have more than one API key (CustomerGUID).


[Next: OWS Starter Project](starter-project){: .btn .btn-outline }
