// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "OWSCharacterBase.h"
#include "GameFramework/Character.h"
#include "Runtime/Online/HTTP/Public/Http.h"
#include "Runtime/JsonUtilities/Public/JsonObjectConverter.h"
#include "Runtime/Core/Public/Misc/Guid.h"
#include "GenericTeamAgentInterface.h"
#include "OWS2API.h"
#include "OWSInventory.h"
#include "OWSCharacter.generated.h"

class AOWSGameMode;

USTRUCT(BlueprintType)
struct FCustomCharacterDataStruct
{
	GENERATED_BODY()

public:
	FCustomCharacterDataStruct() {
		CustomFieldName = "";
		FieldValue = "";
	}

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Custom Character Data Struct")
	FString CustomFieldName;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Custom Character Data Struct")
	FString FieldValue;

};

USTRUCT(BlueprintType)
struct FCustomInventoryItemDataStruct
{
	GENERATED_BODY()

public:
	FCustomInventoryItemDataStruct() {
		CustomFieldName = "";
		FieldValue = "";
	}

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory Data Struct")
		FString CustomFieldName;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory Data Struct")
		FString FieldValue;

};

USTRUCT()
struct FCharacterStats
{
	GENERATED_BODY()

public:
	FCharacterStats() {
		CharName = "";
		CharacterLevel = 0;
		Gender = 0;
		Weight = 0.f;
		Size = 0;
		Fame = 0.f;
		Alignment = 0.f;
		Description = "";
		XP = 0;
		X = 0.f;
		Y = 0.f;
		Z = 0.f;
		RX = 0.f;
		RY = 0.f;
		RZ = 0.f;
		TeamNumber = 0;
		HitDie = 0;
		Wounds = 0.f;
		Thirst = 0.f;
		Hunger = 0.f;
		MaxHealth = 0.f;
		Health = 0.f;
		HealthRegenRate = 0.f;
		MaxMana = 0.f;
		Mana = 0.f;
		ManaRegenRate = 0.f;
		MaxEnergy = 0.f;
		Energy = 0.f;
		EnergyRegenRate = 0.f;
		MaxFatigue = 0.f;
		Fatigue = 0.f;
		FatigueRegenRate = 0.f;
		MaxStamina = 0.f;
		Stamina = 0.f;
		StaminaRegenRate = 0.f;
		MaxEndurance = 0.f;
		Endurance = 0.f;
		EnduranceRegenRate = 0.f;
		Strength = 0.f;
		Dexterity = 0.f;
		Constitution = 0.f;
		Intellect = 0.f;
		Wisdom = 0.f;
		Charisma = 0.f;
		Agility = 0.f;
		Spirit = 0.f;
		Magic = 0.f;
		Fortitude = 0.f;
		Reflex = 0.f;
		Willpower = 0.f;
		BaseAttack = 0.f;
		BaseAttackBonus = 0.f;
		AttackPower = 0.f;
		AttackSpeed = 0.f;
		CritChance = 0.f;
		CritMultiplier = 0.f;
		Haste = 0.f;
		SpellPower = 0.f;
		SpellPenetration = 0.f;
		Defense = 0.f;
		Dodge = 0.f;
		Parry = 0.f;
		Avoidance = 0.f;
		Versatility = 0.f;
		Multishot = 0.f;
		Initiative = 0.f;
		NaturalArmor = 0.f;
		PhysicalArmor = 0.f;
		BonusArmor = 0.f;
		ForceArmor = 0.f;
		MagicArmor = 0.f;
		Resistance = 0.f;
		ReloadSpeed = 0.f;
		Range = 0.f;
		Speed = 0.f;
		Gold = 0;
		Silver = 0;
		Copper = 0;
		FreeCurrency = 0;
		PremiumCurrency = 0;
		Perception = 0;
		Acrobatics = 0;
		Climb = 0;
		Stealth = 0;
		Score = 0;
		CustomerGUID = "";
	}

	UPROPERTY()
		FString CharName;
	UPROPERTY()
		int32 CharacterLevel;
	UPROPERTY()
		int32 Gender;
	UPROPERTY()
		float Weight;
	UPROPERTY()
		int32 Size;
	UPROPERTY()
		float Fame;
	UPROPERTY()
		float Alignment;
	UPROPERTY()
		FString Description;
	UPROPERTY()
		int XP;
	UPROPERTY()
		float X;
	UPROPERTY()
		float Y;
	UPROPERTY()
		float Z;
	UPROPERTY()
		float RX;
	UPROPERTY()
		float RY;
	UPROPERTY()
		float RZ;
	UPROPERTY()
		int TeamNumber;
	UPROPERTY()
		int HitDie;
	UPROPERTY()
		float Wounds;
	UPROPERTY()
		float Thirst;
	UPROPERTY()
		float Hunger;
	UPROPERTY()
		float MaxHealth;
	UPROPERTY()
		float Health;
	UPROPERTY()
		float HealthRegenRate;
	UPROPERTY()
		float MaxMana;
	UPROPERTY()
		float Mana;
	UPROPERTY()
		float ManaRegenRate;
	UPROPERTY()
		float MaxEnergy;
	UPROPERTY()
		float Energy;
	UPROPERTY()
		float EnergyRegenRate;
	UPROPERTY()
		float MaxFatigue;
	UPROPERTY()
		float Fatigue;
	UPROPERTY()
		float FatigueRegenRate;
	UPROPERTY()
		float MaxStamina;
	UPROPERTY()
		float Stamina;
	UPROPERTY()
		float StaminaRegenRate;
	UPROPERTY()
		float MaxEndurance;
	UPROPERTY()
		float Endurance;
	UPROPERTY()
		float EnduranceRegenRate;
	UPROPERTY()
		float Strength;
	UPROPERTY()
		float Dexterity;
	UPROPERTY()
		float Constitution;
	UPROPERTY()
		float Intellect;
	UPROPERTY()
		float Wisdom;
	UPROPERTY()
		float Charisma;
	UPROPERTY()
		float Agility;
	UPROPERTY()
		float Spirit;
	UPROPERTY()
		float Magic;
	UPROPERTY()
		float Fortitude;
	UPROPERTY()
		float Reflex;
	UPROPERTY()
		float Willpower;
	UPROPERTY()
		float BaseAttack;
	UPROPERTY()
		float BaseAttackBonus;
	UPROPERTY()
		float AttackPower;
	UPROPERTY()
		float AttackSpeed;
	UPROPERTY()
		float CritChance;
	UPROPERTY()
		float CritMultiplier;
	UPROPERTY()
		float Haste;
	UPROPERTY()
		float SpellPower;
	UPROPERTY()
		float SpellPenetration;
	UPROPERTY()
		float Defense;
	UPROPERTY()
		float Dodge;
	UPROPERTY()
		float Parry;
	UPROPERTY()
		float Avoidance;
	UPROPERTY()
		float Versatility;
	UPROPERTY()
		float Multishot;
	UPROPERTY()
		float Initiative;
	UPROPERTY()
		float NaturalArmor;
	UPROPERTY()
		float PhysicalArmor;
	UPROPERTY()
		float BonusArmor;
	UPROPERTY()
		float ForceArmor;
	UPROPERTY()
		float MagicArmor;
	UPROPERTY()
		float Resistance;
	UPROPERTY()
		float ReloadSpeed;
	UPROPERTY()
		float Range;
	UPROPERTY()
		float Speed;
	UPROPERTY()
		int Gold;
	UPROPERTY()
		int Silver;
	UPROPERTY()
		int Copper;
	UPROPERTY()
		int FreeCurrency;
	UPROPERTY()
		int PremiumCurrency;
	UPROPERTY()
		float Perception;
	UPROPERTY()
		float Acrobatics;
	UPROPERTY()
		float Climb;
	UPROPERTY()
		float Stealth;
	UPROPERTY()
		int Score;
	UPROPERTY()
		FString CustomerGUID;

};

USTRUCT()
struct FUpdateCharacterStatsJSONPost
{
	GENERATED_BODY()

public:
	UPROPERTY()
		FCharacterStats UpdateCharacterStats;
};

UCLASS()
class OWSPLUGIN_API AOWSCharacter : public AOWSCharacterBase, public IGenericTeamAgentInterface
{
	GENERATED_BODY()

private:
	FGenericTeamId TeamId;

	virtual FGenericTeamId GetGenericTeamId() const override;

protected:
	FHttpModule* Http;

public:
	UPROPERTY(BlueprintReadWrite)
		FString OWSAPICustomerKey;

	UPROPERTY(BlueprintReadWrite, Category = "Input")
		bool bDisableMouseRotation;

	/*
	UPROPERTY(BlueprintReadWrite)
		int32 AlwaysRelevantPartyID;
	*/

	bool CanJumpInternal_Implementation() const;

	UFUNCTION(BlueprintCallable, Category = "Interaction")
		bool IsCharacterFacingActor(AActor* ActorToTest, float MaxAngle);
	UFUNCTION(BlueprintCallable, Category = "Interaction")
		bool IsCharacterFacingActorForward(AActor* ActorToTest, float MaxAngle);

	UFUNCTION(BlueprintCallable, Category = "Player Controller")
		AOWSPlayerController* GetOWSPlayerController();

	//Mob
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Mob)
		bool IsAMob;

	// Sets default values for this character's properties
	AOWSCharacter(const class FObjectInitializer& ObjectInitializer);

	// Called when the game starts or when spawned
	virtual void BeginPlay() override;
	
	// Called every frame
	virtual void Tick( float DeltaSeconds ) override;

	// Called to bind functionality to input
	virtual void SetupPlayerInputComponent(class UInputComponent* PlayerInputComponent) override;

	FTimerHandle OnRPGInitalizationCompleteTimerHandle;

	virtual void PossessedBy(AController* NewController) override;

	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = "Init")
		void OnRPGInitalizationComplete();

	//Get Character Stats
	UFUNCTION(BlueprintCallable, Category = "Stats")
		void GetCharacterStatsBase();	

	//void OnGetCharacterStatsBaseResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	virtual void LoadCharacterStats();
	virtual void LoadCharacterStatsFromJSON(TSharedPtr<FJsonObject> JsonObject);

	//Update Character Stats
	UFUNCTION(BlueprintCallable, Category = "Stats")
		void UpdateCharacterStatsBase();

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Stats")
		bool bShouldAutoLoadCustomCharacterStats;

	void AutoLoadCustomCharacterStats();

	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = "Stats")
		void LoadCustomCharacterStats();

	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = "Stats")
		void UpdateCharacterStatsAfterLoading();

	/*
	//Get Character Statuses
	UFUNCTION(BlueprintCallable, Category = "Stats")
		void GetCharacterStatuses();

	void OnGetCharacterStatusesResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = "Stats")
		void NotifyGetCharacterStatuses();
	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = "Stats")
		void ErrorGetCharacterStatuses(const FString &ErrorMsg);

	//Add or Update Character Status
	UFUNCTION(BlueprintCallable, Category = "Stats")
		void AddOrUpdateCharacterStatus(FString StatusName, int32 StatusValue, int32 StatusDurationMinutes);

	void OnAddOrUpdateCharacterStatusResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);
	*/

	//Get Character Custom Data
	UFUNCTION(BlueprintCallable, Category = "Stats")
		void GetCustomCharacterData();

	void ProcessCustomCharacterData(TSharedPtr<FJsonObject> JsonObject);

	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = "Stats")
		void NotifyGetCustomCharacterData(const TArray<FCustomCharacterDataStruct> &CustomCharacterData);
	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = "Stats")
		void ErrorGetCustomCharacterData(const FString &ErrorMsg);

	//Add Character Custom Data
	UFUNCTION(BlueprintCallable, Category = "Stats")
		void AddOrUpdateCustomCharacterData(FString CustomFieldName, FString CustomValue);

	void OnAddOrUpdateCustomCharacterDataResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	//Get Game Mode
	UFUNCTION(BlueprintCallable, Category = "Game Mode")
		AOWSGameMode* GetGameMode();

	//HUD Inventory System
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		bool bShouldAutoLoadInventoriesToManage;

	UFUNCTION()
		void OnRep_InventoriesToManage();

	UPROPERTY(Transient)
		TArray<FInventoryItemStruct> LocalInventoryItems;

	//UPROPERTY(Transient, ReplicatedUsing = OnRep_InventoriesToManage)
	UPROPERTY(Transient)
		TArray<UOWSInventory*> InventoriesToManage;

	UFUNCTION(BlueprintCallable, Category = "Inventory")
		UOWSInventory* CreateHUDInventory(FName InventoryName, int32 Size, int32 NumberOfColumns);

	bool AddItemToLocalInventoryItems(const FString& ItemName, const bool ItemCanStack, const bool IsUsable, const bool IsConsumedOnUse, const int32 ItemTypeID,
		const FString& TextureToUseForIcon, const int32 IconSlotWidth, const int32 IconSlotHeight, const int32 ItemMeshID, const FString& CustomData);

	UFUNCTION(Client, Reliable)
		void Client_AddItemToLocalInventoryItems(const FString& ItemName, const bool ItemCanStack, const bool IsUsable, const bool IsConsumedOnUse, const int32 ItemTypeID, 
			const FString& TextureToUseForIcon, const int32 IconSlotWidth, const int32 IconSlotHeight, const int32 ItemMeshID, const FString& CustomData);

	UFUNCTION(Client, Reliable)
		void Client_CreateHUDInventory(FName InventoryName, int32 Size, int32 NumberOfColumns);

	UFUNCTION(Client, Reliable)
		void Client_AddItemToInventory(const FName& InventoryName, const FString& ItemName, const int32 StackSize, const int32 InSlotNumber, const int32 NumberOfUsesLeft, const int32 Condition,
			const FString& PerInstanceCustomData, const FGuid UniqueItemGUID, const int32 ItemMeshID);

	UFUNCTION(BlueprintCallable, Category = "Inventory")
		UOWSInventory* GetHUDInventoryFromName(FName InventoryName);

	FString SerializeInventory(FName InventoryName);

	//Get Character Inventory
	UFUNCTION(BlueprintCallable, Category = "Inventory")
		void GetInventoryItems(FString InventoryName);

	void OnGetInventoryItemsResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	virtual void ReadInventoryItems(const TArray<TSharedPtr<FJsonValue>> Rows, FName& InventoryName, int32& InventorySize, TArray<FInventoryItemStruct>& InventoryItems);

	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = "Inventory")
		void GetInventoryItemsComplete(const FString &InventoryName, const TArray<FInventoryItemStruct>& InventoryItems, int32 InventorySize);
	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = "Inventory")
		void GetInventoryItemsError(const FString &ErrorMsg);

	//Add Item to Inventory
	UFUNCTION(BlueprintCallable, Category = "Inventory")
		void AddItemToInventory(FString InventoryName, FString ItemName, int InSlotNumber, int Quantity, int NumberOfUsesLeft, int Condition, FGuid &UniqueItemGUID);

	void OnAddItemToInventoryResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = "Inventory")
		void AddItemToInventoryComplete();
	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = "Inventory")
		void AddItemToInventoryError();

	//Add Item to Inventory with Custom Data
	UFUNCTION(BlueprintCallable, Category = "Inventory")
		void AddItemToInventoryWithCustomData(FString InventoryName, FString ItemName, int InSlotNumber, int Quantity, int NumberOfUsesLeft, int Condition, FString CustomData, FGuid &UniqueItemGUID);

	void OnAddItemToInventoryWithCustomDataResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = "Inventory")
		void AddItemToInventoryWithCustomDataComplete();
	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = "Inventory")
		void AddItemToInventoryWithCustomDataError();

	//Remove Item from Inventory
	UFUNCTION(BlueprintCallable, Category = "Inventory")
		void RemoveItemFromInventory(FGuid UniqueItemGUID);

	void OnRemoveItemFromInventoryResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	//Save Inventory
	UFUNCTION(BlueprintCallable, Category = "Inventory")
		void SaveInventory(FString InventoryName, FString InventoryData);

	void OnSaveInventoryResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	//Serialize and Save Inventory
	UFUNCTION(BlueprintCallable, Category = "Inventory")
		void SerializeAndSaveInventory(FName InventoryName);

	void OnSerializeAndSaveInventoryResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	//Create Inventory
	UFUNCTION(BlueprintCallable, Category = "Inventory")
		void CreateInventory(FString InventoryName, int InventorySize);

	void OnCreateInventoryResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = "Inventory")
		void CreateInventoryComplete();
	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = "Inventory")
		void CreateInventoryError(const FString &ErrorMsg);


	/***** Abilities *****/

	//Get Character Abilities
	UFUNCTION(BlueprintCallable, Category = "Abilities")
		void GetCharacterAbilities();

	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = "Abilities")
		void GetCharacterAbilitiesComplete(const TArray<FAbility> &Abilities);
	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = "Abilities")
		void GetCharacterAbilitiesError(const FString &ErrorMsg);

	//Get Ability Bars
	UFUNCTION(BlueprintCallable, Category = "Abilities")
		void GetAbilityBars();

	void OnGetAbilityBarsResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = "Abilities")
		void GetAbilityBarsComplete(const TArray<FAbilityBar> &AbilityBars);
	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = "Abilities")
		void GetAbilityBarsError(const FString &ErrorMsg);

	//Get Ability Bar Abilities
	UFUNCTION(BlueprintCallable, Category = "Abilities")
		void GetAbilityBarsAndAbilities();

	void OnGetAbilityBarsAndAbilitiesResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = "Abilities")
		void GetAbilityBarsAndAbilitiesComplete(const TArray<FAbilityBarWithAbilities> &AbilityBars);
	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = "Abilities")
		void GetAbilityBarsAndAbilitiesError(const FString &ErrorMsg);


	UFUNCTION(BlueprintCallable, Category = "Inventory")
		void ParseInventoryCustomData(FString InventoryCustomData, TArray<FCustomInventoryItemDataStruct> &ItemCustomDataArray);

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterAttributes", Replicated)
		FString CharacterName;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterAttributes", Replicated)
		FString ClassName;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterAttributes", Replicated)
		int32 Gender;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterAttributes", Replicated)
		bool IsAdmin;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterAttributes", Replicated)
		bool IsModerator;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterAttributes", Replicated)
		bool IsEnemy;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterAttributes", Replicated)
		int32 CharacterLevel;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterAttributes", Replicated)
		int32 XP;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterAttributes", Replicated)
		int32 TeamNumber;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterAttributes", Replicated)
		int32 Gold;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterAttributes", Replicated)
		int32 Silver;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterAttributes", Replicated)
		int32 Copper;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterAttributes", Replicated)
		int32 FreeCurrency;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterAttributes", Replicated)
		int32 PremiumCurrency;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterAttributes", Replicated)
		int32 Score;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float HitDie;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Wounds;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Thirst;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Hunger;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float MaxHealth;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Health;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float HealthRegenRate;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float MaxMana;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Mana;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float ManaRegenRate;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float MaxEnergy;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Energy;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float EnergyRegenRate;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float MaxFatigue;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Fatigue;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float FatigueRegenRate;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float MaxStamina;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Stamina;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float StaminaRegenRate;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float MaxEndurance;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Endurance;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float EnduranceRegenRate;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Strength;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Dexterity;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Constitution;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Intellect;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Wisdom;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Charisma;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Agility;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Spirit;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Magic;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Fortitude;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Reflex;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Willpower;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float BaseAttack;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float BaseAttackBonus;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float AttackPower;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float AttackSpeed;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float CritChance;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float CritMultiplier;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Haste;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float SpellPower;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float SpellPenetration;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Defense;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Dodge;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Parry;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Avoidance;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Versatility;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Multishot;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Initiative;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float NaturalArmor;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float PhysicalArmor;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float BonusArmor;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float ForceArmor;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float MagicArmor;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Resistance;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float ReloadSpeed;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Range;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		float Speed;


	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		int32 HitDice;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		int32 MaxHP;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		int32 Perception;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		int32 Acrobatics;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		int32 Climb;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "CharacterStats", Replicated)
		int32 Stealth;

};
