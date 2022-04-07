// Copyright 2018 Sabre Dart Studios

#include "OWSInventory.h"
#include "OWSGameMode.h"

UOWSInventory::UOWSInventory(const FObjectInitializer& ObjectInitializer)
	: Super(ObjectInitializer)
{
	//SetReplicates(true);
	//bOnlyRelevantToOwner = true;
}

void UOWSInventory::SetInventorySize(int32 Size, int32 inNumberOfColumns)
{
	NumberOfSlots = Size;
	this->NumberOfColumns = inNumberOfColumns;
	InventoryItemStacks.Empty();
	SlotsFilled.Empty();
	for (int32 CurSlot = 0; CurSlot < Size; CurSlot++)
	{
		UOWSInventoryItemStack* tempInventoryItemStack = NewObject<UOWSInventoryItemStack>();
		InventoryItemStacks.Add(tempInventoryItemStack);
		SlotsFilled.Add(false);
	}
}

void UOWSInventory::SetInventoryName(FName inInventoryName)
{
	InventoryName = inInventoryName;
}

void UOWSInventory::AddStackToSlot(UOWSInventoryItemStack* ItemStack, int32 Slot)
{
	if (InventoryItemStacks.IsValidIndex(Slot))
	{
		ItemStack->SlotNumber = Slot;
		InventoryItemStacks[Slot] = ItemStack;
		UpdateSlotsFilled();
	}
}

void UOWSInventory::RemoveStackFromSlot(int32 Slot)
{
	if (InventoryItemStacks.IsValidIndex(Slot))
	{
		UOWSInventoryItemStack* tempInventoryItemStack = NewObject<UOWSInventoryItemStack>();
		InventoryItemStacks[Slot] = tempInventoryItemStack;
		UpdateSlotsFilled();
	}
}

void UOWSInventory::SetOwningPlayerCharacter(AOWSCharacter* inOwningPlayerCharacter)
{
	OwningPlayerCharacter = inOwningPlayerCharacter;
}

//Can only be called on the Server side
bool UOWSInventory::AddItemToInventory(AOWSInventoryItem* Item)
{	
	int32 Slot = FindFirstEmptySlotToFitItemOfSize(Item->IconSlotWidth, Item->IconSlotHeight);
	if (Slot != -1 && Slot < (NumberOfGroupsUnlocked * SlotsPerGroup)) //-1 = Full Inventory
	{
		//Add item on the server
		//AddItemToSlot_Internal(Item, Slot);
		//Call AddItemToInventory on OWSCharacter
		FGuid UniqueItemGUID;

		//Replicate item definition if it does not already exist
		AOWSGameMode* OWSGameMode = OwningPlayerCharacter->GetGameMode();

		if (!OWSGameMode)
			return false;

		FInventoryItemStruct& ItemDefinition = OWSGameMode->FindItemDefinition(Item->ItemName);

		bool bWasItemAdded = OwningPlayerCharacter->AddItemToLocalInventoryItems(Item->ItemName, ItemDefinition.ItemCanStack, ItemDefinition.IsUsable, ItemDefinition.IsConsumedOnUse, ItemDefinition.ItemTypeID,
			ItemDefinition.TextureToUseForIcon, ItemDefinition.IconSlotWidth, ItemDefinition.IconSlotHeight, ItemDefinition.ItemMeshID, ItemDefinition.CustomData);

		if (bWasItemAdded)
		{
			UE_LOG(OWS, Warning, TEXT("UOWSInventory - AddItemToInventory - AddItemMeshToAllPlayers Called"));
			OwningPlayerCharacter->GetGameMode()->AddItemMeshToAllPlayers(Item->ItemName, ItemDefinition.ItemMeshID);

			OwningPlayerCharacter->Client_AddItemToLocalInventoryItems(Item->ItemName, ItemDefinition.ItemCanStack, ItemDefinition.IsUsable, ItemDefinition.IsConsumedOnUse, ItemDefinition.ItemTypeID,
				ItemDefinition.TextureToUseForIcon, ItemDefinition.IconSlotWidth, ItemDefinition.IconSlotHeight, ItemDefinition.ItemMeshID, ItemDefinition.CustomData);
		}

		//Add item to server side inventory
		OwningPlayerCharacter->AddItemToInventory(InventoryName.ToString(), Item->ItemName, Slot, Item->StackSize, Item->NumberOfUsesLeft, Item->Condition, UniqueItemGUID);
		//Call the Owning client to add the item locally
		OwningPlayerCharacter->Client_AddItemToInventory(InventoryName, Item->ItemName, Item->StackSize, Slot, Item->NumberOfUsesLeft, Item->Condition,
			Item->PerInstanceCustomData, UniqueItemGUID, Item->ItemMeshID);
		return true;
	}

	return false;
}

//Can only be called on the Server side
void UOWSInventory::AddItemToSlot(AOWSInventoryItem* Item, int32 Slot)
{	
	UE_LOG(OWS, Warning, TEXT("UOWSInventory - AddItemToSlot Started"));

	AddItemToSlot_Internal(Item, Slot);

	//Replicate item definition if it does not already exist
	AOWSGameMode* OWSGameMode = OwningPlayerCharacter->GetGameMode();

	if (!OWSGameMode)
		return;

	FInventoryItemStruct& ItemDefinition = OWSGameMode->FindItemDefinition(Item->ItemName);

	bool bWasItemAdded = OwningPlayerCharacter->AddItemToLocalInventoryItems(Item->ItemName, ItemDefinition.ItemCanStack, ItemDefinition.IsUsable, ItemDefinition.IsConsumedOnUse, ItemDefinition.ItemTypeID,
		ItemDefinition.TextureToUseForIcon, ItemDefinition.IconSlotWidth, ItemDefinition.IconSlotHeight, ItemDefinition.ItemMeshID, ItemDefinition.CustomData);

	if (bWasItemAdded)
	{
		UE_LOG(OWS, Warning, TEXT("UOWSInventory - AddItemToSlot - AddItemMeshToAllPlayers Called"));
		OwningPlayerCharacter->GetGameMode()->AddItemMeshToAllPlayers(Item->ItemName, ItemDefinition.ItemMeshID);

		OwningPlayerCharacter->Client_AddItemToLocalInventoryItems(Item->ItemName, ItemDefinition.ItemCanStack, ItemDefinition.IsUsable, ItemDefinition.IsConsumedOnUse, ItemDefinition.ItemTypeID,
			ItemDefinition.TextureToUseForIcon, ItemDefinition.IconSlotWidth, ItemDefinition.IconSlotHeight, ItemDefinition.ItemMeshID, ItemDefinition.CustomData);
	}

	//Call the Owning client to add the item locally
	OwningPlayerCharacter->Client_AddItemToInventory(InventoryName, Item->ItemName, Item->StackSize, Slot, Item->NumberOfUsesLeft, Item->Condition,
		Item->PerInstanceCustomData, Item->UniqueItemGUID, Item->ItemMeshID);
}

void UOWSInventory::AddItemToSlot_Internal(AOWSInventoryItem* Item, int32 Slot)
{
	UOWSInventoryItemStack* InventoryItemStack = GetStackInSlot(Slot);
	if (InventoryItemStack)
	{
		if (!InventoryItemStack->GetTopItemFromStack())
		{
			InventoryItemStack = NewObject<UOWSInventoryItemStack>();
			InventoryItemStack->AddToStack(Item);
			//Set the SlotNumber
			InventoryItemStack->SlotNumber = Slot;
			AddStackToSlot(InventoryItemStack, Slot);
		}
		else
		{
			InventoryItemStack->AddToStack(Item);
		}
	}
}


void UOWSInventory::AddItemsFromInventoryItemStruct(const TArray<FInventoryItemStruct>& ItemsToAdd)
{
	UE_LOG(OWS, Warning, TEXT("UOWSInventory - AddItemsFromInventoryItemStruct"));

	for (auto CurItem : ItemsToAdd)
	{
		AOWSInventoryItem* ItemToAdd = NewObject<AOWSInventoryItem>();

		ItemToAdd->UniqueItemGUID = CurItem.UniqueItemGUID;
		ItemToAdd->ItemName = CurItem.ItemName;
		ItemToAdd->StackSize = CurItem.ItemStackSize;
		ItemToAdd->Condition = CurItem.Condition;
		ItemToAdd->IconTexture = CurItem.TextureIcon;
		ItemToAdd->ItemMeshID = CurItem.ItemMeshID;

		for (int CurItemToAddToStack = 0; CurItemToAddToStack < FMath::Max(CurItem.Quantity, 1); CurItemToAddToStack++)
		{
			AddItemToSlot(ItemToAdd, CurItem.InSlotNumber);
		}
	}
}

AOWSInventoryItem* UOWSInventory::RemoveOneItemFromSlot(int32 Slot)
{
	UOWSInventoryItemStack* InventoryItemStack = GetStackInSlot(Slot);
	if (InventoryItemStack)
	{
		AOWSInventoryItem* InventoryItem = InventoryItemStack->GetTopItemFromStack();
		if (InventoryItem)
		{
			AOWSInventoryItem* InventoryItemRemoved = InventoryItemStack->RemoveFromTopOfStack();
			UpdateSlotsFilled();
			return InventoryItemRemoved;
		}

		return nullptr;
	}

	return nullptr;
}

void UOWSInventory::SwapSlots(int32 SlotA, int32 SlotB)
{
	if (InventoryItemStacks.IsValidIndex(SlotA) && InventoryItemStacks.IsValidIndex(SlotB))
	{
		//Change the SlotNumber's to their new values
		InventoryItemStacks[SlotA]->SlotNumber = SlotB;
		InventoryItemStacks[SlotB]->SlotNumber = SlotA;
		//Then swap the locations in the TArray
		InventoryItemStacks.Swap(SlotA, SlotB);

		UpdateSlotsFilled();
	}
}

UOWSInventoryItemStack* UOWSInventory::GetStackInSlot(int32 SlotNumber)
{
	if (InventoryItemStacks.Num() > 0 && InventoryItemStacks.IsValidIndex(SlotNumber))
	{
		return InventoryItemStacks[SlotNumber];
	}

	return nullptr;
}

bool UOWSInventory::IsSlotFilled(int32 Slot)
{
	if (Slot < 0 || Slot >= NumberOfSlots)
		return false;

	return SlotsFilled[Slot];
}

void UOWSInventory::UpdateSlotsFilled()
{
	int32 Slot = 0;
	for (TArray<UOWSInventoryItemStack*>::TConstIterator StackIter(InventoryItemStacks); StackIter; ++StackIter)
	{
		SlotsFilled[Slot] = false;
		Slot++;
	}

	Slot = 0;
	for (TArray<UOWSInventoryItemStack*>::TConstIterator StackIter(InventoryItemStacks); StackIter; ++StackIter)
	{
		AOWSInventoryItem* ItemInSlot = (*StackIter)->GetTopItemFromStack();

		if (ItemInSlot)
		{
			SlotsFilled[Slot] = true;

			if (ItemInSlot->IconSlotWidth > 1 || ItemInSlot->IconSlotHeight)
			{
				int32 StartingRow = FMath::FloorToInt(Slot / NumberOfColumns);
				int32 StartingCol = Slot % NumberOfColumns;

				for (int32 CurRow = StartingRow; CurRow < StartingRow + ItemInSlot->IconSlotHeight; CurRow++) {
					for (int32 CurCol = StartingCol; CurCol < StartingCol + ItemInSlot->IconSlotWidth; CurCol++) {
						if (CurRow != StartingRow || CurCol != StartingCol)
						{
							//SlotsFilled.EmplaceAt((CurRow * NumberOfColumns) + CurCol, true);
							int32 SlotToSet = (CurRow * NumberOfColumns) + CurCol;
							if (SlotToSet < 0 || SlotToSet >= NumberOfSlots)
								continue;

							SlotsFilled[SlotToSet] = true;
						}
					}
				}
			}
		}

		Slot++;
	}
}

int32 UOWSInventory::FindFirstEmptySlotToFitItemOfSize(int32 IconSlotWidth, int32 IconSlotHeight)
{
	int32 Slot = 0;

	UpdateSlotsFilled();

	Slot = 0;
	for (TArray<UOWSInventoryItemStack*>::TConstIterator StackIter(InventoryItemStacks); StackIter; ++StackIter)
	{
		AOWSInventoryItem* ItemInSlot = (*StackIter)->GetTopItemFromStack();

		bool SlotIsFilled = false;

		if (!ItemInSlot)
		{
			if (SlotsFilled[Slot])
			{
				Slot++;
				continue;
			}

			if (IconSlotWidth > 1 || IconSlotHeight)
			{
				int32 StartingRow = FMath::FloorToInt(Slot / NumberOfColumns);
				int32 StartingCol = Slot % NumberOfColumns;

				for (int32 CurRow = StartingRow; CurRow < StartingRow + IconSlotHeight; CurRow++) {
					for (int32 CurCol = StartingCol; CurCol < StartingCol + IconSlotWidth; CurCol++) {
						if (CurRow != StartingRow || CurCol != StartingCol)
						{
							//We are outside the bounds of the inventory on the right side
							if (CurCol >= NumberOfColumns)
							{
								SlotIsFilled = true;
								break;
							}

							//We are outside the bounds of the inventory on the bottom side
							if (CurRow >= NumberOfColumns)
							{
								SlotIsFilled = true;
								break;
							}

							int32 SlotToCheck = (CurRow * NumberOfColumns) + CurCol;
							if (SlotToCheck < 0 || SlotToCheck >= NumberOfSlots)
							{
								continue;
							}

							if (SlotsFilled[SlotToCheck])
							{
								SlotIsFilled = true;
								break;
							}
						}
					}
				}
			}
		}
		else
		{
			Slot++;
			continue;
		}

		if (!SlotIsFilled)
		{
			return Slot;
		}

		Slot++;
	}

	return -1; //Inventory is Full
}

int32 UOWSInventory::FindItemIndex(FString ItemName)
{
	int32 Slot = 0;
	for (TArray<UOWSInventoryItemStack*>::TConstIterator StackIter(InventoryItemStacks); StackIter; ++StackIter)
	{
		if ((*StackIter)->GetTopItemFromStack() && (*StackIter)->GetTopItemFromStack()->ItemName == ItemName)
		{
			return Slot;
		}
		Slot++;
	}

	return -1; //Item not found
}