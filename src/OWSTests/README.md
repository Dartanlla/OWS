# OWS API regression tests

From the repository root, run this before checking in changes.

```sh
dotnet test src/OWSTests/OWSTests.csproj
```

The suite requires the .NET 10 SDK. It runs locally with in-memory HTTP hosts and
test doubles. After NuGet restore, the tests run without Unreal, Docker, a
database, or cloud credentials. A build or test failure makes the command exit
nonzero.

## Coverage

The route inventory checks all 77 application API routes across the six HTTP
services. Each route has a behavior test. The Public API, Character Persistence,
and Instance Management tests also invoke their API Gateway handlers with
synthetic events. OWSInstanceLauncher exposes Razor pages and runs background
jobs. Its API calls to Instance Management are covered on the receiving side.

| Service | Routes | Scenarios |
| --- | ---: | --- |
| Public API | 19 | Registration, password login and the disabled external-login response, session lookup, character creation by class and by defaults, roster, selection, deletion, logout, player-group filters, public character fields, zone assignment and polling, ticket issuance, health |
| Character Persistence | 16 | Character lookup by name and ID, statuses, custom data read/write, stat saves, multi-player position saves, character logout, ability catalog and character loadouts, ability add/update/remove, ability bars, health |
| Instance Management | 27 | Zone creation, instance lookups and listings, world time, player counts, launcher registration/start/stop, instance spin-up/shutdown and status, ready assignments, Edge server registry and credentials, ticket redemption, health |
| Global Data | 3 | Tenant-scoped create/update/read, missing keys, health |
| Content | 8 | Zone cache read/write and missing zones, feature and package reads/writes, example GET and POST |
| Management | 4 | User list/create/edit, tenant header checks, health |

`PluginTravelTests` follows OWSPlugin's selected-character flow across Public API
and Instance Management, including launch messaging, polling before readiness,
and assignment after the instance becomes ready. Other tests check the HTTP
requests and response data used by the UE4 and UE5 plugin calls. The plugin
sources used for the audit were `OWSLoginWidget.cpp`,
`OWSPlayerControllerComponent.cpp`, `OWSGameMode.cpp`, and
`OWSAPISubsystem.cpp` under `plugins/OWSPlugin*/Source/OWSPlugin/Private`.

## Known differences in the current checkout

- UE5's `GetZoneInstanceFromZoneInstanceID` posts an object with `ZoneInstanceId`.
  The API accepts a JSON integer. ASP.NET returns 400 for the object and the
  Lambda handler throws during deserialization. Tests cover both request shapes.
- The plugin's player-group callback expects an object with `success` and
  `rows`. The Public API returns a JSON array. The tests check the current
  array and the requested group-type filter.
- `GetAbilityBarsAndAbilities` returns flat character abilities over ASP.NET
  and nested bars with abilities from the Lambda handler.
- Both plugin versions contain a commented-out `UpdateZone` call. The OWS2
  services in this checkout have no `UpdateZone` route. Legacy
  `RPGServer/*`, `RPGInventory/*`, and `RPGAbility/*` URLs belong to the
  earlier API and are outside the OWS2 route inventory.

These tests exercise OWS controllers, request handlers, middleware, and Lambda
dispatch. Database queries, cloud services, Unreal response parsing, deployment
configuration, and the legacy internal instance spin-up branch that opens a
RabbitMQ connection remain outside the test run. The redemption tests exercise
successful inspect and claim through ASP.NET with a test ticket store. The
Lambda cases cover request validation, instance authentication, and an
unconfigured store. Successful Lambda redemption uses its environment-configured
Valkey store.
