// Copyright 2018 Sabre Dart Studios

#pragma once

#include "CoreMinimal.h"
#include "UObject/NoExportTypes.h"
#include "OWSInventoryItem.h"
#include "OWSInventoryItemStack.generated.h"

/**
 * 
 */
UCLASS()
class OWSPLUGIN_API UOWSInventoryItemStack : public UObject
{
	GENERATED_UCLASS_BODY()

public:
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = Inventory)
		TArray<AOWSInventoryItem*> InventoryItems;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = Inventory)
		int32 SlotNumber;

	bool IsBeingDragged;

	UFUNCTION(BlueprintCallable, Category = "InventoryStack")
		void AddToStack(AOWSInventoryItem* InventoryItem);

	UFUNCTION(BlueprintCallable, Category = "InventoryStack")
		AOWSInventoryItem* GetTopItemFromStack();

	void AddToStack(UOWSInventoryItemStack* InventoryItemStack);
	AOWSInventoryItem* RemoveFromTopOfStack();
	
};
