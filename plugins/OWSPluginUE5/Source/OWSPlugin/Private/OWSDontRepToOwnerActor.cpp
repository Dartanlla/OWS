// Copyright 2018 Sabre Dart Studios

#include "OWSDontRepToOwnerActor.h"


// Sets default values
AOWSDontRepToOwnerActor::AOWSDontRepToOwnerActor()
{
 	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;
	bReplicates = true;
	SetReplicatingMovement(true);
	bNetUseOwnerRelevancy = false;
	bOnlyRelevantToOwner = false;
}

// Called when the game starts or when spawned
void AOWSDontRepToOwnerActor::BeginPlay()
{
	Super::BeginPlay();
}

// Called every frame
void AOWSDontRepToOwnerActor::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);
}

bool AOWSDontRepToOwnerActor::IsNetRelevantFor(const AActor * RealViewer, const AActor * ViewTarget, const FVector & SrcLocation) const
{
	return !IsOwnedBy(ViewTarget);
}