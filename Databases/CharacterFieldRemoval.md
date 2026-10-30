# Characters field removal (20261029)

Deploy the API changes and the upgrade script for the database provider together:

| Provider | Upgrade script |
|---|---|
| PostgreSQL | `Postgres/UpdateScripts/20261029UpdateFrom20260401to20261029.sql` |
| SQL Server | `MSSQL/UpdateScripts/20261029UpdateFrom20230304to20261029.sql` |
| MySQL | `MySQL/UpdateScripts/20261029UpdateFrom20230304to20261029.sql` |

These scripts permanently remove the listed character attributes, resources,
currencies, progression fields, description, email, and privilege flags where
present. They also replace customer setup routines that insert character rows.
The SQL Server script replaces `AddCharacter`; the PostgreSQL script updates
the legacy `JoinMapByCharName` routine. The PostgreSQL cumulative upgrade includes
the same changes.

Character lookup, listing, and creation responses omit the removed fields.
The existing `UpdateCharacterStats` endpoint accepts character name, customer GUID,
position, and rotation; it updates only position and rotation. Unknown legacy JSON
fields are ignored. Local network testing uses `IsInternalNetworkTestUser`.

The `Class` table and user account email fields retain their existing columns.
Game-specific data can use the existing custom character data endpoints.

Validation: contract and serialization tests cover the removed fields. The
PostgreSQL upgrade, character creation with default and class inventories, and
position updates were exercised in a disposable PostgreSQL 16 database. SQL Server
and MySQL upgrades require verification on their respective database engines.
