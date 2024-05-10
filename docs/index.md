---
layout: home
---


<p style="margin-bottom: -20px"> 
    <img src="assets/images/logo-black.png" alt="SabreDartStudios" width="400">
    <h4>
        <a href="http://www.sabredartstudios.com/">By SabreDartStudios</a>
    </h4>
</p>

<p>
    <img alt="GitHub Workflow Status" src="https://img.shields.io/github/actions/workflow/status/Dartanlla/OWS/.github/workflows/ci.yml?branch=main&style=flat-square">
    <a href="https://github.com/Dartanlla/OWS/blob/master/LICENSE">
        <img src="https://img.shields.io/github/license/Dartanlla/ows.svg?style=flat-square">
    </a>
    <a href="https://discord.gg/qZ76Cmxcgp">
        <img src="https://img.shields.io/badge/Discord-%237289DA.svg?style=flat-square&logo=discord&logoColor=white" alt="Join Discord">
    </a>
    <img src="https://img.shields.io/badge/unrealengine-%23313131.svg?style=flat-square&logo=unrealengine&logoColor=white">
    <img src="https://img.shields.io/badge/docker-%230db7ed.svg?style=flat-square&logo=docker&logoColor=white">
    <img src="https://img.shields.io/badge/.NET-5C2D91?style=flat-square&logo=.net&logoColor=white">
</p>

---

## What is OWS?

<iframe width="100%" height="410" src="https://www.youtube.com/embed/yRXzbaNT6_k" title="Open World Server 2.0 Overview" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

Open World Server (OWS) is a server instance manager designed to create large worlds in Unreal Engine. Either by stitching together multiple Unreal Engine maps or by splitting a single large map into multiple zones, OWS will spin up and shut down server instances as needed based on your world population. OWS can load balance your world across multiple hardware devices. OWS can support thousands of players in the same world by instancing up and out. A single zone can be instanced multiple times to support a large population in one area. Areas of a map can also be split up into multiple zones to support a larger population. In addition to server instance management, OWS also handles persistence for Accounts, Characters, Abilities, Inventory and more.

## Projects

| [Benchmarks](https://github.com/Dartanlla/OWS/tree/main/src/OWSBenchmarks){: .fw-500 } | This project will allow us to configure and run performance testing on the OWS API. This will be important for comparing the impact of certain changes. |
| [Character Persistence](https://github.com/Dartanlla/OWS/tree/main/src/OWSCharacterPersistence){: .fw-500 } | The Character Persistence API is responsible for storing our player characters and all related data. |
| [Data](https://github.com/Dartanlla/OWS/tree/main/src/OWSData){: .fw-500 } | This is a shared project that houses our data repository access code. |
| [External Login Providers](https://github.com/Dartanlla/OWS/tree/main/src/OWSExternalLoginProviders){: .fw-500 } | This project contains code for integrating with external login providers such as Xsolla, Google, Facebook, etc. |
| [Global Data](https://github.com/Dartanlla/OWS/tree/main/src/OWSGlobalData){: .fw-500 } | The Global Data API can be used to store data that is not related to a specific Character or User. |
| [Instance Launcher](https://github.com/Dartanlla/OWS/tree/main/src/OWSInstanceLauncher){: .fw-500 } | This project builds our Instance Launcher that replaces the RPG World Server in OWS 1. |
| [Instance Management](https://github.com/Dartanlla/OWS/tree/main/src/OWSInstanceManagement){: .fw-500 } | This API manages Zone Instances and the OWS Instance Launchers. |
| [Management](https://github.com/Dartanlla/OWS/tree/main/src/OWSManagement){: .fw-500 } | The Management website is work in-process project for managing the data in OWS 2. |
| [Public API](https://github.com/Dartanlla/OWS/tree/main/src/OWSPublicAPI){: .fw-500 } | This API handles all API calls that come directly from player clients such as registration, login, and connecting to the game. |
| [Shared](https://github.com/Dartanlla/OWS/tree/main/src/OWSShared){: .fw-500 } | This project houses various miscellaneous code that multiple other projects require. |
| [Status](https://github.com/Dartanlla/OWS/tree/main/src/OWSStatus){: .fw-500 } <span class="label label-yellow">Beta</span> | This service provides Monitoring & Health Checking. |
| [Tests](https://github.com/Dartanlla/OWS/tree/main/src/OWSTests){: .fw-500 } | This project provides Unit testing and Functional testing and Benchmark Tools. |

[Get started now](/getting-started){: .btn .btn-blue .mr-4}
[View on GitHub](https://github.com/Dartanlla/OWS){: .btn }

## Contributing

* [Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)
* [Naming Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines)
* [Coding Style](https://github.com/dotnet/corefx/blob/368fdfd86ee3a3bf1bca2a6c339ee590f3d6505d/Documentation/coding-guidelines/coding-style.md)
