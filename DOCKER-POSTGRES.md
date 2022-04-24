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
6. Replace the `OWSDBConnectionString` with `Host=host.docker.internal;Port=5432;Database=OpenWorldServer;Username=postgres;Password=yourStrong(!)Password;`.
7. Replace the value in `OWSDBBackend` with `postgres`.
8. Follow the steps in [Docker Setup Instructions](DOCKER.md) until "Create Api Key" (See below for Postgres version)


# Create Api Key
1. Open PGAdmin3/4 and connect to localhost with postgres / yourStrong(!)Password
2. Run the following SQL statment against the Open World Server database.  Be sure to replace the values CustomerName, FirstName, LastName, Email, and Password.

    ```sql
    CALL AddNewCustomer 'CustomerName', 'FirstName', 'LastName', 'Email', 'Password';
    ```
3. Run the following SQL statment against the Open World Server database to get your API key.  Save this for later.

    ```sql
    SELECT CustomerGUID FROM Customers LIMIT 1;
    ```
4. Run the following SQL statment against the Open World Server database to add your local PC as a World Server.  Replace [CustomerGUID] with the API key from the previous step.

    ```sql
    INSERT INTO WorldServers (CustomerGUID, ServerIP, MaxNumberOfInstances, ActiveStartTime, Port, ServerStatus, InternalServerIP, StartingMapInstancePort)
    VALUES ('[CustomerGUID]', '127.0.0.1', 10, NULL, '8081', 0, '127.0.0.1', '7778');
    ```
5. Follow the instructions here to setup the OWS Starter Project and connect it to your OWS API.  [Instructions for OWS Starter Project](http://rpgwebapi.sabredartstudios.com/Docs/Install)
