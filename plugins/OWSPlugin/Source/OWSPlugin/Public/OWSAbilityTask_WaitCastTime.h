// Copyright 2017 Sabre Dart Studios

#pragma once

#include "CoreMinimal.h"
#include "UObject/ObjectMacros.h"
#include "Abilities/Tasks/AbilityTask.h"
#include "OWSAbilityTask_WaitCastTime.generated.h"

DECLARE_DYNAMIC_MULTICAST_DELEGATE(FWaitDelayDelegate);
DECLARE_DYNAMIC_MULTICAST_DELEGATE(FWaitCancelDelegate);
/**
 * 
 */
UCLASS()
class OWSPLUGIN_API UOWSAbilityTask_WaitCastTime : public UAbilityTask
{
	GENERATED_UCLASS_BODY()

	UPROPERTY(BlueprintAssignable)
		FWaitDelayDelegate	OnFinish;

	UPROPERTY(BlueprintAssignable)
		FWaitDelayDelegate	OnClientFinish;

	UPROPERTY(BlueprintAssignable)
		FWaitCancelDelegate	Cancelled;

	UFUNCTION()
		void OnCancelCallback();

	UFUNCTION()
		void OnLocalCancelCallback();

	virtual void Activate() override;

	/** Return debug string describing task */
	virtual FString GetDebugString() const override;

	/** Wait specified time. This is functionally the same as a standard Delay node. */
	UFUNCTION(BlueprintCallable, Category = "Ability|Tasks", meta = (HidePin = "OwningAbility", DefaultToSelf = "OwningAbility", BlueprintInternalUseOnly = "TRUE"))
		static UOWSAbilityTask_WaitCastTime* RPGWaitCastTime(UGameplayAbility* OwningAbility, float CastTime);

private:

	void OnCastFinish();
	void OnCastFinishClient();

	virtual void OnDestroy(bool AbilityEnded) override;
	float CastTime;
	float TimeStarted;
	
	bool RegisteredCallbacks;
};
