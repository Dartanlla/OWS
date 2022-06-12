// Copyright 2022 Sabre Dart Studios

#pragma once

#include "CoreMinimal.h"
#include "UObject/NoExportTypes.h"
#include "OWS2API.generated.h"

USTRUCT()
struct FSetSelectedCharacterAndConnectToLastZoneJSONPost
{
	GENERATED_BODY()

public:
	FSetSelectedCharacterAndConnectToLastZoneJSONPost() {
		UserSessionGUID = "";
		SelectedCharacterName = "";
	}

	UPROPERTY()
		FString UserSessionGUID;
	UPROPERTY()
		FString SelectedCharacterName;
};

USTRUCT()
struct FTravelToLastZoneServerJSONPost
{
	GENERATED_BODY()

public:
	FTravelToLastZoneServerJSONPost() {
		CharacterName = "";
		ZoneName = "";
		PlayerGroupType = 0;
	}

	UPROPERTY()
		FString CharacterName;
	UPROPERTY()
		FString ZoneName;
	UPROPERTY()
		int32 PlayerGroupType;
};

USTRUCT()
struct FGetCharacterStatsJSONPost
{
	GENERATED_BODY()

public:
	FGetCharacterStatsJSONPost() {
		CharacterName = "";
	}

	UPROPERTY()
		FString CharacterName;
};

USTRUCT()
struct FGetCustomCharacterDataJSONPost {
	GENERATED_BODY()

public:
	FGetCustomCharacterDataJSONPost() {
		CharacterName = "";
	}

	UPROPERTY()
		FString CharacterName;
};

USTRUCT()
struct FCustomCharacterData {
	GENERATED_BODY()

public:
	FCustomCharacterData() {
		CharacterName = "";
		CustomFieldName = "";
		FieldValue = "";
	}

	UPROPERTY()
		FString CharacterName;
	UPROPERTY()
		FString CustomFieldName;
	UPROPERTY()
		FString FieldValue;
};

USTRUCT()
struct FAddOrUpdateCustomCharacterDataJSONPost {
	GENERATED_BODY()

public:
	FAddOrUpdateCustomCharacterDataJSONPost() {
		AddOrUpdateCustomCharacterData = FCustomCharacterData();
	}

	UPROPERTY()
		FCustomCharacterData AddOrUpdateCustomCharacterData;
};

USTRUCT()
struct FGetAllCharactersJSONPost
{
	GENERATED_BODY()

public:
	FGetAllCharactersJSONPost() {
		UserSessionGUID = "";
	}

	UPROPERTY()
		FString UserSessionGUID;
};

USTRUCT(BlueprintType, Blueprintable)
struct FUserCharacter
{
	GENERATED_USTRUCT_BODY()

public:
	FUserCharacter() {
		CharacterName = "";
		ClassName = "";
		Level = 0;
		Gender = 0;
		ZoneName = "";
		Gold = 0;
		Silver = 0;
		Copper = 0;
		FreeCurrency = 0;
		PremiumCurrency = 0;
		Score = 0;
		XP = 0;
		LastActivity = "";
		CreateDate = "";
	}

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		FString CharacterName;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		FString ClassName;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		int32 Level;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		int32 Gender;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		FString ZoneName;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		int32 Gold;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		int32 Silver;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		int32 Copper;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		int32 FreeCurrency;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		int32 PremiumCurrency;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		int32 Score;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		int32 XP;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		FString LastActivity;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		FString CreateDate;

};

USTRUCT()
struct FLoginAndCreateSessionJSONPost
{
	GENERATED_BODY()

public:
	FLoginAndCreateSessionJSONPost() {
		Email = "";
		Password = "";
	}

	UPROPERTY()
		FString Email;
	UPROPERTY()
		FString Password;

	FLoginAndCreateSessionJSONPost(FString InEmail, FString InPassword)
	{
		InEmail.TrimStartAndEndInline();

		Email = InEmail;
		Password = InPassword;
	}
};

USTRUCT(BlueprintType)
struct FLoginAndCreateSession
{
	GENERATED_BODY()

public:
	FLoginAndCreateSession() {
		Authenticated = false;
		ErrorMessage = "";
		UserSessionGUID = "";
	}

	UPROPERTY()
		bool Authenticated;
	UPROPERTY()
		FString ErrorMessage;
	UPROPERTY()
		FString UserSessionGUID;
};

USTRUCT()
struct FRegisterJSONPost
{
	GENERATED_BODY()

public:
	FRegisterJSONPost() {
		Email = "";
		Password = "";
		FirstName = "";
		LastName = "";
	}

	UPROPERTY()
		FString Email;
	UPROPERTY()
		FString Password;
	UPROPERTY()
		FString FirstName;
	UPROPERTY()
		FString LastName;

	FRegisterJSONPost(FString InEmail, FString InPassword, FString InFirstName, FString InLastName)
	{
		//Trim whitespace
		InEmail.TrimStartAndEndInline();
		InFirstName.TrimStartAndEndInline();
		InLastName.TrimStartAndEndInline();

		Email = InEmail;
		Password = InPassword;
		FirstName = InFirstName;
		LastName = InLastName;
	}
};

USTRUCT(BlueprintType)
struct FSuccessAndErrorMessage
{
	GENERATED_BODY()

public:
	FSuccessAndErrorMessage() {
		Success = true;
		ErrorMessage = "";
	}

	UPROPERTY()
		bool Success;
	UPROPERTY()
		FString ErrorMessage;
};

USTRUCT()
struct FCharacterNameJSONPost
{
	GENERATED_BODY()

public:
	FCharacterNameJSONPost() {
		CharacterName = "";
	}

	UPROPERTY()
		FString CharacterName;
};

USTRUCT()
struct FAddAbilityToCharacterJSONPost
{
	GENERATED_BODY()

public:
	FAddAbilityToCharacterJSONPost() {
		CharacterName = "";
		AbilityName = "";
		AbilityLevel = 0;
		CharHasAbilitiesCustomJSON = "";
	}

	UPROPERTY()
		FString CharacterName;
	UPROPERTY()
		FString AbilityName;
	UPROPERTY()
		int32 AbilityLevel;
	UPROPERTY()
		FString CharHasAbilitiesCustomJSON;
};

USTRUCT()
struct FUpdateAbilityOnCharacterJSONPost
{
	GENERATED_BODY()

public:
	FUpdateAbilityOnCharacterJSONPost() {
		CharacterName = "";
		AbilityName = "";
		AbilityLevel = 0;
		CharHasAbilitiesCustomJSON = "";
	}

	UPROPERTY()
		FString CharacterName;
	UPROPERTY()
		FString AbilityName;
	UPROPERTY()
		int32 AbilityLevel;
	UPROPERTY()
		FString CharHasAbilitiesCustomJSON;
};

USTRUCT()
struct FRemoveAbilityFromCharacterJSONPost
{
	GENERATED_BODY()

public:
	FRemoveAbilityFromCharacterJSONPost() {
		CharacterName = "";
		AbilityName = "";
	}

	UPROPERTY()
		FString CharacterName;
	UPROPERTY()
		FString AbilityName;
};

USTRUCT(BlueprintType)
struct FAbility
{
	GENERATED_BODY()

public:
	FAbility()
	{
		AbilityID = 0;
		AbilityCustomJSON = "";
		AbilityName = "";
		AbilityTypeID = 0;
		Class = "";
		CustomerGUID = "";
		Race = "";
		TextureToUseForIcon = "";
		GameplayAbilityClassName = "";
		CharHasAbilitiesID = 0;
		AbilityLevel = 0;
		CharHasAbilitiesCustomJSON = "";
		CharacterID = 0;
		CharName = "";
		AbilityInSlotNumber = 0;
	}

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Struct")
		int32 AbilityID;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Struct")
		FString AbilityCustomJSON;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Struct")
		FString AbilityName;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Struct")
		int32 AbilityTypeID;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Struct")
		FString Class;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Struct")
		FString CustomerGUID;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Struct")
		FString Race;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Struct")
		FString TextureToUseForIcon;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Struct")
		FString GameplayAbilityClassName;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Struct")
		int32 CharHasAbilitiesID;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Struct")
		int32 AbilityLevel;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Struct")
		FString CharHasAbilitiesCustomJSON;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Struct")
		int32 CharacterID;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Struct")
		FString CharName;

	//This one is not used if we are just looking at all abilities a character has
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Struct")
		int32 AbilityInSlotNumber;
};

USTRUCT(BlueprintType)
struct FAbilityBar
{
	GENERATED_BODY()

public:
	FAbilityBar() {
		CharAbilityBarID = 0;
		AbilityBarName = "";
		CharacterID = 0;
		MaxNumberOfSlots = 0;
		NumberOfUnlockedSlots = 0;
		CharAbilityBarsCustomJSON = "";
	}

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Bar Struct")
		int32 CharAbilityBarID;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Bar Struct")
		FString AbilityBarName;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Bar Struct")
		int32 CharacterID;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Bar Struct")
		int32 MaxNumberOfSlots;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Bar Struct")
		int32 NumberOfUnlockedSlots;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Bar Struct")
		FString CharAbilityBarsCustomJSON;

};

USTRUCT(BlueprintType)
struct FAbilityBarWithAbilities
{
	GENERATED_BODY()

public:
	FAbilityBarWithAbilities() {
		CharAbilityBarID = 0;
		AbilityBarName = "";
		CharacterID = 0;
		MaxNumberOfSlots = 0;
		NumberOfUnlockedSlots = 0;
		CharAbilityBarsCustomJSON = "";
	}

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Bar Struct")
		int32 CharAbilityBarID;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Bar Struct")
		FString AbilityBarName;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Bar Struct")
		int32 CharacterID;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Bar Struct")
		int32 MaxNumberOfSlots;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Bar Struct")
		int32 NumberOfUnlockedSlots;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Bar Struct")
		FString CharAbilityBarsCustomJSON;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Ability Bar Struct")
		TArray<FAbility> Abilities;
};


USTRUCT()
struct FUpdateAllPlayerPositionsJSONPost
{
	GENERATED_BODY()

public:
	FUpdateAllPlayerPositionsJSONPost() {
		SerializedPlayerLocationData = "";
		MapName = "";
	}

	UPROPERTY()
		FString SerializedPlayerLocationData;
	UPROPERTY()
		FString MapName;
};


USTRUCT()
struct FUpdateNumberOfPlayersJSONPost
{
	GENERATED_BODY()

public:
	FUpdateNumberOfPlayersJSONPost() {
		ZoneInstanceId = 0;
		NumberOfConnectedPlayers = "";
	}

	UPROPERTY()
		int32 ZoneInstanceId;
	UPROPERTY()
		FString NumberOfConnectedPlayers;
};

USTRUCT()
struct FZoneNameJSONPost
{
	GENERATED_BODY()

public:
	FZoneNameJSONPost() {
		ZoneName = "";
	}

	UPROPERTY()
		FString ZoneName;
};

USTRUCT()
struct FGetZoneInstancesForZoneJSONPost
{
	GENERATED_BODY()

public:
	FGetZoneInstancesForZoneJSONPost() {
		Request = FZoneNameJSONPost();
	}

	UPROPERTY()
		FZoneNameJSONPost Request;
};

USTRUCT(BlueprintType)
struct FZoneInstance
{
	GENERATED_BODY()

public:
	FZoneInstance() {
		MapID = 0;
		MapName = "";
		ZoneName = "";
		WorldCompContainsFilter = "";
		MapMode = "";
		SoftPlayerCap = 0;
		HardPlayerCap = 0;
		MinutesToShutdownAfterEmpty = 0;
		MapInstanceID = 0;
		WorldServerID = 0;
		Port = 0;
		Status = 0;
		PlayerGroupID = 0;
		NumberOfReportedPlayers = 0;
		LastUpdateFromServer = FDateTime::MinValue();
		LastServerEmptyDate = FDateTime::MinValue();
		CreateDate = FDateTime::MinValue();
	}

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Zone Struct")
		int32 MapID;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Zone Struct")
		FString MapName;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Zone Struct")
		FString ZoneName;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Zone Struct")
		FString WorldCompContainsFilter;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Zone Struct")
		FString MapMode;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Zone Struct")
		int32 SoftPlayerCap;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Zone Struct")
		int32 HardPlayerCap;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Zone Struct")
		int32 MinutesToShutdownAfterEmpty;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Zone Struct")
		int32 MapInstanceID;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Zone Struct")
		int32 WorldServerID;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Zone Struct")
		int32 Port;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Zone Struct")
		int32 Status;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Zone Struct")
		int32 PlayerGroupID;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Zone Struct")
		int32 NumberOfReportedPlayers;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Zone Struct")
		FDateTime LastUpdateFromServer;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Zone Struct")
		FDateTime LastServerEmptyDate;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Zone Struct")
		FDateTime CreateDate;

};

USTRUCT()
struct FCreateCharacterJSONPost
{
	GENERATED_BODY()

public:
	FCreateCharacterJSONPost() {
		UserSessionGUID = "";
		CharacterName = "";
		ClassName = "";
	}

	UPROPERTY()
		FString UserSessionGUID;

	UPROPERTY()
		FString CharacterName;

	UPROPERTY()
		FString ClassName;
};

USTRUCT(BlueprintType, Blueprintable)
struct FCreateCharacter
{
	GENERATED_USTRUCT_BODY()

public:
	FCreateCharacter() {
		Success = true;
		ErrorMessage = "";
		CharacterName = "";
		ClassName = "";
		CharacterLevel = 0;
		StartingMapName = "";
		X = 0.f;
		Y = 0.f;
		Z = 0.f;
		RX = 0.f;
		RY = 0.f;
		RZ = 0.f;
		TeamNumber = 0;
		Gold = 0;
		Silver = 0;
		Copper = 0;
		FreeCurrency = 0;
		PremiumCurrency = 0;
		Fame = 0;
		Alignment = 0;
		Score = 0;
		Gender = 0;
		XP = 0;
		Size = 0;
		Weight = 0.f;
		LastActivity = "";
		CreateDate = "";
	}

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		bool Success;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		FString ErrorMessage;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		FString CharacterName;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		FString ClassName;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		int32 CharacterLevel;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		FString StartingMapName;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		float X;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		float Y;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		float Z;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		float RX;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		float RY;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		float RZ;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		int32 TeamNumber;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		int32 Gold;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		int32 Silver;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		int32 Copper;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		int32 FreeCurrency;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		int32 PremiumCurrency;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		int32 Fame;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		int32 Alignment;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		int32 Score;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		int32 Gender;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		int32 XP;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		int32 Size;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		float Weight;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		FString LastActivity;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Character")
		FString CreateDate;

};

USTRUCT()
struct FRemoveCharacterJSONPost
{
	GENERATED_BODY()

public:
	FRemoveCharacterJSONPost() {
		UserSessionGUID = "";
		CharacterName = "";
	}

	UPROPERTY()
		FString UserSessionGUID;

	UPROPERTY()
		FString CharacterName;

};

USTRUCT()
struct FGetPlayerGroupsCharacterIsInJSONPost
{
	GENERATED_BODY()

public:
	FGetPlayerGroupsCharacterIsInJSONPost() {
		UserSessionGUID = "";
		CharacterName = "";
		PlayerGroupTypeID = 0;
	}

	UPROPERTY()
		FString UserSessionGUID;

	UPROPERTY()
		FString CharacterName;

	UPROPERTY()
		int32 PlayerGroupTypeID;
};

USTRUCT(BlueprintType, Blueprintable)
struct FPlayerGroup
{
	GENERATED_USTRUCT_BODY()

		FPlayerGroup() {
		PlayerGroupID = 0;
		PlayerGroupName = "";
		PlayerGroupTypeID = 0;
		DateAdded = FDateTime::MinValue();
		ReadyState = 0;
		TeamNumber = 0;
	}

	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Player Group")
		int32 PlayerGroupID;
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Player Group")
		FString PlayerGroupName;
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Player Group")
		int32 PlayerGroupTypeID;
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Player Group")
		FDateTime DateAdded;
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Player Group")
		int32 ReadyState;
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Player Group")
		int32 TeamNumber;
};

USTRUCT()
struct FLaunchZoneInstance
{
	GENERATED_BODY()

public:
	FLaunchZoneInstance() {
		CharacterName = "";
		ZoneName = "";
		PlayerGroupTypeID = 0;
	}

	UPROPERTY()
		FString CharacterName;

	UPROPERTY()
		FString ZoneName;

	UPROPERTY()
		int32 PlayerGroupTypeID;
};


USTRUCT()
struct FAddOrUpdateZone
{
	GENERATED_BODY()

public:
	FAddOrUpdateZone() {
		MapID = 0;
		ZoneName = "";
		MapName = "";
		WorldCompContainsFilter = "";
		WorldCompListFilter = "";
		SoftPlayerCap = 0;
		HardPlayerCap = 0;
		MapMode = 0;
	}

	UPROPERTY()
		int32 MapID;

	UPROPERTY()
		FString ZoneName;

	UPROPERTY()
		FString MapName;

	UPROPERTY()
		FString WorldCompContainsFilter;

	UPROPERTY()
		FString WorldCompListFilter;

	UPROPERTY()
		int32 SoftPlayerCap;

	UPROPERTY()
		int32 HardPlayerCap;

	UPROPERTY()
		int32 MapMode;
};

USTRUCT()
struct FAddZoneJSONPost
{
	GENERATED_BODY()

public:
	FAddZoneJSONPost() {
		AddOrUpdateZone = FAddOrUpdateZone();
	}

	UPROPERTY()
		FAddOrUpdateZone AddOrUpdateZone;
};

USTRUCT()
struct FUpdateZoneJSONPost
{
	GENERATED_BODY()

public:
	FUpdateZoneJSONPost() {
		AddOrUpdateZone = FAddOrUpdateZone();
	}

	UPROPERTY()
		FAddOrUpdateZone AddOrUpdateZone;
};

USTRUCT()
struct FGetServerInstanceFromPort
{
	GENERATED_BODY()

public:
	UPROPERTY()
		FString MapName;

	UPROPERTY()
		FString ZoneName;

	UPROPERTY()
		FString WorldCompContainsFilter;

	UPROPERTY()
		FString WorldCompListFilter;

	UPROPERTY()
		int32 MapInstanceID;

	UPROPERTY()
		int32 Status;

	UPROPERTY()
		int32 MaxNumberOfInstances;

	UPROPERTY()
		FDateTime ActiveStartTime;

	UPROPERTY()
		int8 ServerStatus;

	UPROPERTY()
		FString InternalServerIP;

};

UENUM(BlueprintType)
namespace ERPGSchemeToChooseMap
{
	enum SchemeToChooseMap
	{
		Default = 0,
		MapWithFewestPlayers = 1 UMETA(DisplayName = "Map with fewest players"),
		MapWithMostPlayers = 2 UMETA(DisplayName = "Map with most players"),
		MapOnWorldServerWithLeastNumberOfMaps = 3 UMETA(DisplayName = "Map on World Server with least number of maps"),
		SpecificWorldServer = 4 UMETA(DisplayName = "Specific World Server")
	};
}

UENUM(BlueprintType)
namespace ERPGPlayerGroupType
{
	enum PlayerGroupType
	{
		NoGroup UMETA(DisplayName = "NoGroup"),
		Party UMETA(DisplayName = "Party"),
		Raid UMETA(DisplayName = "Raid"),
		Guild UMETA(DisplayName = "Guild"),
		Team UMETA(DisplayName = "Team"),
		Faction UMETA(DisplayName = "Faction"),
		FlightGroup UMETA(DisplayName = "FlightGroup"),
		Alliance UMETA(DisplayName = "Alliance"),
		Squad UMETA(DisplayName = "Squad"),
		Other = 99
	};
}

/**
 * 
 */
UCLASS()
class OWSPLUGIN_API UOWS2API : public UObject
{
	GENERATED_BODY()
	
};
