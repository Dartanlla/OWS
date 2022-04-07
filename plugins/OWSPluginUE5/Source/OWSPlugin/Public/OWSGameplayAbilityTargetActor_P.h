// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "UObject/ObjectMacros.h"
#include "Abilities/GameplayAbilityTargetActor_GroundTrace.h"
#include "OWSGameplayAbilityWorldReticle.h"
#include "OWSGameplayAbilityTargetActor_P.generated.h"

//class AGameplayAbilityWorldReticle_ActorVisualization;
class UGameplayAbility;

/**
 * 
 */
UCLASS(Blueprintable)
class OWSPLUGIN_API AOWSGameplayAbilityTargetActor_P : public AGameplayAbilityTargetActor_GroundTrace
{
	GENERATED_UCLASS_BODY()

	virtual void EndPlay(const EEndPlayReason::Type EndPlayReason) override;

	virtual void StartTargeting(UGameplayAbility* InAbility) override;

	/** Actor we intend to place. */
	UPROPERTY(BlueprintReadWrite, EditAnywhere, meta = (ExposeOnSpawn = true), Category = Targeting)
		UClass* PlacedActorClass;		//Using a special class for replication purposes. (Not implemented yet)

										/** Override Material 0 on our placed actor's meshes with this material for visualization. */
	UPROPERTY(BlueprintReadWrite, EditAnywhere, meta = (ExposeOnSpawn = true), Category = Projectile)
		UMaterialInterface *PlacedActorMaterial;

	UPROPERTY(BlueprintReadWrite, EditAnywhere, meta = (ExposeOnSpawn = true), Category = Targeting)
		float MinimumTargetingDistance;

	UPROPERTY(BlueprintReadWrite, EditAnywhere, meta = (ExposeOnSpawn = true), Category = Targeting)
		bool AimAtMouseCursor;

	/** Visualization for the intended location of the placed actor. */
	TWeakObjectPtr<AOWSGameplayAbilityWorldReticle> ActorVisualizationReticle;
	
	UFUNCTION(BlueprintCallable, Category = "Targeting")
		void ConfirmTarget();
	UFUNCTION(BlueprintCallable, Category = "Targeting")
		void CancelTarget();

	void AimWithPlayerController(const AActor* InSourceActor, FCollisionQueryParams Params, const FVector& TraceStart, FVector& OutTraceEnd, bool bIgnorePitch) const;
	FHitResult PerformTrace(AActor* InSourceActor);
	void ConfirmTargeting();
	void BindToConfirmCancelInputs();
	static bool ClipCameraRayToAbilityRange(FVector CameraLocation, FVector CameraDirection, FVector AbilityCenter, float AbilityRange, float MinimumTargetingDistance, FVector& ClippedPosition);
};
