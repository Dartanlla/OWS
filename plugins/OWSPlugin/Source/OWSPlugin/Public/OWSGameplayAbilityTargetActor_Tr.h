// Copyright 2018 Sabre Dart Studios

#pragma once

#include "CoreMinimal.h"
#include "UObject/ObjectMacros.h"
#include "Engine/EngineTypes.h"
#include "CollisionQueryParams.h"
#include "WorldCollision.h"
#include "Engine/CollisionProfile.h"
#include "Abilities/GameplayAbilityTargetTypes.h"
#include "Abilities/GameplayAbilityTargetDataFilter.h"
#include "OWSGameplayAbilityTargetActor.h"
#include "OWSGameplayAbilityTargetDatafilter.h"
#include "OWSGameplayAbilityTargetActor_Tr.generated.h"

class UGameplayAbility;

/** Intermediate base class for all line-trace type targeting actors. */
UCLASS(Abstract, Blueprintable, notplaceable, config = Game)
class OWSPLUGIN_API AOWSGameplayAbilityTargetActor_Tr : public AOWSGameplayAbilityTargetActor
{
	GENERATED_UCLASS_BODY()

public:
	virtual void EndPlay(const EEndPlayReason::Type EndPlayReason) override;

	/** Traces as normal, but will manually filter all hit actors */
	static void LineTraceWithFilter(FHitResult& OutHitResult, const UWorld* World, const FOWSGameplayTargetDataFilterHandle FilterHandle, const FVector& Start, const FVector& End, FName ProfileName, const FCollisionQueryParams Params);

	/** Sweeps as normal, but will manually filter all hit actors */
	static void SweepWithFilter(FHitResult& OutHitResult, const UWorld* World, const FOWSGameplayTargetDataFilterHandle FilterHandle, const FVector& Start, const FVector& End, const FQuat& Rotation, const FCollisionShape CollisionShape, FName ProfileName, const FCollisionQueryParams Params);

	void AimWithPlayerController(const AActor* InSourceActor, FCollisionQueryParams Params, const FVector& TraceStart, FVector& OutTraceEnd, bool bIgnorePitch = false) const;

	static bool ClipCameraRayToAbilityRange(FVector CameraLocation, FVector CameraDirection, FVector AbilityCenter, float AbilityRange, FVector& ClippedPosition);

	virtual void StartTargeting(UGameplayAbility* Ability) override;

	virtual void ConfirmTargetingAndContinue() override;

	virtual void Tick(float DeltaSeconds) override;

	UPROPERTY(BlueprintReadWrite, EditAnywhere, meta = (ExposeOnSpawn = true), Category = Trace)
		float MaxRange;

	UPROPERTY(BlueprintReadWrite, EditAnywhere, config, meta = (ExposeOnSpawn = true), Category = Trace)
		FCollisionProfileName TraceProfile;

	// Does the trace affect the aiming pitch
	UPROPERTY(BlueprintReadWrite, EditAnywhere, meta = (ExposeOnSpawn = true), Category = Trace)
		bool bTraceAffectsAimPitch;

protected:
	virtual FHitResult PerformTrace(AActor* InSourceActor) PURE_VIRTUAL(AGameplayAbilityTargetActor_Trace, return FHitResult(););

	FGameplayAbilityTargetDataHandle MakeTargetData(const FHitResult& HitResult) const;

	TWeakObjectPtr<AGameplayAbilityWorldReticle> ReticleActor;
};

