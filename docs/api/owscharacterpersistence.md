---
layout: default
title: OWSCharacterPersistence
nav_order: 7
---

# OWSCharacterPersistence
This page documents endpoints in the `OWSCharacterPersistence` project. This API is intended for internal network use and should not be exposed to the internet.

## Common Headers
Most endpoints rely on this header:

| Name | Required | Type | Description |
|---|---|---|---|
| `X-CustomerGUID` | Yes | `guid` | Customer/tenant identifier used by OWS middleware and repositories. |

## Abilities

## Abilities/AddAbilityToCharacter
Adds an ability to a character.  When a character is granted an ability that needs to be persistent, call this API to store `AbilityName` and `AbilityLevel` for the `CharacterName`.  `CharHasAbilitiesCustomJSON` is an optional value that ccan be used to store additional ability related 

- Method: `POST`
- Route: `/api/Abilities/AddAbilityToCharacter`
- Content-Type: `application/json`

### Request Body
| Field | Required | Type | Description |
|---|---|---|---|
| `AbilityName` | Yes | `string` | Ability name to add. |
| `AbilityLevel` | Yes | `int` | Ability level value. |
| `CharacterName` | Yes | `string` | Character to modify. |
| `CharHasAbilitiesCustomJSON` | No | `string` | Per-character ability custom JSON payload. |

### Response
`200 OK` with `SuccessAndErrorMessage`.

| Field | Type | Description |
|---|---|---|
| `Success` | `bool` | `true` if operation completed. |
| `ErrorMessage` | `string` | Error details when unsuccessful. |

## Abilities/GetAbilities
Gets all abilities.

- Method: `GET`
- Route: `/api/Abilities/GetAbilities`

### Response
`200 OK` with array of `Abilities`.

Key fields:

| Field | Type | Description |
|---|---|---|
| `AbilityId` | `int` | Ability ID. |
| `AbilityName` | `string` | Ability name. |
| `AbilityTypeId` | `int` | Ability type ID. |
| `AbilityTypeName` | `string` | Ability type display name. |
| `GameplayAbilityClassName` | `string` | Gameplay ability class path/name. |

## Abilities/GetAbilityBars
Gets ability bars for a character.  This intended to be used for games where players can customize their ability UI and put abilities into slots on bars.  This API only gets the ability bars and their configurations, not the abilities on the bars.

- Method: `POST`
- Route: `/api/Abilities/GetAbilityBars`
- Content-Type: `application/json`

### Request Body
| Field | Required | Type | Description |
|---|---|---|---|
| `CharacterName` | Yes | `string` | Character to query. |

### Response
`200 OK` with array of `GetAbilityBars`.

| Field | Type | Description |
|---|---|---|
| `CharAbilityBarID` | `int` | Ability bar ID. |
| `AbilityBarName` | `string` | Ability bar name. |
| `CharacterID` | `int` | Character ID. |
| `MaxNumberOfSlots` | `int` | Max slots on bar. |
| `NumberOfUnlockedSlots` | `int` | Unlocked slot count. |
| `CharAbilityBarsCustomJSON` | `string` | Ability bar custom JSON. |

## Abilities/GetAbilityBarsAndAbilities
Gets ability bars and abilities for a character.  This intended to be used for games where players can customize their ability UI and put abilities into slots on bars.  This API gets the ability bars and the abilities on those bars.

- Method: `POST`
- Route: `/api/Abilities/GetAbilityBarsAndAbilities`
- Content-Type: `application/json`

### Request Body
| Field | Required | Type | Description |
|---|---|---|---|
| `CharacterName` | Yes | `string` | Character to query. |

### Response
`200 OK` with array of `GetAbilityBarsAndAbilities`.

| Field | Type | Description |
|---|---|---|
| `AbilityBarName` | `string` | Ability bar name. |
| `CharAbilityBarID` | `int` | Ability bar ID. |
| `CharacterID` | `int` | Character ID. |
| `Abilities` | `array` | Nested list of abilities on that bar. |

## Abilities/GetCharacterAbilities
Gets a list of all abilities assigned to a character.

- Method: `POST`
- Route: `/api/Abilities/GetCharacterAbilities`
- Content-Type: `application/json`

### Request Body
| Field | Required | Type | Description |
|---|---|---|---|
| `CharacterName` | Yes | `string` | Character to query. |

### Response
`200 OK` with array of `GetCharacterAbilities`.

| Field | Type | Description |
|---|---|---|
| `AbilityID` | `int` | Ability ID. |
| `AbilityName` | `string` | Ability name. |
| `AbilityLevel` | `int` | Character ability level. |
| `CharHasAbilitiesID` | `int` | Character-ability relation ID. |
| `CharHasAbilitiesCustomJSON` | `string` | Per-character ability custom JSON. |
| `CharacterID` | `int` | Character ID. |

## Abilities/RemoveAbilityFromCharacter
Removes an ability from a character.  This is the opposite of AddAbilityToCharacter.

- Method: `POST`
- Route: `/api/Abilities/RemoveAbilityFromCharacter`
- Content-Type: `application/json`

### Request Body
| Field | Required | Type | Description |
|---|---|---|---|
| `AbilityName` | Yes | `string` | Ability name to remove. |
| `CharacterName` | Yes | `string` | Character to modify. |

### Response
`200 OK` with `SuccessAndErrorMessage`.

## Abilities/UpdateAbilityOnCharacter
Updates a character ability.

- Method: `POST`
- Route: `/api/Abilities/UpdateAbilityOnCharacter`
- Content-Type: `application/json`

### Request Body
| Field | Required | Type | Description |
|---|---|---|---|
| `AbilityName` | Yes | `string` | Ability name to update. |
| `AbilityLevel` | Yes | `int` | New ability level. |
| `CharacterName` | Yes | `string` | Character to modify. |
| `CharHasAbilitiesCustomJSON` | No | `string` | Per-character ability custom JSON payload. |

### Response
`200 OK` with `SuccessAndErrorMessage`.

## Characters

## Characters/AddOrUpdateCustomData
Adds or updates one custom character data field.  We generally store data in JSON string format in these custom character data field so that one custom character data field could potentially hold dozens or hundreds of data values.  In the Hub World MMO example project we split up our character data into chunks based on access frequency, update frequency, and potential for data loss.

- Method: `POST`
- Route: `/api/Characters/AddOrUpdateCustomData`
- Content-Type: `application/json`

### Request Body
| Field | Required | Type | Description |
|---|---|---|---|
| `addOrUpdateCustomCharacterData` | Yes | `object` | Custom data payload object. |
| `addOrUpdateCustomCharacterData.CharacterName` | Yes | `string` | Character name. |
| `addOrUpdateCustomCharacterData.CustomFieldName` | Yes | `string` | Custom field key. |
| `addOrUpdateCustomCharacterData.FieldValue` | Yes | `string` | Custom field value. |

### Response
`200 OK` with an empty response body.

## Characters/GetByName
Get basic character information for a character name `CharacterName`.

- Method: `POST`
- Route: `/api/Characters/GetByName`
- Content-Type: `application/json`

### Request Body
| Field | Required | Type | Description |
|---|---|---|---|
| `CharacterName` | Yes | `string` | Character name to retrieve. |

### Response
`200 OK` with `GetCharByCharName`.

Key fields:

| Field | Type | Description |
|---|---|---|
| `CharacterId` | `int` | Character ID. |
| `CharName` | `string` | Character name. |
| `MapName` | `string` | Current zone/map. |
| `CharacterLevel` | `short` | Character level. |
| `X`,`Y`,`Z` | `double` | Character position. |
| `ClassName` | `string` | Character class name. |

## Characters/GetCustomData
Gets all custom character data fields for a character.  This is generally used to get all custom character data each time a player connects to a new server instance.

- Method: `POST`
- Route: `/api/Characters/GetCustomData`
- Content-Type: `application/json`

### Request Body
| Field | Required | Type | Description |
|---|---|---|---|
| `CharacterName` | Yes | `string` | Character name to query. |

### Response
`200 OK` with `CustomCharacterDataRows`.

| Field | Type | Description |
|---|---|---|
| `Rows` | `array` | Custom data rows for the character. |
| `Rows[].CustomFieldName` | `string` | Custom field key. |
| `Rows[].FieldValue` | `string` | Custom field value. |

## Characters/PlayerLogout
Logs out a character.

- Method: `POST`
- Route: `/api/Characters/PlayerLogout`
- Content-Type: `application/json`

### Request Body
| Field | Required | Type | Description |
|---|---|---|---|
| `CharacterName` | Yes | `string` | Character name to log out. |

### Response
`200 OK` with `SuccessAndErrorMessage`.

## Characters/UpdateAllPlayerPositions
Updates position/rotation for multiple players in one request.  This API accepts `SerializedPlayerLocationData` which contains multiple player locations to update.

- Method: `POST`
- Route: `/api/Characters/UpdateAllPlayerPositions`
- Content-Type: `application/json`

### Request Body
| Field | Required | Type | Description |
|---|---|---|---|
| `SerializedPlayerLocationData` | Yes | `string` | Pipe-delimited records, each record `PlayerName:X:Y:Z:RX:RY:RZ`. |
| `MapName` | Yes | `string` | Map name used for position updates. |

### Response
`200 OK` with `SuccessAndErrorMessage`.

## Characters/UpdateCharacterStats
Updates a character's stat block.

- Method: `POST`
- Route: `/api/Characters/UpdateCharacterStats`
- Content-Type: `application/json`

### Request Body
| Field | Required | Type | Description |
|---|---|---|---|
| `updateCharacterStats` | Yes | `object` | Full stat payload for the character. |
| `updateCharacterStats.CharName` | Yes | `string` | Character name to update. |
| `updateCharacterStats.CharacterLevel` | No | `int` | Character level. |
| `updateCharacterStats.XP` | No | `int` | Character XP. |
| `updateCharacterStats.X`,`Y`,`Z` | No | `float` | Position values. |
| `updateCharacterStats.RX`,`RY`,`RZ` | No | `float` | Rotation values. |
| `updateCharacterStats.Health`,`MaxHealth` | No | `float` | Health values. |

Note: `UpdateCharacterStats` contains many additional fields (attributes, resources, currencies, combat stats, etc.) and is passed through to the repository.

### Response
`200 OK` with `SuccessAndErrorMessage`.

## Status

## Status/GetCharacterStatuses
Gets character status data by character name.

- Method: `POST`
- Route: `/api/Status/GetCharacterStatuses`
- Content-Type: `application/json`

### Request Body
| Field | Required | Type | Description |
|---|---|---|---|
| `CharacterName` | Yes | `string` | Character name to query. |

### Response
`200 OK` with `GetCharByCharName`.

## System

## System/Status
Returns API health status.

- Method: `GET`
- Route: `/api/System/Status`

### Response
`200 OK` with boolean `true` when the API is running.
