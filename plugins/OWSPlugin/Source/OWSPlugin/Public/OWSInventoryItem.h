// Copyright 2018 Sabre Dart Studios

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "OWSInventoryItem.generated.h"

UENUM(BlueprintType)
enum ItemCategories
{
	Consumable	UMETA(DisplayName = "Consumable"),
};


USTRUCT(BlueprintType)
struct FInventoryItemStruct
{
	GENERATED_BODY()

public:
	FInventoryItemStruct() {
		ItemName = "";
		InventoryName = "";
		ItemDescription = "";
		UniqueItemGUID = FGuid();
		Quantity = 0;
		ItemValue = 0;
		ItemCanStack = false;
		ItemStackSize = 0;
		IsUsable = false;
		IsConsumedOnUse = false;
		NumberOfUsesLeft = 0;
		ItemWeight = 0.f;
		ItemTypeID = 0;
		ItemTypeDescription = "";
		ItemTypeQuality = 0;
		UserItemType = 0;
		EquipmentType = 0;
		EquipmentSlotType = 0;
		ItemTier = 0;
		ItemQuality = 0;
		CustomData = "";
		InSlotNumber = 0;
		Condition = 0;
		ItemDuration = 0;		
		CanBeDropped = false;
		CanBeDestroyed = false;
		PremiumCurrencyPrice = 0;
		FreeCurrencyPrice = 0;
		WeaponActorClassPath = "";
		ItemMeshID = 0;
		StaticMeshPath = "";
		SkeletalMeshPath = "";
		TextureToUseForIcon = "";
		IconSlotWidth = 0;
		IconSlotHeight = 0;
		TextureIcon = nullptr;
		PerInstanceCustomData = "";
	}

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		FString ItemName;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		FString InventoryName;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		FString ItemDescription;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		FGuid UniqueItemGUID;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		int32 Quantity;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		int32 ItemValue;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		bool ItemCanStack;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		int32 ItemStackSize;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		bool IsUsable;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		bool IsConsumedOnUse;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		int32 NumberOfUsesLeft;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		float ItemWeight;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		int32 ItemTypeID;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		FString ItemTypeDescription;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		int32 ItemTypeQuality;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		int32 UserItemType;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		int32 EquipmentType;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		int32 EquipmentSlotType;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		int32 ItemTier;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		int32 ItemQuality;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		FString CustomData;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		int32 InSlotNumber;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		int32 Condition;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		int32 ItemDuration;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		bool CanBeDropped;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		bool CanBeDestroyed;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		int32 PremiumCurrencyPrice;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		int32 FreeCurrencyPrice;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		FString WeaponActorClassPath;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		int32 ItemMeshID;

	/*UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		TSubclassOf<class AActor> WeaponActorClass = nullptr;*/

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		FString StaticMeshPath;

	/*UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Inventory")
		UStaticMesh* StaticMesh = nullptr;*/

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		FString SkeletalMeshPath;

	/*UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		USkeletalMesh* SkeletalMesh = nullptr;*/

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		FString TextureToUseForIcon;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		int32 IconSlotWidth;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		int32 IconSlotHeight;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		UTexture2D* TextureIcon = nullptr;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		FString PerInstanceCustomData;
};

UCLASS()
class OWSPLUGIN_API AOWSInventoryItem : public AActor
{
	GENERATED_BODY()
	
public:	
	// Sets default values for this actor's properties
	AOWSInventoryItem();

	UPROPERTY(EditAnywhere, Category = "Inventory")
		bool CanStack;
	UPROPERTY(EditAnywhere, Category = "Inventory")
		int32 StackSize;
	UPROPERTY(EditAnywhere, Category = "Inventory")
		FString ItemName;
	UPROPERTY(EditAnywhere, Category = "Inventory")
		FGuid UniqueItemGUID;

	UPROPERTY(EditAnywhere, Category = "Inventory")
		int32 Condition;
	UPROPERTY(EditAnywhere, Category = "Inventory")
		int32 NumberOfUsesLeft;

	UPROPERTY(EditAnywhere, Category = "Inventory")
		FString PerInstanceCustomData;

	UPROPERTY(EditAnywhere, Category = "Inventory")
		FString SkeletalMeshPath;

	UPROPERTY(EditAnywhere, Category = "Inventory")
		int32 ItemMeshID;

	/*UPROPERTY(EditAnywhere, Category = "Inventory")
		int32 ItemID;			//(for SQL)
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Inventory")
		FName InventoryItemName;

	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Inventory")
		TEnumAsByte<ItemCategories> ItemCategory;	//This can be used to define groups of items that work similarly
		*/



	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Inventory")
		UTexture2D* IconTexture;

	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Inventory")
		int32 IconSlotHeight;

	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Inventory")
		int32 IconSlotWidth;

	/*UPROPERTY(EditAnywhere, Category = "Inventory")
		FInventoryItemStruct InventoryItemData;*/

protected:
	// Called when the game starts or when spawned
	virtual void BeginPlay() override;

public:	
	// Called every frame
	virtual void Tick(float DeltaTime) override;

	
	
};
