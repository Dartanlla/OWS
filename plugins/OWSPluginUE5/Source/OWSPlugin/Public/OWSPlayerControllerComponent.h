// Copyright 2020 Sabre Dart Studios

#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "Runtime/Online/HTTP/Public/Http.h"
#include "OWS2API.h"
#include "OWSPlayerState.h"
#include <JsonObjectConverter.h>
#include "OWSPlayerControllerComponent.generated.h"



//Get Zone Server To Travel To
DECLARE_DELEGATE_OneParam(FNotifyGetZoneServerToTravelToDelegate, const FString&)
DECLARE_DELEGATE_OneParam(FErrorGetZoneServerToTravelToDelegate, const FString&)

//Get All Characters Delegates
DECLARE_DELEGATE_OneParam(FNotifyGetAllCharactersDelegate, const TArray<FUserCharacter>&)
DECLARE_DELEGATE_OneParam(FErrorGetAllCharactersDelegate, const FString&)

//Get Character Stats
DECLARE_DELEGATE_OneParam(FNotifyGetCharacterStatsDelegate, const TArray<FUserCharacterStat>&)
DECLARE_DELEGATE_OneParam(FErrorGetCharacterStatsDelegate, const FString&)

//Get Character Inventory
DECLARE_DELEGATE_OneParam(FNotifyGetCharacterQuestsDelegate, const TArray<FUserCharacterQuest>&)
DECLARE_DELEGATE_OneParam(FErrorGetCharacterQuestsDelegate, const FString&)

//Get Character Inventory
DECLARE_DELEGATE_OneParam(FNotifyGetCharacterInventoryDelegate, const TArray<FUserCharacterInventory>&)
DECLARE_DELEGATE_OneParam(FErrorGetCharacterInventoryDelegate, const FString&)

//Get Character Abilities Delegates
DECLARE_DELEGATE_OneParam(FNotifyGetCharacterAbilitiesDelegate, const TArray<FUserCharacterAbility>&)
DECLARE_DELEGATE_OneParam(FErrorGetCharacterAbilitiesDelegate, const FString&)

//Get Character Data and Custom Data
DECLARE_DELEGATE_OneParam(FNotifyGetCharacterDataAndCustomDataDelegate, TSharedPtr<FJsonObject>)
DECLARE_DELEGATE_OneParam(FErrorGetCharacterDataAndCustomDataDelegate, const FString&)

//Update Character Stats
DECLARE_DELEGATE(FNotifyUpdateCharacterStatsDelegate)
DECLARE_DELEGATE_OneParam(FErrorUpdateCharacterStatsDelegate, const FString&)

//Update Character Quest
DECLARE_DELEGATE(FNotifyUpdateCharacterQuestDelegate)
DECLARE_DELEGATE_OneParam(FErrorUpdateCharacterQuestDelegate, const FString&)

//Update Character Inventory
DECLARE_DELEGATE(FNotifyUpdateCharacterInventoryDelegate)
DECLARE_DELEGATE_OneParam(FErrorUpdateCharacterInventoryDelegate, const FString&)

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

//TODO: Create Character Using Default Character Values
// DECLARE_DELEGATE(FNotifyCreateCharacterUsingDefaultCharacterValuesDelegate)
// DECLARE_DELEGATE_OneParam(FErrorCreateCharacterUsingDefaultCharacterValuesDelegate, const FString&)

//Remove Character
DECLARE_DELEGATE(FNotifyRemoveCharacterDelegate)
DECLARE_DELEGATE_OneParam(FErrorRemoveCharacterDelegate, const FString&)

//Get Player Groups Character is In
DECLARE_DELEGATE_OneParam(FNotifyGetPlayerGroupsCharacterIsInDelegate, const TArray<FPlayerGroup>&)
DECLARE_DELEGATE_OneParam(FErrorGetPlayerGroupsCharacterIsInDelegate, const FString&)

//Launch Zone Instance
DECLARE_DELEGATE_OneParam(FNotifyLaunchZoneInstanceDelegate, const FString&)
DECLARE_DELEGATE_OneParam(FErrorLaunchZoneInstanceDelegate, const FString&)

//Logout
DECLARE_DELEGATE(FNotifyLogoutDelegate)
DECLARE_DELEGATE_OneParam(FErrorLogoutDelegate, const FString&)


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

	// TODO: Save Player Location
	// UFUNCTION(BlueprintCallable, Category = "Save")
	// 	void SavePlayerLocation();

	// void OnSavePlayerLocationResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	// //Save All Player Data
	// UFUNCTION(BlueprintCallable, Category = "Save")
	// 	void SaveAllPlayerData();

	// void OnSaveAllPlayerDataResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

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

	//Get Character Quest
	UFUNCTION(BlueprintCallable, Category = "Character")
	void GetCharacterQuests(FString CharName);
	void OnGetCharacterQuestsResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyGetCharacterQuestsDelegate OnNotifyGetCharacterQuestsDelegate;
	FErrorGetCharacterQuestsDelegate OnErrorGetCharacterQuestsDelegate;

	//Get Character Inventory
	UFUNCTION(BlueprintCallable, Category = "Character")
	void GetCharacterInventory(FString CharName);
	void OnGetCharacterInventoryResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyGetCharacterInventoryDelegate OnNotifyGetCharacterInventoryDelegate;
	FErrorGetCharacterInventoryDelegate OnErrorGetCharacterInventoryDelegate;

	//Get Character Data and Custom Data
	UFUNCTION(BlueprintCallable, Category = "Character")
	void GetCharacterDataAndCustomData(FString UserSessionGUID, FString CharName);
	void OnGetCharacterDataAndCustomDataResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyGetCharacterDataAndCustomDataDelegate OnNotifyGetCharacterDataAndCustomDataDelegate;
	FErrorGetCharacterDataAndCustomDataDelegate OnErrorGetCharacterDataAndCustomDataDelegate;

	//Update Character Stats
	UFUNCTION(BlueprintCallable, Category = "Character")
	void UpdateCharacterStats(FString JSONString);
	void OnUpdateCharacterStatsResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyUpdateCharacterStatsDelegate OnNotifyUpdateCharacterStatsDelegate;
	FErrorUpdateCharacterStatsDelegate OnErrorUpdateCharacterStatsDelegate;

		//Update Ability On Character
	UFUNCTION(BlueprintCallable, Category = "Character")
	void UpdateCharacterAbilities(FString JSONString);
	void OnUpdateCharacterAbilitiesOnCharacterResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);
	FNotifyUpdateAbilityOnCharacterDelegate OnNotifyUpdateAbilityOnCharacterDelegate;
	FErrorUpdateAbilityOnCharacterDelegate OnErrorUpdateAbilityOnCharacterDelegate;

	UFUNCTION(BlueprintCallable, Category = "Character")
	void UpdateCharacterQuests(FString JSONString);
	void OnUpdateCharacterQuestsResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);
	FNotifyUpdateCharacterQuestDelegate OnNotifyUpdateCharacterQuestDelegate;
	FErrorUpdateCharacterQuestDelegate OnErrorUpdateCharacterQuestDelegate;

	UFUNCTION(BlueprintCallable, Category = "Character")
	void UpdateCharacterInventory(FString JSONString);
	void OnUpdateCharacterInventoryResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyUpdateCharacterInventoryDelegate OnNotifyUpdateCharacterInventoryDelegate;
	FErrorUpdateCharacterInventoryDelegate OnErrorUpdateCharacterInventoryDelegate;

	//Get Character Abilities
	UFUNCTION(BlueprintCallable, Category = "Character")
	void GetCharacterAbilities(FString CharName);
	void OnGetCharacterAbilitiesResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyGetCharacterAbilitiesDelegate OnNotifyGetCharacterAbilitiesDelegate;
	FErrorGetCharacterAbilitiesDelegate OnErrorGetCharacterAbilitiesDelegate;

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

	//Add Ability To Character
	UFUNCTION(BlueprintCallable, Category = "Character")
	void AddAbilityToCharacter(FString CharName, FString AbilityName, int32 AbilityLevel, FString CustomJSON);
	void OnAddAbilityToCharacterResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyAddAbilityToCharacterDelegate OnNotifyAddAbilityToCharacterDelegate;
	FErrorAddAbilityToCharacterDelegate OnErrorAddAbilityToCharacterDelegate;	

	//Get Ability Bars
	UFUNCTION(BlueprintCallable, Category = "Character")
	void GetAbilityBars(FString CharName);
	void OnGetAbilityBarsResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyGetAbilityBarsDelegate OnNotifyGetAbilityBarsDelegate;
	FErrorGetAbilityBarsDelegate OnErrorGetAbilityBarsDelegate;

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

	// TODO: Create Character Using Default Character Values
	// UFUNCTION(BlueprintCallable, Category = "Character")
	// 	void CreateCharacterUsingDefaultCharacterValues(FString UserSessionGUID, FString CharacterName, FString DefaultSetName);

	// void CreateCharacterUsingDefaultCharacterValuesSuccess();
	// void CreateCharacterUsingDefaultCharacterValuesError(const FString& ErrorMsg);

	// FNotifyCreateCharacterUsingDefaultCharacterValuesDelegate OnNotifyCreateCharacterUsingDefaultCharacterValuesDelegate;

	//Remove Character
	UFUNCTION(BlueprintCallable, Category = "Character")
	void RemoveCharacter(FString UserSessionGUID, FString CharacterName);
	void OnRemoveCharacterResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyRemoveCharacterDelegate OnNotifyRemoveCharacterDelegate;
	FErrorRemoveCharacterDelegate OnErrorRemoveCharacterDelegate;

	//Launch Zone Instance
	void LaunchZoneInstance(FString CharacterName, FString ZoneName, ERPGPlayerGroupType::PlayerGroupType GroupType);
	void OnLaunchZoneInstanceResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyLaunchZoneInstanceDelegate OnNotifyLaunchZoneInstanceDelegate;
	FErrorLaunchZoneInstanceDelegate OnErrorLaunchZoneInstanceDelegate;


	void AddQuestListToDatabase(FString JSONString);
	void OnAddQuestListToDatabaseResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	// TODO: Lougout
	// void Logout(FString UserSessionGUID);

	// void LogoutSuccess();
	// void LogoutError(const FString& ErrorMsg);

	// FNotifyLogoutDelegate OnNotifyLogoutDelegate;
	// FErrorLogoutDelegate OnErrorLogoutDelegate;

protected:
	// Called when the game starts
	virtual void BeginPlay() override;

	void ProcessOWS2POSTRequest(FString ApiModuleToCall, FString ApiToCall, FString PostParameters, void (UOWSPlayerControllerComponent::* InMethodPtr)(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful));
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
