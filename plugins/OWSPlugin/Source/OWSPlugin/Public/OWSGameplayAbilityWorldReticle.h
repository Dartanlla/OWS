// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "UObject/ObjectMacros.h"
#include "Abilities/GameplayAbilityWorldReticle.h"
#include "OWSGameplayAbilityWorldReticle.generated.h"

class AGameplayAbilityTargetActor;
class UMaterialInterface;
/**
 * 
 */
UCLASS()
class OWSPLUGIN_API AOWSGameplayAbilityWorldReticle : public AGameplayAbilityWorldReticle
{
	GENERATED_UCLASS_BODY()

public:
	void InitializeReticleVisualizationInformation(AGameplayAbilityTargetActor* InTargetingActor, AActor* VisualizationActor, UMaterialInterface *VisualizationMaterial);

private:
	/** Hardcoded collision component, so other objects don't think they can collide with the visualization actor */
	//DEPRECATED_FORGAME(4.6, "CollisionComponent should not be accessed directly, please use GetCollisionComponent() function instead. CollisionComponent will soon be private and your code will not compile.")
		UPROPERTY()
		class UCapsuleComponent* CollisionComponent;
public:

	UPROPERTY()
		TArray<UActorComponent*> VisualizationComponents;

	/** Overridable function called whenever this actor is being removed from a level */
	virtual void EndPlay(const EEndPlayReason::Type EndPlayReason) override;

	/** Returns CollisionComponent subobject **/
	UCapsuleComponent* GetCollisionComponent();
	
	
};
