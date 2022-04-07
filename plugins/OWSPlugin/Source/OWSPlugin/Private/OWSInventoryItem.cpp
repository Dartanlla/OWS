// Copyright 2018 Sabre Dart Studios

#include "OWSInventoryItem.h"


// Sets default values
AOWSInventoryItem::AOWSInventoryItem()
{
 	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

	bReplicates = true;
	bOnlyRelevantToOwner = true;
}

// Called when the game starts or when spawned
void AOWSInventoryItem::BeginPlay()
{
	Super::BeginPlay();
	
}

// Called every frame
void AOWSInventoryItem::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

}

