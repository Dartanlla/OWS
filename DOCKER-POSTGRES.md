<p align="center">
    <br>
    <img src="img/Logo512pxWhite.png" alt="SabreDartStudios" width="120">
    <h2 align="center">Open World Server (OWS)</h2>
    <h4 align="center"><a href="http://www.sabredartstudios.com/">By SabreDartStudios</a></h4>
</p>

# Using Postgres as an alternative to MSSQL

1. Open `docker-compose.yml`.
2. Comment out the `Microsoft SQL Database` service and uncomment the `PostgreSQL Database` service.
3. In all `depends_on` sections, comment out `mssql` and uncomment `postgres`
4. Comment out all 3 volumes for `mssql_*` and uncomment `postgres_data`.
5. Open `src/OWSInstanceLauncher/appsettings.json`, `src/OWSInstanceManagement/appsettings.json`, and `src/OWSPublicAPI/appsettings.json`.
6. Replace the `OWSDBConnectionString` with `Host=host.docker.internal;Port=5432;Database=openworldserver;Username=postgres;Password=yourStrong(!)Password;`.
7. Replace the value in `OWSDBBackend` with `postgres`.
8. Follow the steps in [Docker Setup Instructions](DOCKER.md) replacing steps **Update your MSSQL Database** and **Create Api Key** with the below alternatives.

# Update your Postgres Database
1. Open PGAdmin3/4 and connect to localhost with postgres / yourStrong(!)Password
2. Run the following SQL statement against the Open World Server database.

    ```sql
    SELECT * FROM OWSVersion
    ```
3. Compare the OWSDBVersion returned with the SQL update scripts in the Databases\Postgres\UpdateScriptsFolder to see which scripts to run on your database.
4. The update scripts have From[SomeVersion]To[SomeVersion] in the name.  Run them in order starting with the From[SomeVersion] that matches your OWSDBVersion from the previous step.

# Create Api Key
1. Open PGAdmin3/4 and connect to localhost with postgres / yourStrong(!)Password
2. Run the following SQL statment against the Open World Server database.  Be sure to replace the values CustomerName, FirstName, LastName, Email, and Password.

    ```sql
    CALL AddNewCustomer ('CustomerName', 'FirstName', 'LastName', 'Email', 'Password');
    ```
3. Run the following SQL statment against the Open World Server database to get your API key.  Save this for later.

    ```sql
    SELECT CustomerGUID FROM Customers LIMIT 1;
    ```
4. Follow the instructions here to setup the OWS Starter Project and connect it to your OWS API.  [Instructions for OWS Starter Project](http://rpgwebapi.sabredartstudios.com/Docs/Install)
