---
layout: default
title: Unreal Engine 5.4.0
parent: OWS Starter Project
grand_parent: Getting Started
nav_order: 1
---

# OWS Starter Project for Unreal Engine 5.4.0
Follow these instructions if you want to setup the OWS Starter Project for Unreal Engine 5. Before you are able to setup this project you need to finish the [Docker Setup](docker-setup) and [Database setup](setup-database) sections.

## Download the project

Click on the button below to download the Open World Starter project.

[Download OpenWorldStarterDockerUE5.zip - 20240428 Release - UE 5.4.0](https://drive.google.com/file/d/1X4YXwesqLHvB00xq8TqYQht3V2OuNrET/view?usp=sharing){: .btn .btn-blue .mr-4}

## Initial setup

1. Open the [OWS GitHub project](https://github.com/Dartanlla/OWS) in Visual Studio 2022 and click on the Docker Compose button (if it is not already running). Check the [Docker Setup](docker-setup) section for a detailed explanation.
   
2. Unzip OpenWorldStarterPlugin.zip to your Unreal Projects folder or another location on your PC.
   
3. Open `/OWSInstanceLauncher/appsettings.json` file in notepad.
   
4. Verify that the **PathToDedicatedServer** value points to the latest version of the **UnrealEditor.exe**.
   
5. Modify the **PathToUProject** value to include the path to the **OpenWorldStarter.uproject**. The path must also include the uproject file and file extension.

6. Enter the **OWSAPIKey** value that you created when following the [Database setup instructions](setup-database).

7. Save the `appsettings.json` file

8. If you want to host multiplayer games that others can join, open your router configuration and forward UDP for ports 7777 to 7787 to your PC (will support up to 10 maps).

9. If you want to host multiplayer games that others can join, open your Windows firewall (and any other firewall software you might have) and add port rules to allow incoming TCP and UDP for ports 7777 to 7787.

10. Open `Config/DefaultGame.ini` and replace the APIKey (**OWSAPICustomerKey=""**) with the **OWSAPIKey** you used in step 6. Make sure you keep the double quotes at the start and end of the APIKey.

11. Open a Command Prompt to the **OWSInstanceLauncher** folder. Then type: `dotnet owsinstancelauncher.dll` and press enter. Agree to any security warnings that are displayed.

12. If the OWS Instance Launcher is working, you will see `Attempting to Register OWS Instance Launcher with RabbitMQ ServerSpinUp Queue...` in yellow, followed by `Registered OWS Instance Launcher with RabbitMQ ServerSpinUp Queue Success!` in green.

13. If you get an error `Unable to configure HTTPS endpoint. No server certificate was specified, and the default developer certificate could not be found or is out of date.`, then run the following command in the command prompt and press enter `dotnet dev-certs https --trust`. Now run `dotnet owsinstancelauncher.dll` again.

14. Right click on the **OpenWorldStarter.uproject**, and click on `Generate Visual Studio project files`.

15. Open the **OpenWorldStarter.sln** file in Visual Studio 2022. Set Open World Starter as the Default Startup Project. Make sure the configuration is set to **Development Editor**. Then press the Play button.

16. After the project opens in Unreal Engine, open the `ThirdPersonBP/Maps` folder and open the **Login** map. Make sure that the play button in Unreal Engine is set to **Play as Standalone**. Press the play button in Unreal Engine.

17. At the login screen, click on **Create New User**. Use that newly created username and password to login.

18. Click on the **Create Character** button to create a new character.

19. If your ISP doesn't support auto-loopback and your Unreal Engine server is running on the same device as your game client, you will need to go into the SQL database using SQL Server Management Studio (SSMS) and set IsInternalNetworkTestUser to 1 on the Characters table for the Character you are trying to connect with. This will replace your **IP** with `127.0.0.1` and allow you to connect to a UE5 server on the same device as your game client. Without this setting, you will not be able to connect and will get kicked back to the login screen.

20. Click the **Select Character** button. It will take about 15 seconds the first time as it has to boot up the dedicated server. Running to Map2 (the connected map) will also take 15 seconds the first time it has to boot up. After they are up and running, it will only take a second or two.

21. To shut down the Zone Server(s) and the OWS Instance Launcher, set your focus to the OWS Instance Launcher console window and press **Ctrl-C**.
