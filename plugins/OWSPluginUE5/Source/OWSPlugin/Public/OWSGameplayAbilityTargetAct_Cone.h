// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "UObject/ObjectMacros.h"
#include "Abilities/GameplayAbilityTargetTypes.h"
#include "Abilities/GameplayAbilityTargetActor.h"
#include "OWSGameplayAbilityTargetAct_Cone.generated.h"

class UGameplayAbility;

/**
 * 
 */
UCLASS(Blueprintable, notplaceable)
class OWSPLUGIN_API AOWSGameplayAbilityTargetAct_Cone : public AGameplayAbilityTargetActor
{
	GENERATED_UCLASS_BODY()
	
public:
	/** Radius of target acquisition around the ability's start location. */
	UPROPERTY(BlueprintReadWrite, EditAnywhere, meta = (ExposeOnSpawn = true), Category = Radius)
		float Radius;

	UPROPERTY(BlueprintReadWrite, EditAnywhere, meta = (ExposeOnSpawn = true), Category = Settings)
		float HalfAngle;

	UPROPERTY(BlueprintReadWrite, EditAnywhere, meta = (ExposeOnSpawn = true), Category = Settings)
		FVector ForwardVector;

	virtual void StartTargeting(UGameplayAbility* Ability) override;

	virtual void ConfirmTargetingAndContinue() override;

protected:
	TArray<TWeakObjectPtr<AActor>>	PerformOverlap(const FVector& Origin);

	FGameplayAbilityTargetDataHandle MakeTargetData(const TArray<TWeakObjectPtr<AActor>>& Actors, const FVector& Origin) const;
};
