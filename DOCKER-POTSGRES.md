<p align="center">
    <br>
    <img src="img/Logo512pxWhite.png" alt="SabreDartStudios" width="120">
    <h2 align="center">Open World Server (OWS)</h2>
    <h4 align="center"><a href="http://www.sabredartstudios.com/">By SabreDartStudios</a></h4>
</p>

# Using Postgres as an alternative to MSSQL

1. Open docker-compose.yml and replace
```
  mssql:
    build:
      context: .
      dockerfile: .docker/mssql/Dockerfile
    environment:
     - ACCEPT_EULA=Y
     # Developer, Express, Standard, Enterprise, EnterpriseCore
     - MSSQL_PID=Developer
     - SA_PASSWORD=yourStrong(!)Password
    ports:
     - "1433:1433"
    volumes:
     - mssql_data:/var/opt/mssql/data
     - mssql_log:/var/opt/mssql/log
     - mssql_secrets:/var/opt/mssql/secrets
     - type: bind
       source: .docker/mssql/backups
       target: /var/opt/mssql/backups
```
with
```
  postgres:
    build:
      context: .
      dockerfile: .docker/postgres/Dockerfile
    environment:
     - POSTGRES_PASSWORD=yourStrong(!)Password
    ports:
     - "5432:5432"
    volumes:
     - postgres_data:/var/lib/postgresql/data
```
2. Replace
```
volumes:
  mssql_data:
  mssql_log:
  mssql_secrets:
```

with
```
volumes:
  postgres_data:
```
3. In `src/OWSCharacterPersistence/Startup.cs` under `InitializeContainer` replace
```
            container.Register<ICharactersRepository, OWSData.Repositories.Implementations.MSSQL.CharactersRepository>(Lifestyle.Scoped);
            container.Register<IUsersRepository, OWSData.Repositories.Implementations.MSSQL.UsersRepository>(Lifestyle.Scoped);
```
with
```
            container.Register<ICharactersRepository, OWSData.Repositories.Implementations.Postgres.CharactersRepository>(Lifestyle.Scoped);
            container.Register<IUsersRepository, OWSData.Repositories.Implementations.Postgres.UsersRepository>(Lifestyle.Scoped);
```
4. In `src/OWSInstanceManagement/Startup.cs` under `InitializeContainer` replace
```
            container.Register<IInstanceManagementRepository, OWSData.Repositories.Implementations.MSSQL.InstanceManagementRepository>(Lifestyle.Scoped);
            container.Register<ICharactersRepository, OWSData.Repositories.Implementations.MSSQL.CharactersRepository>(Lifestyle.Scoped);
            container.Register<IUsersRepository, OWSData.Repositories.Implementations.MSSQL.UsersRepository>(Lifestyle.Scoped);
```

with
```
            container.Register<IInstanceManagementRepository, OWSData.Repositories.Implementations.Postgres.InstanceManagementRepository>(Lifestyle.Scoped);
            container.Register<ICharactersRepository, OWSData.Repositories.Implementations.Postgres.CharactersRepository>(Lifestyle.Scoped);
            container.Register<IUsersRepository, OWSData.Repositories.Implementations.Postgres.UsersRepository>(Lifestyle.Scoped);
```
5. In `src/OWSPublicAPI/Startup.cs` under `InitializeContainer` replace
```
            container.Register<ICharactersRepository, OWSData.Repositories.Implementations.MSSQL.CharactersRepository>(Lifestyle.Transient);
            container.Register<IUsersRepository, OWSData.Repositories.Implementations.MSSQL.UsersRepository>(Lifestyle.Transient);
```

with
```
            container.Register<ICharactersRepository, OWSData.Repositories.Implementations.Postgres.CharactersRepository>(Lifestyle.Transient);
            container.Register<IUsersRepository, OWSData.Repositories.Implementations.Postgres.UsersRepository>(Lifestyle.Transient);
```
6. In `src/OWSInstanceLauncher/appsettings.json`, `src/OWSInstanceManagement/appsettings.json`, and `src/OWSPublicAPI/appsettings.json` replace the `OWSDBConnectionString` with `Host=host.docker.internal;Port=5432;Database=OpenWorldServer;Username=postgres;Password=yourStrong(!)Password;`.
7. Follow the steps in [Docker Setup Instructions](DOCKER.md) until "Create Api Key" (See below for Postgres version)


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
