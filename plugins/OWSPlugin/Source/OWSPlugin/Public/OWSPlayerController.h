// Copyright 2021 Sabre Dart Studios

#pragma once

#include "GameFramework/PlayerController.h"
#include "Runtime/Online/HTTP/Public/Http.h"
//#include "OWSAPIStructs.h"
#include "OWSCharacterWithAbilities.h"
#include "OWSPlayerState.h"
#include "OWSReplicationGraph.h"
#include "OWSPlayerControllerComponent.h"
#include "OWSPlayerController.generated.h"





USTRUCT(BlueprintType, Blueprintable)
struct FChatGroup
{
	GENERATED_USTRUCT_BODY()

	FChatGroup() {
		ChatGroupID = 0;
		ChatGroupName = "";
	}

	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Chat")
		int32 ChatGroupID;
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Chat")
		FString ChatGroupName;
};







/**
 * 
 */
UCLASS()
class OWSPLUGIN_API AOWSPlayerController : public APlayerController
{
	GENERATED_BODY()

	FHttpModule* Http;

public:

	AOWSPlayerController();

	UPROPERTY(BlueprintReadOnly)
		UOWSPlayerControllerComponent* OWSPlayerControllerComponent;

	UPROPERTY(BlueprintReadWrite)
		FString RPGAPICustomerKey;

	UPROPERTY(BlueprintReadWrite, Category = "Config")
		FString OWS2APIPath = "";

	UPROPERTY(BlueprintReadWrite, Category = "Config")
		FString OWSEncryptionKey = "";

	AOWSGameMode* GetGameMode();

	UFUNCTION(BlueprintCallable, Category = "Travel")
		void SetSelectedCharacterAndConnectToLastZone(FString UserSessionGUID, FString SelectedCharacterName);

		void TravelToLastZoneServer(FString CharacterName);

		FString ServerTravelUserSessionGUID;
		FString ServerTravelCharacterName;
		float ServerTravelX;
		float ServerTravelY;
		float ServerTravelZ;
		float ServerTravelRX;
		float ServerTravelRY;
		float ServerTravelRZ;

	FVector LastCharacterLocation;
	FRotator LastCharacterRotation;

	UPROPERTY()
		TMap<FString, int32> LocalMeshItemsMap;

	UFUNCTION(BlueprintCallable, Category = "Inventory")
		void SynchUpLocalMeshItemsMap();

	UFUNCTION()
		void AddItemToLocalMeshItemsMap(const FString& ItemName, const int32 ItemMeshID);

	UFUNCTION(Client, Reliable)
		void Client_AddItemToLocalMeshItemsMap(const FString& ItemName, const int32 ItemMeshID);

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Selection")
		AOWSCharacter* SelectedCharacter;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Groups")
		TArray<FPlayerGroup> PlayerGroupsPlayerIsIn;

	UFUNCTION(BlueprintCallable, Category = "Player State")
		AOWSPlayerState* GetOWSPlayerState() const;

	UFUNCTION(BlueprintCallable, Category = "Replication")
		UOWSReplicationGraph* GetReplicationGraph() const;

	UFUNCTION(BlueprintCallable, Category = "Travel")
		void TravelToMap(const FString& URL, const bool SeamlessTravel);

	UFUNCTION(BlueprintCallable, Category = "Travel")
		void TravelToMap2(const FString& ServerAndPort, const float X, const float Y, const float Z, const float RX, const float RY,
			const float RZ, const FString& PlayerName, const bool SeamlessTravel);

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Connection")
		float TravelTimeout;

	virtual void GetLifetimeReplicatedProps(TArray< FLifetimeProperty > & OutLifetimeProps) const override;

	//Predicted projectiles
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Projectiles")
		TArray<class AOWSAdvancedProjectile*> FakeProjectiles;

	/** Return amount of time to tick or simulate to make up for network lag */
	UFUNCTION(BlueprintCallable, Category = "Prediction")
	virtual float GetPredictionTime();

	virtual float GetProjectileSleepTime();

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Replicated, Category = "Projectiles")
		float MaxPredictionPing;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Projectiles")
		float DesiredPredictionPing;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Replicated, Category = "Projectiles")
		float PredictionFudgeFactor;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Projectiles")
		bool bIsDebuggingProjectiles;

	UFUNCTION(BlueprintCallable, Category = "Utility")
		UTexture2D* LoadTextureReference(const FString& TexturePath);

	UFUNCTION(BlueprintCallable, Category = "Selection")
		void SetSelectedCharacter(AOWSCharacter* RPGCharacter);
	UFUNCTION(BlueprintCallable, Category = "Selection")
		void ClearSelectedCharacter();
	UFUNCTION(BlueprintCallable, Category = "Selection")
		void ClearSelectionOnCharacter(AOWSCharacter* RPGCharacter);
	
	UFUNCTION(BlueprintCallable, Category = "Save")
		void SavePlayerLocation();

	void OnSavePlayerLocationResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	UFUNCTION(BlueprintCallable, Category = "Save")
		void SaveAllPlayerData();

	void OnSaveAllPlayerDataResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	void PawnLeavingGame();

	//bool InputKey(FKey Key, EInputEvent EventType, float AmountDepressed, bool bGamepad) override;
	bool InputAxis(FKey Key, float Delta, float DeltaTime, int32 NumSamples, bool bGamepad) override;

	UFUNCTION(BlueprintImplementableEvent, Category = "Save")
		void NotifyPawnLeavingGame(const AOWSCharacter* RPGCharacter);

	//Add Player to Group
	UFUNCTION(BlueprintCallable, Category = "Groups")
		void AddPlayerToGroup(FString PlayerGroupName, ERPGPlayerGroupType::PlayerGroupType GroupType, FString CharacterNameToAdd);

	void OnAddPlayerToGroupResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	UFUNCTION(BlueprintImplementableEvent, Category = "Groups")
		void NotifyAddPlayerToGroup(const FString& CharacterNameAddedToGroup);

	//Remove Player from Group
	UFUNCTION(BlueprintCallable, Category = "Groups")
		void RemovePlayerFromGroup(FString PlayerGroupName, FString CharacterNameToRemove);

	void OnRemovePlayerFromGroupResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	UFUNCTION(BlueprintImplementableEvent, Category = "Groups")
		void NotifyRemovePlayerFromGroup(const FString& CharacterNameAddedToGroup);

	//Launch Dungeon
	UFUNCTION(BlueprintCallable, Category = "Travel")
		void LaunchDungeon(FString MapName, ERPGPlayerGroupType::PlayerGroupType GroupType);

	UFUNCTION(BlueprintImplementableEvent, Category = "Travel")
		void NotifyLaunchDungeon(const FString &ServerAndPort);
	UFUNCTION(BlueprintImplementableEvent, Category = "Travel")
		void ErrorLaunchDungeon(const FString &ErrorMsg);

	//Player Logout
	UFUNCTION(BlueprintCallable, Category = "Player")
		void PlayerLogout();

	UFUNCTION(BlueprintImplementableEvent, Category = "Player")
		void NotifyPlayerLogout();
	UFUNCTION(BlueprintImplementableEvent, Category = "Player")
		void ErrorPlayerLogout(const FString &ErrorMsg);

	//Get Chat Groups for Player
	UFUNCTION(BlueprintCallable, Category = "Chat")
		void GetChatGroupsForPlayer();

	UFUNCTION(BlueprintImplementableEvent, Category = "Chat")
		void NotifyGetChatGroupsForPlayer(const TArray<FChatGroup> &ChatGroups);
	UFUNCTION(BlueprintImplementableEvent, Category = "Chat")
		void ErrorGetChatGroupsForPlayer(const FString &ErrorMsg);

	//Is Player Online?
	UFUNCTION(BlueprintCallable, Category = "Chat")
		void IsPlayerOnline(FString PlayerName);

	void OnIsPlayerOnlineResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	UFUNCTION(BlueprintImplementableEvent, Category = "Chat")
		void NotifyIsPlayerOnline(const bool PlayerIsOnline);
	UFUNCTION(BlueprintImplementableEvent, Category = "Chat")
		void ErrorIsPlayerOnline(const FString &ErrorMsg);

	//Get All Characters for UserSessionGUID
	UFUNCTION(BlueprintCallable, Category = "Login")
		void GetAllCharacters(FString UserSessionGUID);

	UFUNCTION(BlueprintImplementableEvent, Category = "Login")
		void NotifyGetAllCharacters(const TArray<FUserCharacter> &UserCharacters);

	UFUNCTION(BlueprintImplementableEvent, Category = "Login")
		void ErrorGetAllCharacters(const FString &ErrorMsg);

	//Create Character
	UFUNCTION(BlueprintCallable, Category = "Login")
		void CreateCharacter(FString UserSessionGUID, FString CharacterName, FString ClassName);

	UFUNCTION(BlueprintImplementableEvent, Category = "Login")
		void NotifyCreateCharacter(const FCreateCharacter &CreateCharacterData);

	UFUNCTION(BlueprintImplementableEvent, Category = "Login")
		void ErrorCreateCharacter(const FString &ErrorMsg);

	//Get Cosmetic Character Custom Data
	UFUNCTION(BlueprintCallable, Category = "Stats")
		void GetCosmeticCustomCharacterData(FString UserSessionGUID, FString CharacterName);

	void OnGetCosmeticCustomCharacterDataResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	UFUNCTION(BlueprintImplementableEvent, Category = "Stats")
		void NotifyGetCosmeticCustomCharacterData(const TArray<FCustomCharacterDataStruct> &CustomCharacterData);
	UFUNCTION(BlueprintImplementableEvent, Category = "Stats")
		void ErrorGetCosmeticCustomCharacterData(const FString &ErrorMsg);

	//Add Cosmetic Character Custom Data
	UFUNCTION(BlueprintCallable, Category = "Stats")
		void AddOrUpdateCosmeticCustomCharacterData(FString UserSessionGUID, FString CharacterName, FString CustomFieldName, FString CustomValue);

	void OnAddOrUpdateCosmeticCustomCharacterDataResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	//Get Player Groups Character Is In
	UFUNCTION(BlueprintCallable, Category = "Player Groups")
		void GetPlayerGroupsCharacterIsIn(FString UserSessionGUID, FString CharacterName, int32 PlayerGroupTypeID);

	UFUNCTION(BlueprintImplementableEvent, Category = "Player Groups")
		void NotifyGetPlayerGroupsCharacterIsIn(const TArray<FPlayerGroup> &PlayerGroups);
	UFUNCTION(BlueprintImplementableEvent, Category = "Player Groups")
		void ErrorGetPlayerGroupsCharacterIsIn(const FString &ErrorMsg);

	//Get Map Server to Travel To
	UFUNCTION(BlueprintCallable, Category = "Travel")
		void GetMapServerToTravelTo(FString ZoneName);

	UFUNCTION(BlueprintImplementableEvent, Category = "Travel")
		void NotifyMapServerToTravelTo(const FString &ServerAndPort);
	UFUNCTION(BlueprintImplementableEvent, Category = "Travel")
		void ErrorMapServerToTravelTo(const FString &ErrorMsg);

	//RemoveCharacter
	UFUNCTION(BlueprintCallable, Category = "Player")
		void RemoveCharacter(FString UserSessionGUID, FString CharacterName);

	UFUNCTION(BlueprintImplementableEvent, Category = "Player")
		void NotifyRemoveCharacter();
	UFUNCTION(BlueprintImplementableEvent, Category = "Player")
		void ErrorRemoveCharacter(const FString &ErrorMsg);

	//Add Ability to Character Events
	UFUNCTION(BlueprintImplementableEvent, Category = "Abilities")
		void NotifyAddAbilityToCharacter();
	UFUNCTION(BlueprintImplementableEvent, Category = "Abilities")
		void ErrorAddAbilityToCharacter(const FString& ErrorMsg);

	//Update Ability on Character Events
	UFUNCTION(BlueprintImplementableEvent, Category = "Abilities")
		void NotifyUpdateAbilityOnCharacter();
	UFUNCTION(BlueprintImplementableEvent, Category = "Abilities")
		void ErrorUpdateAbilityOnCharacter(const FString& ErrorMsg);

	//Remove Ability from Character Events
	UFUNCTION(BlueprintImplementableEvent, Category = "Abilities")
		void NotifyRemoveAbilityFromCharacter();
	UFUNCTION(BlueprintImplementableEvent, Category = "Abilities")
		void ErrorRemoveAbilityFromCharacter(const FString& ErrorMsg);


	//Player Controller Component Delegate Bindings
	void NotifyGetCharacterStats(TSharedPtr<FJsonObject> JsonObject);
	void NotifyUpdateCharacterStats();
	void NotifyGetCustomCharacterData(TSharedPtr<FJsonObject> JsonObject);
	void ErrorCustomCharacterData(const FString& ErrorMsg);
	void NotifyGetCharacterAbilities(const TArray<FAbility>& Abilities);
	void ErrorGetCharacterAbilities(const FString& ErrorMsg);
	void NotifyGetAbilityBars(const TArray<FAbilityBar>& AbilityBars);
	void ErrorGetAbilityBars(const FString& ErrorMsg);
};
