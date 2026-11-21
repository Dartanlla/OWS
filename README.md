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

From the repository root, run the OWS API regression suite before check-in.

```sh
dotnet test src/OWSTests/OWSTests.csproj
```

See [OWSTests coverage and prerequisites](src/OWSTests/README.md).

# Documentation
[Open World Server Documentation](https://www.openworldserver.com/)

[OWSInventoryService setup and API](docs/api/owsinventoryservice.md)

### Nilux external connection tickets

`POST /api/Users/ExternalConnectionTicket` accepts `UserSessionGUID`, the shared
Edge's `ServerId`, and the destination `MapInstanceId` in JSON over HTTPS. Include
the same `X-CustomerGUID` header used for OWS login. The API checks that the
selected character is assigned to the ready destination and that the Edge is
active and credentialed for the same customer. It returns an opaque ticket, a
new Base64-encoded 32-byte session secret, `CharacterId`, `ServerId`, `MapInstanceId`, and
`ExpiresAtUtc`. The ticket expires after 45 seconds.

The default Docker Compose setup runs without the Nilux ticket integration. To
enable it, copy `src/.env` to `src/.env.tickets` and add a unique
`OWSTicketValkeyPassword` and configure the Instance Management service credentials as described
in the [Lambda setup guide](src/OWSPublicAPILambda/Readme.md). The copy is excluded from Git and Docker builds.
Use a password without commas because it is passed in a comma-delimited Valkey
connection string.

From `src`, add `--env-file .env.tickets` to the Compose command and append
`-f .docker/owsticket.yml` after your usual Compose files, before `up`. For
Visual Studio, set `DockerComposeEnvFilePath` to `.env.tickets` in
`docker-compose.dcproj` and append `;.docker/owsticket.yml` to
`AdditionalComposeFilePaths`.

The optional `owsticket` service stores records in memory, publishes
`127.0.0.1:6380`, and configures the containerized Public API automatically.
When running the Public API directly, configure
`OWSTicketConfig__ConnectionString` with `localhost:6380` and the Valkey
password.

`ITicketRepository` exposes `GetTicketAsync` for retrieving the secret before
handshake verification and `RedeemTicketAsync` for atomically claiming the ticket
after the Edge verifies the AES-GCM authentication tag. A repeated claim by the
same authenticated handshake succeeds until the original TTL expires; other
claimants and other server IDs are rejected.

Nilux Edge uses `POST /api/Instance/RedeemExternalConnectionTicket` on OWSInstanceManagement.
Lambda uses the existing Instance Management HTTP API Gateway. For standalone
ASP.NET hosting, configure a dedicated Kestrel HTTPS listener
bound to a private interface, restrict it to Edge with network policy, and set
`OWS_EDGE_PRIVATE_HTTPS_PORT` to its local port. Keep this port separate from
the public ingress and out of Compose's published ports. Requests on any other
listener receive 404. Check this from both the public ingress and the Edge
network before enabling redemption.

Edge runs independently of map launches. A trusted operator provisions an Edge
credential, and Edge sends it as `X-Edge-Redemption-Credential`. See the
[Lambda setup guide](src/OWSPublicAPILambda/Readme.md) for the database migration
and Edge readiness contract.
