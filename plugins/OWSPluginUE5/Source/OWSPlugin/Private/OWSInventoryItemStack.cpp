// Copyright 2018 Sabre Dart Studios

#include "OWSInventoryItemStack.h"


UOWSInventoryItemStack::UOWSInventoryItemStack(const FObjectInitializer& ObjectInitializer)
	: Super(ObjectInitializer)
{

}


void UOWSInventoryItemStack::AddToStack(AOWSInventoryItem* InventoryItem)
{
	InventoryItems.Push(InventoryItem);
}

void UOWSInventoryItemStack::AddToStack(UOWSInventoryItemStack* InventoryItemStack)
{
	int curItem = 0;
	for (TArray<AOWSInventoryItem*>::TConstIterator ItemIter(InventoryItemStack->InventoryItems); ItemIter; ++ItemIter)
	{
		if (curItem < InventoryItemStack->InventoryItems.Num())
		{
			InventoryItems.Push((*ItemIter));
		}

		curItem++;
	}
}

AOWSInventoryItem* UOWSInventoryItemStack::RemoveFromTopOfStack()
{
	return InventoryItems.Pop();
}

AOWSInventoryItem* UOWSInventoryItemStack::GetTopItemFromStack()
{
	if (InventoryItems.Num() > 0)
		return InventoryItems.Top();
	else
		return nullptr;
}


