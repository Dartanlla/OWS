// Copyright 2020 Sabre Dart Studios

#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "Runtime/Online/HTTP/Public/Http.h"
#include "OWS2API.h"
#include "OWSCharacter.h"
#include "OWSPlayerState.h"
#include "OWSPlayerControllerComponent.generated.h"



struct FChatGroup;

//Get Zone Server To Travel To
DECLARE_DELEGATE_OneParam(FNotifyGetZoneServerToTravelToDelegate, const FString&)
DECLARE_DELEGATE_OneParam(FErrorGetZoneServerToTravelToDelegate, const FString&)

//Get All Characters Delegates
DECLARE_DELEGATE_OneParam(FNotifyGetAllCharactersDelegate, const TArray<FUserCharacter>&)
DECLARE_DELEGATE_OneParam(FErrorGetAllCharactersDelegate, const FString&)

//Get Character Stats
DECLARE_DELEGATE_OneParam(FNotifyGetCharacterStatsDelegate, TSharedPtr<FJsonObject>)
DECLARE_DELEGATE_OneParam(FErrorGetCharacterStatsDelegate, const FString&)

//Update Character Stats
DECLARE_DELEGATE(FNotifyUpdateCharacterStatsDelegate)
DECLARE_DELEGATE_OneParam(FErrorUpdateCharacterStatsDelegate, const FString&)

//Get Custom Character Data
DECLARE_DELEGATE_OneParam(FNotifyGetCustomCharacterDataDelegate, TSharedPtr<FJsonObject>)
DECLARE_DELEGATE_OneParam(FErrorGetCustomCharacterDataDelegate, const FString&)

//Add or Update Custom Character Data
DECLARE_DELEGATE(FNotifyAddOrUpdateCustomCharacterDataDelegate)
DECLARE_DELEGATE_OneParam(FErrorAddOrUpdateCustomCharacterDataDelegate, const FString&)

//Get Chat Groups for Player Delegates
DECLARE_DELEGATE_OneParam(FNotifyGetChatGroupsForPlayerDelegate, const TArray<FChatGroup>&)
DECLARE_DELEGATE_OneParam(FErrorGetChatGroupsForPlayerDelegate, const FString&)

//Add Ability To Character Delegates
DECLARE_DELEGATE(FNotifyAddAbilityToCharacterDelegate)
DECLARE_DELEGATE_OneParam(FErrorAddAbilityToCharacterDelegate, const FString&)

//Get Character Abilities Delegates
DECLARE_DELEGATE_OneParam(FNotifyGetCharacterAbilitiesDelegate, const TArray<FAbility>&)
DECLARE_DELEGATE_OneParam(FErrorGetCharacterAbilitiesDelegate, const FString&)

//Get Ability Bar Delegates
DECLARE_DELEGATE_OneParam(FNotifyGetAbilityBarsDelegate, const TArray<FAbilityBar>&)
DECLARE_DELEGATE_OneParam(FErrorGetAbilityBarsDelegate, const FString&)

//Get Ability Bars With Abilities Delegates
DECLARE_DELEGATE_OneParam(FNotifyGetAbilityBarsWithAbilitiesDelegate, const TArray<FAbilityBarWithAbilities>&)
DECLARE_DELEGATE_OneParam(FErrorGetAbilityBarsWithAbilitiesDelegate, const FString&)

//Update Ability On Character Delegates
DECLARE_DELEGATE(FNotifyUpdateAbilityOnCharacterDelegate)
DECLARE_DELEGATE_OneParam(FErrorUpdateAbilityOnCharacterDelegate, const FString&)

//Remove Ability From Character Delegates
DECLARE_DELEGATE(FNotifyRemoveAbilityFromCharacterDelegate)
DECLARE_DELEGATE_OneParam(FErrorRemoveAbilityFromCharacterDelegate, const FString&)

//Player Logout
DECLARE_DELEGATE(FNotifyPlayerLogoutDelegate)
DECLARE_DELEGATE_OneParam(FErrorPlayerLogoutDelegate, const FString&)

//Create Character
DECLARE_DELEGATE_OneParam(FNotifyCreateCharacterDelegate, const FCreateCharacter&)
DECLARE_DELEGATE_OneParam(FErrorCreateCharacterDelegate, const FString&)

//Remove Character
DECLARE_DELEGATE(FNotifyRemoveCharacterDelegate)
DECLARE_DELEGATE_OneParam(FErrorRemoveCharacterDelegate, const FString&)

//Get Player Groups Character is In
DECLARE_DELEGATE_OneParam(FNotifyGetPlayerGroupsCharacterIsInDelegate, const TArray<FPlayerGroup>&)
DECLARE_DELEGATE_OneParam(FErrorGetPlayerGroupsCharacterIsInDelegate, const FString&)

//Launch Zone Instance
DECLARE_DELEGATE_OneParam(FNotifyLaunchZoneInstanceDelegate, const FString&)
DECLARE_DELEGATE_OneParam(FErrorLaunchZoneInstanceDelegate, const FString&)


UCLASS( ClassGroup=(Custom), meta=(BlueprintSpawnableComponent) )
class OWSPLUGIN_API UOWSPlayerControllerComponent : public UActorComponent
{
	GENERATED_BODY()

	FHttpModule* Http;

public:	
	// Sets default values for this component's properties
	UOWSPlayerControllerComponent();

	UFUNCTION(BlueprintCallable, Category = "Player State")
		AOWSPlayerState* GetOWSPlayerState() const;

	//Travel methods
	UFUNCTION(BlueprintCallable, Category = "Travel")
		void TravelToMap(const FString& URL, const bool SeamlessTravel);

	UFUNCTION(BlueprintCallable, Category = "Travel")
		void TravelToMap2(const FString& ServerAndPort, const float X, const float Y, const float Z, const float RX, const float RY,
			const float RZ, const FString& PlayerName, const bool SeamlessTravel);

	//Set Selected Character And Connect to Last Zone
	UFUNCTION(BlueprintCallable, Category = "Travel")
		void SetSelectedCharacterAndConnectToLastZone(FString UserSessionGUID, FString SelectedCharacterName);
		void OnSetSelectedCharacterAndConnectToLastZoneResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	//Travel to Last Zone Server
		void TravelToLastZoneServer(FString CharacterName);
		void OnTravelToLastZoneServerResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	//Get Zone Server to Travel To
	UFUNCTION(BlueprintCallable, Category = "Travel")
		void GetZoneServerToTravelTo(FString CharacterName, TEnumAsByte<ERPGSchemeToChooseMap::SchemeToChooseMap> SelectedSchemeToChooseMap, int32 WorldServerID, FString ZoneName);

	void OnGetZoneServerToTravelToResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);
	FNotifyGetZoneServerToTravelToDelegate OnNotifyGetZoneServerToTravelToDelegate;
	FErrorGetZoneServerToTravelToDelegate OnErrorGetZoneServerToTravelToDelegate;

	//Save Player Location
	UFUNCTION(BlueprintCallable, Category = "Save")
		void SavePlayerLocation();

	void OnSavePlayerLocationResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	//Save All Player Data
	UFUNCTION(BlueprintCallable, Category = "Save")
		void SaveAllPlayerData();

	void OnSaveAllPlayerDataResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	//Get All Characters
	UFUNCTION(BlueprintCallable, Category = "Character")
		void GetAllCharacters(FString UserSessionGUID);

	void OnGetAllCharactersResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyGetAllCharactersDelegate OnNotifyGetAllCharactersDelegate;
	FErrorGetAllCharactersDelegate OnErrorGetAllCharactersDelegate;

	//Get Character Stats
	UFUNCTION(BlueprintCallable, Category = "Character")
		void GetCharacterStats(FString CharName);

	void OnGetCharacterStatsResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyGetCharacterStatsDelegate OnNotifyGetCharacterStatsDelegate;
	FErrorGetCharacterStatsDelegate OnErrorGetCharacterStatsDelegate;

	//Update Character Stats
	UFUNCTION(BlueprintCallable, Category = "Character")
		void UpdateCharacterStats(FString JSONString);

	void OnUpdateCharacterStatsResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyUpdateCharacterStatsDelegate OnNotifyUpdateCharacterStatsDelegate;
	FErrorUpdateCharacterStatsDelegate OnErrorUpdateCharacterStatsDelegate;

	//Get Custom Character Data
	UFUNCTION(BlueprintCallable, Category = "Character")
		void GetCustomCharacterData(FString CharName);

	void OnGetCustomCharacterDataResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyGetCustomCharacterDataDelegate OnNotifyGetCustomCharacterDataDelegate;
	FErrorGetCustomCharacterDataDelegate OnErrorGetCustomCharacterDataDelegate;

	//Add or Update Custom Character Data
	UFUNCTION(BlueprintCallable, Category = "Character")
		void AddOrUpdateCustomCharacterData(FString CharName, FString CustomFieldName, FString CustomValue);

	void OnAddOrUpdateCustomCharacterDataResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyAddOrUpdateCustomCharacterDataDelegate OnNotifyAddOrUpdateCustomCharacterDataDelegate;
	FErrorAddOrUpdateCustomCharacterDataDelegate OnErrorAddOrUpdateCustomCharacterDataDelegate;

	//Get Chat Groups for Player
	UFUNCTION(BlueprintCallable, Category = "Chat")
		void GetChatGroupsForPlayer();

	void OnGetChatGroupsForPlayerResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyGetChatGroupsForPlayerDelegate OnNotifyGetChatGroupsForPlayerDelegate;
	FErrorGetChatGroupsForPlayerDelegate OnErrorGetChatGroupsForPlayerDelegate;

	//Get Character Statuses
	UFUNCTION(BlueprintCallable, Category = "Character")
		void GetCharacterStatuses();

	void OnGetCharacterStatusesResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	//Add Ability To Character
	UFUNCTION(BlueprintCallable, Category = "Character")
		void AddAbilityToCharacter(FString CharName, FString AbilityName, int32 AbilityLevel, FString CustomJSON);

	void OnAddAbilityToCharacterResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyAddAbilityToCharacterDelegate OnNotifyAddAbilityToCharacterDelegate;
	FErrorAddAbilityToCharacterDelegate OnErrorAddAbilityToCharacterDelegate;	

	//Get Character Abilities
	UFUNCTION(BlueprintCallable, Category = "Character")
		void GetCharacterAbilities(FString CharName);

	void OnGetCharacterAbilitiesResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyGetCharacterAbilitiesDelegate OnNotifyGetCharacterAbilitiesDelegate;
	FErrorGetCharacterAbilitiesDelegate OnErrorGetCharacterAbilitiesDelegate;

	//Get Ability Bars
	UFUNCTION(BlueprintCallable, Category = "Character")
		void GetAbilityBars(FString CharName);

	void OnGetAbilityBarsResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyGetAbilityBarsDelegate OnNotifyGetAbilityBarsDelegate;
	FErrorGetAbilityBarsDelegate OnErrorGetAbilityBarsDelegate;

	//Update Ability On Character
	UFUNCTION(BlueprintCallable, Category = "Character")
		void UpdateAbilityOnCharacter(FString CharName, FString AbilityName, int32 AbilityLevel, FString CustomJSON);

	void OnUpdateAbilityOnCharacterResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyUpdateAbilityOnCharacterDelegate OnNotifyUpdateAbilityOnCharacterDelegate;
	FErrorUpdateAbilityOnCharacterDelegate OnErrorUpdateAbilityOnCharacterDelegate;

	//Remove Ability From Character
	UFUNCTION(BlueprintCallable, Category = "Character")
		void RemoveAbilityFromCharacter(FString CharName, FString AbilityName);

	void OnRemoveAbilityFromCharacterResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyRemoveAbilityFromCharacterDelegate OnNotifyRemoveAbilityFromCharacterDelegate;
	FErrorRemoveAbilityFromCharacterDelegate OnErrorRemoveAbilityFromCharacterDelegate;

	//Player Logout
	UFUNCTION(BlueprintCallable, Category = "Character")
		void PlayerLogout(FString CharName);

	void OnPlayerLogoutResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyPlayerLogoutDelegate OnNotifyPlayerLogoutDelegate;
	FErrorPlayerLogoutDelegate OnErrorPlayerLogoutDelegate;

	//Create Character
	UFUNCTION(BlueprintCallable, Category = "Character")
		void CreateCharacter(FString UserSessionGUID, FString CharacterName, FString ClassName);

	void OnCreateCharacterResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyCreateCharacterDelegate OnNotifyCreateCharacterDelegate;
	FErrorCreateCharacterDelegate OnErrorCreateCharacterDelegate;

	//Remove Character
	UFUNCTION(BlueprintCallable, Category = "Character")
		void RemoveCharacter(FString UserSessionGUID, FString CharacterName);

	void OnRemoveCharacterResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyRemoveCharacterDelegate OnNotifyRemoveCharacterDelegate;
	FErrorRemoveCharacterDelegate OnErrorRemoveCharacterDelegate;

	//Get Player Groups Character is In
	UFUNCTION(BlueprintCallable, Category = "Groups")
		void GetPlayerGroupsCharacterIsIn(FString UserSessionGUID, FString CharacterName, int32 PlayerGroupTypeID);

	void OnGetPlayerGroupsCharacterIsInResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyGetPlayerGroupsCharacterIsInDelegate OnNotifyGetPlayerGroupsCharacterIsInDelegate;
	FErrorGetPlayerGroupsCharacterIsInDelegate OnErrorGetPlayerGroupsCharacterIsInDelegate;

	//Launch Zone Instance
	void LaunchZoneInstance(FString CharacterName, FString ZoneName, ERPGPlayerGroupType::PlayerGroupType GroupType);

	void OnLaunchZoneInstanceResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyLaunchZoneInstanceDelegate OnNotifyLaunchZoneInstanceDelegate;
	FErrorLaunchZoneInstanceDelegate OnErrorLaunchZoneInstanceDelegate;

protected:
	// Called when the game starts
	virtual void BeginPlay() override;

	void ProcessOWS2POSTRequest(FString ApiModuleToCall, FString ApiToCall, FString PostParameters, void (UOWSPlayerControllerComponent::* InMethodPtr)(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful));
	void GetPlayerNameAndOWSCharacter(AOWSCharacter* OWSCharacter, FString& PlayerName);
	void GetJsonObjectFromResponse(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful, FString CallingMethodName, FString& ErrorMsg, TSharedPtr<FJsonObject>& JsonObject);

	template <typename T>
	TSharedPtr<T> GetStructFromJsonObject(TSharedPtr<FJsonObject> JsonObject)
	{
		TSharedPtr<T> Result = MakeShareable(new T);
		FJsonObjectConverter::JsonObjectToUStruct(JsonObject.ToSharedRef(), T::StaticStruct(), Result.Get(), 0, 0);
		return Result;
	}

	UPROPERTY(BlueprintReadWrite)
		FString OWSAPICustomerKey;

	UPROPERTY(BlueprintReadWrite, Category = "Config")
		FString OWS2APIPath = "";

	UPROPERTY(BlueprintReadWrite, Category = "Config")
		FString OWS2InstanceManagementAPIPath = "";

	UPROPERTY(BlueprintReadWrite, Category = "Config")
		FString OWS2CharacterPersistenceAPIPath = "";

	UPROPERTY(BlueprintReadWrite, Category = "Config")
		FString OWSEncryptionKey = "";

	UPROPERTY(BlueprintReadWrite)
		float TravelTimeout = 60.f;

	FString ServerTravelUserSessionGUID;
	FString ServerTravelCharacterName;
	float ServerTravelX;
	float ServerTravelY;
	float ServerTravelZ;
	float ServerTravelRX;
	float ServerTravelRY;
	float ServerTravelRZ;

};
