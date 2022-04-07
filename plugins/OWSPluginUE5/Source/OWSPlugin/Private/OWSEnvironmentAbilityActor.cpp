// Copyright 2018 Sabre Dart Studios


#include "OWSEnvironmentAbilityActor.h"

// Sets default values
AOWSEnvironmentAbilityActor::AOWSEnvironmentAbilityActor()
{
 	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = false;
	bNetLoadOnClient = true;
	bReplicates = true;

	//BeginOverlapLatchLengthSeconds = 1.f;
	//EndOverlapLatchLengthSeconds = 1.f;
	PeriodicActivationCount = 0;
	PeriodLength = 2.f;
	MaxNumberOfPeriodicActivations = 0.f;
	CollisionRadius = 100.f;
	//ActivateOnBeginOverlapAlreadyFired = false;
	//ActivateOnEndOverlapAlreadyFired = false;

	SphereCollision = CreateDefaultSubobject<USphereComponent>(TEXT("SphereCollision"));
	SphereCollision->InitSphereRadius(CollisionRadius);
	RootComponent = SphereCollision;

	FXOnPeriodicActivation = CreateDefaultSubobject<UParticleSystemComponent>(TEXT("FXOnPeriodicActivation"));
	FXOnPeriodicActivation->SetupAttachment(RootComponent);
	FXOnPeriodicActivation->bAutoActivate = false;
	FXOnPeriodicActivation->SetRelativeLocation(FVector(0.f, 0.f, 0.f));
	FXOnPeriodicActivation->bAllowRecycling = true;
	FXOnPeriodicActivation->bSuppressSpawning = true;	
}

// Called when the game starts or when spawned
void AOWSEnvironmentAbilityActor::BeginPlay()
{
	Super::BeginPlay();
	
}

void AOWSEnvironmentAbilityActor::PostInitializeComponents()
{
	Super::PostInitializeComponents();

	SphereCollision->SetSphereRadius(CollisionRadius);
	FXOnPeriodicActivation->SetTemplate(ParticleEffectToPlayOnPeriodicActivaion);

	//Only run the timer on the Server
	if (GetLocalRole() == ROLE_Authority)
	{
		GetWorld()->GetTimerManager().SetTimer(PeriodicAbilityTimer, this, &AOWSEnvironmentAbilityActor::ActivatePeriodicAbility, PeriodLength, true);
	}
}

// Called every frame
/*void AOWSEnvironmentAbilityActor::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

}*/

void AOWSEnvironmentAbilityActor::ActivatePeriodicAbility()
{
	//Only run this on the server
	if (GetLocalRole() != ROLE_Authority)
		return;
	
	UE_LOG(OWS, Verbose, TEXT("Server: AOWSEnvironmentAbilityActor - Activate Periodic Ability"));

	//if MaxNumberOfPeriodicActivations == 0, the run infinitely
	if (MaxNumberOfPeriodicActivations > 0)
	{
		if (PeriodicActivationCount >= MaxNumberOfPeriodicActivations)
		{
			GetWorld()->GetTimerManager().ClearTimer(PeriodicAbilityTimer);
			Destroy();
			return;
		}

		//Increate the activation count by 1
		PeriodicActivationCount++;
	}

	//Play the particle FX multicast
	if (FXOnPeriodicActivation)
	{
		Multicast_PlayPeriodicParticleFX();
	}

	TArray<AActor*> OverlappingActors;
	SphereCollision->GetOverlappingActors(OverlappingActors, ActorClassFilter);

	for (auto ActorThatMightHaveASC : OverlappingActors)
	{
		UE_LOG(OWS, Verbose, TEXT("Server: AOWSEnvironmentAbilityActor - Activate Ability On: %s"), *ActorThatMightHaveASC->GetName());

		AOWSCharacterWithAbilities* ActorWithASC = Cast<AOWSCharacterWithAbilities>(ActorThatMightHaveASC);

		if (!ActorWithASC)
			continue;

		UAbilitySystemComponent* ASC = ActorWithASC->GetAbilitySystemComponent();

		bool wasAbleToActivateAbility = ASC->TryActivateAbilityByClass(AbilityToActivatePeriodically, true);

		bool test1 = wasAbleToActivateAbility;
	}
}

void AOWSEnvironmentAbilityActor::Multicast_PlayPeriodicParticleFX_Implementation()
{
	FXOnPeriodicActivation->ActivateSystem(true);
}

