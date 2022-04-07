// Copyright 2017 Sabre Dart Studios

#include "OWSCharacterBase.h"
#include "Net/UnrealNetwork.h"


// Sets default values
AOWSCharacterBase::AOWSCharacterBase(const FObjectInitializer& ObjectInitializer)
	: Super(ObjectInitializer.SetDefaultSubobjectClass<UOWSCharacterMovementComponent>(ACharacter::CharacterMovementComponentName))
{
 	// Set this character to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

	IsTransferringBetweenMaps = false;
}

// Called when the game starts or when spawned
void AOWSCharacterBase::BeginPlay()
{
	Super::BeginPlay();
	
}

// Called every frame
void AOWSCharacterBase::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

}

// Called to bind functionality to input
void AOWSCharacterBase::SetupPlayerInputComponent(UInputComponent* PlayerInputComponent)
{
	Super::SetupPlayerInputComponent(PlayerInputComponent);	
}

void AOWSCharacterBase::PostInitializeComponents()
{
	Super::PostInitializeComponents();

	OWSCharacterMovementComponent = Cast<UOWSCharacterMovementComponent>(Super::GetMovementComponent());
}


/*
void AOWSCharacterBase::GetLifetimeReplicatedProps(TArray< FLifetimeProperty > & OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);

	// Replicate to everyone
	DOREPLIFETIME(AOWSCharacterBase, IsTransferringBetweenMaps);
}*/
