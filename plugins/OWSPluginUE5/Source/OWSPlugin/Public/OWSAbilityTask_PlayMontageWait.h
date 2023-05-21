// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "UObject/ObjectMacros.h"
#include "Animation/AnimInstance.h"
#include "Abilities/Tasks/AbilityTask.h"
#include "OWSAbilityTask_PlayMontageWait.generated.h"

DECLARE_DYNAMIC_MULTICAST_DELEGATE(FMontageWaitSimpleDelegate2);

/**
 * 
 */
UCLASS()
class OWSPLUGIN_API UOWSAbilityTask_PlayMontageWait : public UAbilityTask
{
	GENERATED_UCLASS_BODY()

	UPROPERTY(BlueprintAssignable)
		FMontageWaitSimpleDelegate2	OnCompleted;

	UPROPERTY(BlueprintAssignable)
		FMontageWaitSimpleDelegate2	OnBlendOut;

	UPROPERTY(BlueprintAssignable)
		FMontageWaitSimpleDelegate2	OnInterrupted;

	UPROPERTY(BlueprintAssignable)
		FMontageWaitSimpleDelegate2	OnCancelled;

	UPROPERTY(BlueprintAssignable)
		FMontageWaitSimpleDelegate2	OnAnimNotify;

	UFUNCTION()
		void OnMontageBlendingOut(UAnimMontage* Montage, bool bInterrupted);

	UFUNCTION()
		void OnMontageInterrupted();

	UFUNCTION()
		void OnMontageEnded(UAnimMontage* Montage, bool bInterrupted);

	UFUNCTION()
		void OnOWSNotifyBeginReceived(FName NotifyName, const FBranchingPointNotifyPayload& BranchingPointPayload);

	UFUNCTION(BlueprintCallable, Category = "Ability|Tasks", meta = (DisplayName = "OWSPlayMontageAndWait",
		HidePin = "OwningAbility", DefaultToSelf = "OwningAbility", BlueprintInternalUseOnly = "TRUE"))
		static UOWSAbilityTask_PlayMontageWait* CreatePlayMontageAndWaitProxy(UGameplayAbility* OwningAbility,
			FName TaskInstanceName, UAnimMontage* MontageToPlay, USkeletalMeshComponent* UseAlternateSKMC, float Rate = 1.f, FName StartSection = NAME_None, bool bStopWhenAbilityEnds = true, float AnimRootMotionTranslationScale = 1.f,
			FName AnimNotifyName = NAME_None);

	virtual void Activate() override;

	/** Called when the ability is asked to cancel from an outside node. What this means depends on the individual task. By default, this does nothing other than ending the task. */
	virtual void ExternalCancel() override;

	virtual FString GetDebugString() const override;

private:

	virtual void OnDestroy(bool AbilityEnded) override;

	/** Checks if the ability is playing a montage and stops that montage, returns true if a montage was stopped, false if not. */
	bool StopPlayingMontage();

	FOnMontageBlendingOutStarted BlendingOutDelegate;
	FOnMontageEnded MontageEndedDelegate;
	FDelegateHandle InterruptedHandle;
	FPlayMontageAnimNotifyDelegate AnimNotifyDelegate;

	UPROPERTY()
		UAnimMontage* MontageToPlay;

	UPROPERTY()
		USkeletalMeshComponent* UseAlternateSKMC;

	UPROPERTY()
		float Rate;

	UPROPERTY()
		FName StartSection;

	UPROPERTY()
		float AnimRootMotionTranslationScale;

	UPROPERTY()
		bool bStopWhenAbilityEnds;
	
	UPROPERTY()
		FName WaitForAnimNotifyName;
};




