---
layout: default
title: Docker Setup
parent: Getting Started
nav_order: 1
---

# Docker Setup

## Requirements  
* Download or clone OWS
* Download and install [Visual Studio 2019 Community](https://visualstudio.microsoft.com/downloads/)
  * The ASP.NET and Web Development workload is required
* Download and install [Docker Desktop](https://www.docker.com/products/docker-desktop) for Windows/macOS/Linux
* [.Net 5.0 SDK]([another-page](https://dotnet.microsoft.com/download/dotnet/5.0))
<br />
<br />

<details open markdown="block">
  <summary class="fs-6 mb-3">
    Windows
  </summary>

1. Download and install [Docker Desktop for Windows](https://www.docker.com/products/docker-desktop)
2. Open OWS Project in Visual Studio 2019 Community. The [ASP.NET and Web Development workload](../troubleshooting/visual-studio#installing-workloads) is required.
3. Select Docker Compose startup project and Launch Docker Compose. ![Launch Docker Compose](images/docker-compose-windows.png){: .mt-3}
4. <span class="label" style="margin-left: -3px">Optional</span>  
   Running Docker Compose without Visual Studio Debugger, Enter the following Command Prompt from the OWS root directory.

   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.override.windows.yml up -d
   ```

   For a fresh build use the following command
   
   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.override.windows.yml up -d --build --force-recreate
   ```
</details>

<details markdown="block">
  <summary class="fs-6 mb-3">
    macOS
  </summary>

1. Download and install [Docker Desktop for Windows](https://www.docker.com/products/docker-desktop)
2. Open OWS Project in [Visual Studio For Mac](https://visualstudio.microsoft.com/de/vs/mac/). The [ASP.NET and Web Development workload](../troubleshooting/visual-studio#installing-workloads) is required.
3. Run the following command in a terminal to install the Development Certificates
   
   ```bash
   dotnet dev-certs https --trust
   ```

4. Select Docker Compose startup project and Launch Docker Compose. ![Launch Docker Compose](images/docker-compose-mac.png){: .mt-3}
5. <span class="label" style="margin-left: -3px">Optional</span>  
    Running Docker Compose without Visual Studio Debugger, Run the following command in an terminal from the OWS root directory.

   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.override.osx.yml up -d
   ```

   For a fresh build use the following command
   
   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.override.osx.yml up -d --build --force-recreate
   ```
</details>

<details markdown="block">
  <summary class="fs-6 mb-3">
    Linux
  </summary>

1. Download and install [Docker Desktop for Windows](https://www.docker.com/products/docker-desktop)
2. Close all web browsers
3. Download and Run [dotnet-dev-certificate-linux](https://github.com/CodewareGames/dotnet-dev-certificate-linux) to install Development HTTPS Certificate.
4. Run the following command in an terminal from the OWS src directory.

   ```bash
   sudo docker-compose -f docker-compose.yml -f docker-compose.override.linux.yml up -d
   ```

   For a fresh build use the following command

   ```bash
   sudo docker-compose -f docker-compose.yml -f docker-compose.override.linux.yml up -d --build --force-recreate
   ```
</details>

[Next: Database setup](setup-database){: .btn .btn-outline }