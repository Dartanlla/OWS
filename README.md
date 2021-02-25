# OWS
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

# Coding Guidelines
- https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions
- https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines
- https://github.com/dotnet/corefx/blob/368fdfd86ee3a3bf1bca2a6c339ee590f3d6505d/Documentation/coding-guidelines/coding-style.md
