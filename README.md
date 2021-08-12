<p align="center">
    <br>
    <img src="img/Logo512pxWhite.png" alt="SabreDartStudios" width="120">
    <h2 align="center">Open World Server (OWS)</h2>
    <h4 align="center"><a href="http://www.sabredartstudios.com/" target="_blank">By SabreDartStudios</a></h4>
</p>
<h1></h1>
<p align="center">
    <a href="https://github.com/Dartanlla/OWS/blob/master/LICENSE">
        <img src="https://img.shields.io/github/license/Dartanlla/ows.svg?style=flat-square">
    </a>
    <a href="https://discord.gg/RxMkuJF">
        <img src="https://img.shields.io/badge/Discord-%237289DA.svg?style=flat-square&logo=discord&logoColor=white" alt="Join Discord">
    </a>
    <img src="https://img.shields.io/badge/unrealengine-%23313131.svg?style=flat-square&logo=unrealengine&logoColor=white">
    <img src="https://img.shields.io/badge/docker-%230db7ed.svg?style=flat-square&logo=docker&logoColor=white">
    <img src="https://img.shields.io/badge/.NET-5C2D91?style=flat-square&logo=.net&logoColor=white">
</p>

Open World Server (OWS) is a server instance manager designed to create large worlds in UE4. Either by stitching together multiple UE4 maps or by splitting a single large map into multiple zones, OWS will spin up and shut down server instances as needed based on your world population. OWS can load balance your world across multiple hardware devices. OWS can support thousands of players in the same world by instancing up and out. A single zone can be instanced multiple times to support a large population in one area. Areas of a map can also be split up into multiple zones to support a larger population. In addition to server instance management, OWS also handles persistence for Accounts, Characters, Abilities, Inventory and more.

# Projects
- OWS Benchmarks - This project will allow us to configure and run performance testing on the OWS API.  This will be important for comparing the impact of certain changes.
- OWS Character Persistence - The Character Persistence API will be responsible for storing our player characters and all related data.
- OWS Data - This is a shared project that houses our data repository access code.
- OWS External Login Providers - This project contains code for integrating with external login providers such as Xsolla, Google, Facebook, etc.
- OWS Instance Launcher - This project builds our Instance Launcher that replaces the RPG World Server in OWS 1.
- OWS Instance Management - This API manages Zone Instances and the OWS Instance Launchers.
- OWS Public API - This API handles all API calls that come directly from player clients such as registration, login, and connecting to the game.
- OWS Shared - This project houses various miscellaneous code that multiple other projects require.

# Additional Files Needed
- OWS 1 Web API: https://drive.google.com/file/d/1usSUaohEKJv1yPJKV2CVFuhyeocDt6Is/view?usp=sharing  (this will be needed until we replace all of the OWS 1 functionality)
- OWS 1 MSSQL 2017 DB: https://drive.google.com/file/d/1pEXNK1fK5e4fLzSFARzdKvY7ESMkY9Ru/view?usp=sharing (this will be needed until we break out the DB into separate microservice respositories)

# Contributing
* [Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)
* [Naming Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines)
* [Coding Style](https://github.com/dotnet/corefx/blob/368fdfd86ee3a3bf1bca2a6c339ee590f3d6505d/Documentation/coding-guidelines/coding-style.md)

# Windows Server Setup Instructions
- Install Erlang (https://www.erlang.org/downloads) - must be a version your RabbitMQ version supports
- Install RabbitMQ (https://www.rabbitmq.com/install-windows.html)
- Enable the RabbitMQ Admin Web Console - rabbitmq-plugins enable rabbitmq_management
- Open the RabbitMQ Admin Web Console - http://localhost:15672/ - Login as guest / guest
- Create a new RabbitMQ User and give the user access to /
- Put those credentials in appsettings.json for the OWSInstanceLauncher
- Install IIS (from your Windows media)
- Install the .NET Core 3.1 Web Hosting Bundle - https://dotnet.microsoft.com/download/dotnet/3.1 - Windows Hosting Bundle link
- Create the folder in C:\inetpub\sites\OWSPublicAPI
- Create the folder in C:\inetpub\sites\OWSInstanceManagement

# Docker Setup Instructions (Beta)
### Requirements
- Download Or Clone OWS
- [.Net Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet/3.1)
- [.Net 5.0 SDK](https://dotnet.microsoft.com/download/dotnet/5.0) - Required For Mac & Linux

## Windows
1. Download and Install [Docker Desktop For Windows](https://www.docker.com/products/docker-desktop)
2. Open OWS Project in [Visual Studio 2019 Community](https://visualstudio.microsoft.com/downloads/)
3. Select Docker Compose startup project and Launch Docker Compose.

    ![docker-compose-windows](data:image/png;base64,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)

4. **(Optional)** Running Docker Compose without Visual Studio Debugger, Enter the following Command Prompt from the OWS root directory.
    ```
    docker-compose -f docker-compose.yml -f docker-compose.override.windows.yml up -d
    ```
## OSX
1. Download and Install [Docker Desktop For Mac](https://www.docker.com/products/docker-desktop)
2. Open OWS Project in [Visual Studio For Mac](https://visualstudio.microsoft.com/vs/mac/)
3. Run the following command in an terminal to install the Development Certificates
    ```
    dotnet dev-certs https --trust
    ```
3. Select Docker Compose startup project and Launch Docker Compose.

    ![docker-compose-windows](data:image/png;base64,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)

4. **(Optional)** Running Docker Compose without Visual Studio Debugger, Run the following  command in an terminal from the OWS root directory.
    ```
    docker-compose -f docker-compose.yml -f docker-compose.override.osx.yml up -d
    ```
## Linux