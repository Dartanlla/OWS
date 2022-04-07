// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "UObject/ObjectMacros.h"
#include "Templates/SubclassOf.h"
#include "GameFramework/Actor.h"
#include "OWSAdvancedProjectile.h"
#include "Abilities/GameplayAbilityTargetTypes.h"
#include "Abilities/Tasks/AbilityTask.h"
#include "OWSAbilityTask_SpawnProjectile.generated.h"

DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FSpawnProjectileDelegate, AActor*, SpawnedActor);

USTRUCT()
struct FDelayedProjectileInfo
{
	GENERATED_USTRUCT_BODY()

	UPROPERTY()
		TSubclassOf<AOWSAdvancedProjectile> ProjectileClass;

	UPROPERTY()
		FVector SpawnLocation;

	UPROPERTY()
		FRotator SpawnRotation;

	FDelayedProjectileInfo()
		: ProjectileClass(NULL), SpawnLocation(ForceInit), SpawnRotation(ForceInit)
	{}
};

/**
 * Spawn a projectile
 */
UCLASS()
class OWSPLUGIN_API UOWSAbilityTask_SpawnProjectile : public UAbilityTask
{
	GENERATED_UCLASS_BODY()
	
	UPROPERTY(BlueprintAssignable)
		FSpawnProjectileDelegate	Success;

	/** Called when we can't spawn: on clients or potentially on server if they fail to spawn (rare) */
	UPROPERTY(BlueprintAssignable)
		FSpawnProjectileDelegate	DidNotSpawn;

	/** Spawn new Actor on the network authority (server) */
	UFUNCTION(BlueprintCallable, meta = (HidePin = "OwningAbility", DefaultToSelf = "OwningAbility", BlueprintInternalUseOnly = "true"), Category = "Ability|Tasks")
		static UOWSAbilityTask_SpawnProjectile* SpawnProjectile(UGameplayAbility* OwningAbility, FGameplayAbilityTargetDataHandle TargetData, TSubclassOf<AActor> Class, bool UseAimCamera,
			bool IgnoreAimCameraPitch, FName StartingSocketName, float ForwardOffsetFromSocket, bool UseFixedStartingLocationRotation, FVector StartingLocation, FRotator StartingRotation,
			FGameplayEffectSpecHandle DirectDamageEffect, FGameplayEffectSpecHandle AOEDamageEffect, FGameplayTag ActivateAbilityTagOnImpact);

	UPROPERTY()
		TSubclassOf<AActor> ProjectileClass;

	UFUNCTION(BlueprintCallable, meta = (HidePin = "OwningAbility", DefaultToSelf = "OwningAbility", BlueprintInternalUseOnly = "true"), Category = "Abilities")
		bool BeginSpawningActor(UGameplayAbility* OwningAbility, FGameplayAbilityTargetDataHandle TargetData, TSubclassOf<AActor> Class, AActor*& SpawnedActor);

	UFUNCTION(BlueprintCallable, meta = (HidePin = "OwningAbility", DefaultToSelf = "OwningAbility", BlueprintInternalUseOnly = "true"), Category = "Abilities")
		void FinishSpawningActor(UGameplayAbility* OwningAbility, FGameplayAbilityTargetDataHandle TargetData, AActor* SpawnedActor);

	void SpawnDelayedFakeProjectile();

	void GetAimTransform(FTransform& SpawnTransform);

	virtual void Activate() override;

protected:
	FGameplayAbilityTargetDataHandle CachedTargetDataHandle;

	UPROPERTY()
		bool bUseAimCamera;

	UPROPERTY()
		bool bIgnoreAimCameraPitch;

	UPROPERTY()
		FName nameStartingSocketName;

	UPROPERTY()
		float fForwardOffsetFromSocket;

	UPROPERTY()
		bool bUseFixedStartingLocationRotation;

	UPROPERTY()
		FVector StartingLocation;

	UPROPERTY()
		FRotator StartingRotation;

	UPROPERTY()
		bool bDoesAOEDamge;
	
	UPROPERTY()
		float fAOEDamageRadius;
	
	UPROPERTY()
		FGameplayEffectSpecHandle geshDirectDamageEffect;

	UPROPERTY()
		FGameplayEffectSpecHandle geshAOEDamageEffect;

	UPROPERTY()
		FGameplayTag tagActivateAbilityTagOnImpact;



	/** Delayed projectile information */
	UPROPERTY()
		FDelayedProjectileInfo DelayedProjectile;

	FTimerHandle SpawnDelayedFakeProjHandle;
};
