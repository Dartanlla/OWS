---
layout: default
title: Visual Studio Code
parent: Getting Started
nav_order: 2
---

# Docker Setup

## Requirements  
* Download or clone [OWS](https://github.com/Dartanlla/OWS)
* Download and install [Visual Studio Code](https://code.visualstudio.com/)
  * [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension is required
  * [Docker for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-docker) extension is required
* Download and install [Docker Desktop](https://www.docker.com/products/docker-desktop) for Windows/macOS/Linux
* [.Net 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
* Enable "Virtualization Technology" in your BIOS
<br />
<br />

<details open markdown="block">
  <summary class="fs-6 mb-3">
    Windows
  </summary>

1. Make sure Windows Subsystem for Linux is installed. ![Launch Docker Compose](images/windows-subsystem-for-linux01.png){: .mt-3}  
2. Download and install [Docker Desktop for Windows](https://www.docker.com/products/docker-desktop)
3. Open the src folder inside the OWS Project folder and open it with Visual Studio Code.
4. Select View -> Command Palette, Type Docker Compose Up select docker-compose.xml as the compose file.
5. Select Windows Template File ![Launch Docker Compose](images/docker-compose-vscode.png){: .mt-3}
6. <span class="label" style="margin-left: -3px">Optional</span>  
   Running Docker Compose without Visual Studio Code, Enter the following Command Prompt from the OWS root directory.

   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.override.windows.yml -f docker-compose-additional.yml up -d 
   ```

   For a fresh build use the following command
   
   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.override.windows.yml -f docker-compose-additional.yml up -d --build --force-recreate 
   ```
</details>

<details markdown="block">
  <summary class="fs-6 mb-3">
    macOS
  </summary>

1. Download and install [Docker Desktop for Mac](https://docs.docker.com/desktop/install/mac-install/)
2. Run the following command in a terminal to install the Development Certificates
   
   ```bash
   dotnet dev-certs https --trust
   ```

3. Open the src folder inside the OWS Project folder and open it with Visual Studio Code.
4. Select View -> Command Palette, Type Docker Compose Up select docker-compose.xml as the compose file.
5. Select macOS Template File ![Launch Docker Compose](images/docker-compose-vscode.png){: .mt-3}
5. <span class="label" style="margin-left: -3px">Optional</span>  
    Running Docker Compose without Visual Studio Code, Run the following command in an terminal from the OWS root directory.

   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.override.osx.yml -f docker-compose-additional.yml up -d 
   ```

   For a fresh build use the following command
   
   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.override.osx.yml -f docker-compose-additional.yml up -d --build --force-recreate 
   ```
</details>

<details markdown="block">
  <summary class="fs-6 mb-3">
    Linux
  </summary>

1. Download and install [Docker Desktop for Linux](https://docs.docker.com/desktop/install/linux-install/)
2. Close all web browsers
3. Download and Run [dotnet-dev-certificate-linux](https://github.com/CodewareGames/dotnet-dev-certificate-linux) to install Development HTTPS Certificate.
4. Open the src folder inside the OWS Project folder and open it with Visual Studio Code.
5. Select View -> Command Palette, Type Docker Compose Up select docker-compose.xml as the compose file.
6. Select Linux Template File ![Launch Docker Compose](images/docker-compose-vscode.png){: .mt-3}
7. Run the following command in an terminal from the OWS src directory.

   ```bash
   sudo docker-compose -f docker-compose.yml -f docker-compose.override.linux.yml -f docker-compose-additional.yml up -d 
   ```

   For a fresh build use the following command

   ```bash
   sudo docker-compose -f docker-compose.yml -f docker-compose.override.linux.yml -f docker-compose-additional.yml up -d --build --force-recreate 
   ```
</details>

[Next: Database setup](setup-database){: .btn .btn-outline }
