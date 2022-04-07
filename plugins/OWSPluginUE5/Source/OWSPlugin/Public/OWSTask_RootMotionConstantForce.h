// Copyright 2018 Sabre Dart Studios

#pragma once

#include "CoreMinimal.h"
#include "UObject/ObjectMacros.h"
#include "Abilities/Tasks/AbilityTask_ApplyRootMotionConstantForce.h"
#include "Abilities/Tasks/AbilityTask_ApplyRootMotion_Base.h"
#include "OWSTask_RootMotionConstantForce.generated.h"

class UCharacterMovementComponent;
class UCurveFloat;
class UGameplayTasksComponent;

DECLARE_DYNAMIC_MULTICAST_DELEGATE(FOWSApplyRootMotionConstantForceDelegate);

class AActor;

/**
 * 
 */
UCLASS()
class OWSPLUGIN_API UOWSTask_RootMotionConstantForce : public UOWSTask_ApplyRootMotion_Base
{
	GENERATED_UCLASS_BODY()

	UPROPERTY(BlueprintAssignable)
		FOWSApplyRootMotionConstantForceDelegate OnFinish;

	/** Apply force to character's movement */
	UFUNCTION(BlueprintCallable, Category = "Ability|Tasks", meta = (HidePin = "OwningAbility", DefaultToSelf = "OwningAbility", BlueprintInternalUseOnly = "TRUE"))
		static UOWSTask_RootMotionConstantForce* OWSApplyRootMotionConstantForce
		(
			UGameplayAbility* OwningAbility,
			FName TaskInstanceName,
			FVector WorldDirection,
			float Strength,
			float Duration,
			bool bIsAdditive,
			UCurveFloat* StrengthOverTime,
			ERootMotionFinishVelocityMode VelocityOnFinishMode,
			FVector SetVelocityOnFinish,
			float ClampVelocityOnFinish
		);

	/** Tick function for this task, if bTickingTask == true */
	virtual void TickTask(float DeltaTime) override;

	virtual void PreDestroyFromReplication() override;
	virtual void OnDestroy(bool AbilityIsEnding) override;

protected:

	virtual void SharedInitAndApply() override;

protected:

	UPROPERTY(Replicated)
		FVector WorldDirection;

	UPROPERTY(Replicated)
		float Strength;

	UPROPERTY(Replicated)
		float Duration;

	UPROPERTY(Replicated)
		bool bIsAdditive;

	/**
	*  Strength of the force over time
	*  Curve Y is 0 to 1 which is percent of full Strength parameter to apply
	*  Curve X is 0 to 1 normalized time if this force has a limited duration (Duration > 0), or
	*          is in units of seconds if this force has unlimited duration (Duration < 0)
	*/
	UPROPERTY(Replicated)
		UCurveFloat* StrengthOverTime;
	
};
