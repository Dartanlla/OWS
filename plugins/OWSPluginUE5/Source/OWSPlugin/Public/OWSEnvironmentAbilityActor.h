// Copyright 2018 Sabre Dart Studios

#pragma once

#include "CoreMinimal.h"
#include "OWSGameplayAbility.h"
#include "GameFramework/Actor.h"
#include "Runtime/Engine/Classes/Components/SphereComponent.h"
#include "Runtime/Engine/Classes/Particles/ParticleSystemComponent.h"
#include "OWSEnvironmentAbilityActor.generated.h"

UCLASS()
class OWSPLUGIN_API AOWSEnvironmentAbilityActor : public AActor
{
	GENERATED_BODY()
	
public:	
	// Sets default values for this actor's properties
	AOWSEnvironmentAbilityActor();

	UPROPERTY(BlueprintReadWrite, EditDefaultsOnly, Category = "Ability")
		USphereComponent* SphereCollision;

	UPROPERTY(BlueprintReadWrite, EditAnywhere, Category = "Ability")
		float CollisionRadius;

	UPROPERTY(BlueprintReadWrite, EditDefaultsOnly, Category = "Ability")
		UParticleSystemComponent* FXOnPeriodicActivation;

	/* This is the class we will activating the abilities on */
	UPROPERTY(BlueprintReadWrite, EditDefaultsOnly, Category = "Ability")
		TSubclassOf<AActor> ActorClassFilter;

	/*
	UPROPERTY(BlueprintReadWrite, EditDefaultsOnly, Category = "Ability|OWS")
		TSubclassOf<UOWSGameplayAbility> AbilityToActivateOnBeginOverlap;

	UPROPERTY(BlueprintReadWrite, EditDefaultsOnly, Category = "Ability|OWS")
		float BeginOverlapLatchLengthSeconds;

	UPROPERTY(BlueprintReadWrite, EditDefaultsOnly, Category = "Ability|OWS")
		TSubclassOf<UOWSGameplayAbility> AbilityToActivateOnEndOverlap;

	UPROPERTY(BlueprintReadWrite, EditDefaultsOnly, Category = "Ability|OWS")
		float EndOverlapLatchLengthSeconds;
	*/

	/* Activate this Ability each Period Length (seconds).  Must be a Server Initiated Ability as this only fires on the server side. */
	UPROPERTY(BlueprintReadWrite, EditDefaultsOnly, Category = "Ability")
		TSubclassOf<UOWSGameplayAbility> AbilityToActivatePeriodically;

	UPROPERTY(BlueprintReadWrite, EditAnywhere, Category = "Ability")
		class UParticleSystem* ParticleEffectToPlayOnPeriodicActivaion;

	/* Be careful not to set this value too low and activate too many abilities in a short period of time!*/
	UPROPERTY(BlueprintReadWrite, EditAnywhere, Category = "Ability")
		float PeriodLength;

	/* Despawns when the limit is met. Zero is an unlimited number of activations */
	UPROPERTY(BlueprintReadWrite, EditAnywhere, Category = "Ability")
		float MaxNumberOfPeriodicActivations;

	UFUNCTION(BlueprintCallable, Category = "Ability|OWS")
		void ActivatePeriodicAbility();

	UFUNCTION(NetMulticast, Unreliable)
		void Multicast_PlayPeriodicParticleFX();

protected:
	// Called when the game starts or when spawned
	virtual void BeginPlay() override;

	FTimerHandle PeriodicAbilityTimer;

	int32 PeriodicActivationCount;

	//bool ActivateOnBeginOverlapAlreadyFired;
	//bool ActivateOnEndOverlapAlreadyFired;

public:	
	// Called every frame
	//virtual void Tick(float DeltaTime) override;
	virtual void PostInitializeComponents() override;

};
