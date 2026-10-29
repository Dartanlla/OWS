# Contributing

This fork runs the live backend for our game. Every change to `main` goes through a pull request.

## Branches

Branch off an up-to-date `main` and name the branch `<type>/<short-description>`:

| Type | Use for |
|---|---|
| `feature/` | new behaviour (`feature/guild-bank`) |
| `fix/` | bug fixes (`fix/zone-shutdown-race`) |
| `deps/` | package or image upgrades (`deps/npgsql-9`) |
| `ci/` | workflows and build tooling (`ci/codeql`) |
| `docs/` | documentation only (`docs/hetzner-backups`) |
| `chore/` | everything else (`chore/remove-mongo`) |

Dependabot opens its own `dependabot/...` branches.

## Pull requests

- **No direct pushes to `main`.** Open a pull request, even for one-line fixes, and fill in the template: what changed, why, how you tested it, deploy and migration notes, and risk.
- **CI must be green before merging.** The `Build and test` check builds `src/OWS.sln` in Release and runs the tests. The `Vulnerable packages` check fails on any NuGet package with a High or Critical advisory. Fix a failure in the PR rather than merging past it.
- **Review:** no approval is required for now. Code owners in `.github/CODEOWNERS` are requested automatically, and a code owner merging the PR once checks are green is the sign-off.
- **Squash and merge.** One commit per PR on `main`, titled with a clear summary. Delete the branch afterwards.
- Keep PRs focused. A dependency upgrade, a refactor and a feature are three PRs.

## Before you open a PR

```sh
dotnet build src/OWS.sln -c Release
dotnet test src/OWS.sln -c Release
```

The SDK version comes from `global.json` (any .NET 10 SDK). Postgres is the only supported database: new data-access code uses parameterized SQL against the Postgres repositories, and the MSSQL/MySQL implementations only need to keep compiling.

## Dependencies

Dependabot opens weekly PRs for NuGet packages, GitHub Actions and Docker base images. Minor and patch updates are grouped; major updates arrive one per PR. Review them like any other change. For anything that touches a database driver (Npgsql in particular), gRPC, or serialization, read the release notes before merging.
