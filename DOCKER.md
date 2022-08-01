<p align="center">
    <br>
    <img src="img/Logo512pxWhite.png" alt="SabreDartStudios" width="120">
    <h2 align="center">Open World Server (OWS)</h2>
    <h4 align="center"><a href="http://www.sabredartstudios.com/">By SabreDartStudios</a></h4>
</p>

# Docker Setup Instructions

### Requirements
- Download Or Clone OWS
- [.Net 5.0 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)
- [SQL Server Management Studio](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms?view=sql-server-ver15) (Windows Only)
- [Azure Data Studio](https://docs.microsoft.com/en-us/sql/azure-data-studio/download-azure-data-studio?view=sql-server-ver15) (Windows/macOS/Linux)


# Windows
1. Download and Install [Docker Desktop For Windows](https://www.docker.com/products/docker-desktop)
2. Open OWS Project in [Visual Studio 2019 Community](https://visualstudio.microsoft.com/downloads/).  The ASP.NET and Web Development workload is required.
3. Select Docker Compose startup project and Launch Docker Compose.

    ![docker-compose-windows](https://i.imgur.com/HbRNXDG.png)

4. **(Optional)** Running Docker Compose without Visual Studio Debugger, Enter the following Command Prompt from the OWS root directory.
    ```shell
    docker-compose -f docker-compose.yml -f docker-compose.override.windows.yml up -d
    ```
    For a fresh build use the following command
    ```shell
    docker-compose -f docker-compose.yml -f docker-compose.override.windows.yml up -d --build --force-recreate
    ```
5. Complete the **Update your MSSQL Database** section below.
6. Complete the **Create Api Key** section below.

# macOS
1. Download and Install [Docker Desktop For Mac](https://www.docker.com/products/docker-desktop)
2. Open OWS Project in [Visual Studio For Mac](https://visualstudio.microsoft.com/vs/mac/).  The ASP.NET and Web Development workload is required.
3. Run the following command in an terminal to install the Development Certificates
    ```shell
    dotnet dev-certs https --trust
    ```
3. Select Docker Compose startup project and Launch Docker Compose.

    ![docker-compose-osx](https://i.imgur.com/QOGyGih.png)

4. **(Optional)** Running Docker Compose without Visual Studio Debugger, Run the following  command in an terminal from the OWS root directory.
    ```shell
    docker-compose -f docker-compose.yml -f docker-compose.override.osx.yml up -d
    ```
    For a fresh build use the following command
    ```shell
    docker-compose -f docker-compose.yml -f docker-compose.override.osx.yml up -d --build --force-recreate
    ```
5. Complete the **Update your MSSQL Database** section below.
6. Complete the **Create Api Key** section below.

# Linux
1. Download and Install [Docker Engine For Linux](https://www.docker.com/products/docker-desktop)
2. Close all Web Browsers
3. Download and Run [dotnet-dev-certificate-linux](https://github.com/CodewareGames/dotnet-dev-certificate-linux) to install Development HTTPS Certificate.
4. Run the following  command in an terminal from the OWS src directory.
    ```shell
    sudo docker-compose -f docker-compose.yml -f docker-compose.override.linux.yml up -d
    ```
    For a fresh build use the following command
    ```shell
    sudo docker-compose -f docker-compose.yml -f docker-compose.override.linux.yml up -d --build --force-recreate
    ```
5. Complete the **Update your MSSQL Database** section below.
6. Complete the **Create Api Key** section below.

# Update your MSSQL Database
1. Open SQL Server Management Studio or Azure Data Studio and connect to localhost with sa / yourStrong(!)Password
2. Run the following SQL statement against the Open World Server database.

    ```sql
    SELECT * FROM OWSVersion
    ```
3. Compare the OWSDBVersion returned with the SQL update scripts in the Databases\MSSQL\UpdateScriptsFolder to see which scripts to run on your database.
4. The update scripts have From[SomeVersion]To[SomeVersion] in the name.  Run them in order starting with the From[SomeVersion] that matches your OWSDBVersion from the previous step.

# Create Api Key
1. Open SQL Server Management Studio or Azure Data Studio and connect to localhost with sa / yourStrong(!)Password
2. Run the following SQL statment against the Open World Server database.  Be sure to replace the values CustomerName, FirstName, LastName, Email, and Password.
   (Optional): A specific GUID can be entered as an optional final parameter, after password, in the formaty of '00000000-0000-0000-0000-000000000000'.

    ```sql
    EXEC [dbo].[AddNewCustomer] 'CustomerName', 'FirstName', 'LastName', 'Email', 'Password'
    ```
3. Run the following SQL statment against the Open World Server database to get your API key.  Save this for later.

    ```sql
    SELECT TOP 1 CustomerGUID FROM Customers
    ```
4. Follow the instructions here to setup the OWS Starter Project and connect it to your OWS API.  [Instructions for OWS Starter Project](http://rpgwebapi.sabredartstudios.com/Docs/Install)
