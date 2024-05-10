<p align="center"><img src="img/Logo512pxWhite.png" alt="SabreDartStudios" width="128"></p>
<p align="center">
    <h3 align="center">Open World Server (OWS)</h3>
    <h5 align="center"><a href="http://www.sabredartstudios.com/">By Sabre Dart Studios</a></h5>
</p>

<div align="center">

  ![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/Dartanlla/OWS/.github/workflows/ci.yml?branch=main&style=flat-square)
  <a href="https://github.com/Dartanlla/OWS/blob/master/LICENSE">![LICENSE](https://img.shields.io/github/license/Dartanlla/ows.svg?style=flat-square)</a>
  <a href="https://discord.gg/qZ76Cmxcgp">![Join Discord](https://img.shields.io/badge/Discord-%237289DA.svg?style=flat-square&logo=discord&logoColor=white)</a>
  ![Unreal Engine](https://img.shields.io/badge/unrealengine-%23313131.svg?style=flat-square&logo=unrealengine&logoColor=white)
  ![Docker](https://img.shields.io/badge/docker-%230db7ed.svg?style=flat-square&logo=docker&logoColor=white)
  ![.Net](https://img.shields.io/badge/.NET-5C2D91?style=flat-square&logo=.net&logoColor=white)

</div>

Open World Server (OWS) is a server instance manager designed to create large worlds in UE5. Either by stitching together multiple UE5 maps or by splitting a single large map into multiple zones, OWS will spin up and shut down server instances as needed based on your world population. OWS can load balance your world across multiple hardware devices. OWS can support thousands of players in the same world by instancing up and out. A single zone can be instanced multiple times to support a large population in one area. Areas of a map can also be split up into multiple zones to support a larger population. In addition to server instance management, OWS also handles persistence for Accounts, Characters, Abilities, Inventory and more.

### Projects

| Project                                                         | Purpose                                                            |
|-----------------------------------------------------------------|--------------------------------------------------------------------|
| [Benchmarks](src/OWSBenchmarks)                                 | This project will allow us to configure and run performance testing on the OWS API.  This will be important for comparing the impact of certain changes. |
| [Character Persistence](src/OWSCharacterPersistence)            | The Character Persistence API will be responsible for storing our player characters and all related data.  This will be important for comparing the impact of certain changes. |
| [Data](src/OWSData)                                             | This is a shared project that houses our data repository access code. |
| [External Login Providers](src/OWSExternalLoginProviders)       | This project contains code for integrating with external login providers such as Xsolla, Google, Facebook, etc. |
| [Instance Launcher](src/OWSInstanceLauncher)                    | This project builds our Instance Launcher that replaces the RPG World Server in OWS 1. |
| [Instance Management](src/OWSInstanceManagement)                | This API manages Zone Instances and the OWS Instance Launchers. |
| [Public API](src/OWSPublicAPI)                                  | This API handles all API calls that come directly from player clients such as registration, login, and connecting to the game. |
| [Shared](src/OWSShared)                                         | This project houses various miscellaneous code that multiple other projects require. |
| [Tests](src/OWSTests)                                           | This project provides Unit testing and Functional testing and Benchmark Tools (Development) |

# Contributing
* [Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)
* [Naming Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines)
* [Coding Style](https://github.com/dotnet/corefx/blob/368fdfd86ee3a3bf1bca2a6c339ee590f3d6505d/Documentation/coding-guidelines/coding-style.md)

# Documentation
[Open World Server Documentation](https://www.openworldserver.com/)
