// Copyright 2018 Sabre Dart Studios

#include "OWSAbilityActor.h"
#include "AbilitySystemComponent.h"
#include "GameplayTagContainer.h"
#include "GameplayTagsModule.h"
#include "OWSBasicAttributeSet.h"


// Sets default values
AOWSAbilityActor::AOWSAbilityActor()
{
 	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

	bReplicates = true;

	AbilitySystem = CreateDefaultSubobject<UAbilitySystemComponent>(TEXT("AbilitySystem"));
	AbilitySystem->SetIsReplicated(true);
}

// Called when the game starts or when spawned
void AOWSAbilityActor::BeginPlay()
{
	Super::BeginPlay();

	OWSBasicAttributes = NewObject<UOWSBasicAttributeSet>();

	if (AbilitySystem)
	{
		AbilitySystem->InitAbilityActorInfo(this, this);
	}
}

void AOWSAbilityActor::PostInitializeComponents()
{
	Super::PostInitializeComponents();

	if (AbilitySystem)
	{
		AbilitySystem->InitStats(
			UOWSBasicAttributeSet::StaticClass(), NULL);
	}
}

// Called every frame
void AOWSAbilityActor::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

}

