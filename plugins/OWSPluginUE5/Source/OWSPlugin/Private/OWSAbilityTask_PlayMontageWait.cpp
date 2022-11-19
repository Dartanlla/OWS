// Fill out your copyright notice in the Description page of Project Settings.

#include "OWSAbilityTask_PlayMontageWait.h"
#include "GameFramework/Character.h"
#include "AbilitySystemComponent.h"
#include "AbilitySystemGlobals.h"
#include "AbilitySystemLog.h"

UOWSAbilityTask_PlayMontageWait::UOWSAbilityTask_PlayMontageWait(const FObjectInitializer& ObjectInitializer)
	: Super(ObjectInitializer)
{
	Rate = 1.f;
	bStopWhenAbilityEnds = true;
}

void UOWSAbilityTask_PlayMontageWait::OnMontageBlendingOut(UAnimMontage* Montage, bool bInterrupted)
{
	if (Ability && Ability->GetCurrentMontage() == MontageToPlay)
	{
		if (Montage == MontageToPlay)
		{
			AbilitySystemComponent->ClearAnimatingAbility(Ability);

			// Reset AnimRootMotionTranslationScale
			ACharacter* Character = Cast<ACharacter>(GetAvatarActor());
			if (Character && (Character->GetLocalRole() == ROLE_Authority ||
				(Character->GetLocalRole() == ROLE_AutonomousProxy && Ability->GetNetExecutionPolicy() == EGameplayAbilityNetExecutionPolicy::LocalPredicted)))
			{
				Character->SetAnimRootMotionTranslationScale(1.f);
			}

		}
	}

	if (bInterrupted)
	{
		if (ShouldBroadcastAbilityTaskDelegates())
		{
			OnInterrupted.Broadcast();
		}
	}
	else
	{
		if (ShouldBroadcastAbilityTaskDelegates())
		{
			OnBlendOut.Broadcast();
		}
	}
}

void UOWSAbilityTask_PlayMontageWait::OnMontageInterrupted()
{
	if (StopPlayingMontage())
	{
		// Let the BP handle the interrupt as well
		if (ShouldBroadcastAbilityTaskDelegates())
		{
			OnInterrupted.Broadcast();
		}
	}
}

void UOWSAbilityTask_PlayMontageWait::OnMontageEnded(UAnimMontage* Montage, bool bInterrupted)
{
	if (!bInterrupted)
	{
		if (ShouldBroadcastAbilityTaskDelegates())
		{
			OnCompleted.Broadcast();
		}
	}

	EndTask();
}

UOWSAbilityTask_PlayMontageWait* UOWSAbilityTask_PlayMontageWait::CreatePlayMontageAndWaitProxy(UGameplayAbility* OwningAbility,
	FName TaskInstanceName, UAnimMontage *MontageToPlay, USkeletalMeshComponent* UseAlternateSKMC, float Rate, FName StartSection, 
	bool bStopWhenAbilityEnds, float AnimRootMotionTranslationScale, FName AnimNotifyName)
{

	UAbilitySystemGlobals::NonShipping_ApplyGlobalAbilityScaler_Rate(Rate);

	UOWSAbilityTask_PlayMontageWait* MyObj = NewAbilityTask<UOWSAbilityTask_PlayMontageWait>(OwningAbility, TaskInstanceName);
	MyObj->MontageToPlay = MontageToPlay;
	MyObj->UseAlternateSKMC = UseAlternateSKMC;
	MyObj->Rate = Rate;
	MyObj->StartSection = StartSection;
	MyObj->AnimRootMotionTranslationScale = AnimRootMotionTranslationScale;
	MyObj->bStopWhenAbilityEnds = bStopWhenAbilityEnds;
	MyObj->WaitForAnimNotifyName = AnimNotifyName;
	return MyObj;
}

void UOWSAbilityTask_PlayMontageWait::Activate()
{
	if (Ability == nullptr)
	{
		return;
	}

	bool bPlayedMontage = false;

	if (AbilitySystemComponent.Get())
	{
		const FGameplayAbilityActorInfo* ActorInfo = Ability->GetCurrentActorInfo();
		UAnimInstance* AnimInstance;
		if (UseAlternateSKMC)
		{ 
			AnimInstance = UseAlternateSKMC->GetAnimInstance();

			float Duration = AnimInstance->Montage_Play(MontageToPlay, Rate);
			if (Duration > 0)
			{
				bPlayedMontage = true;
			}
		}
		else
		{
			AnimInstance = ActorInfo->GetAnimInstance();

			if (AnimInstance != nullptr)
			{
				if (AbilitySystemComponent->PlayMontage(Ability, Ability->GetCurrentActivationInfo(), MontageToPlay, Rate, StartSection) > 0.f)
				{
					// Playing a montage could potentially fire off a callback into game code which could kill this ability! Early out if we are  pending kill.
					if (ShouldBroadcastAbilityTaskDelegates() == false)
					{
						return;
					}

					InterruptedHandle = Ability->OnGameplayAbilityCancelled.AddUObject(this, &UOWSAbilityTask_PlayMontageWait::OnMontageInterrupted);

					BlendingOutDelegate.BindUObject(this, &UOWSAbilityTask_PlayMontageWait::OnMontageBlendingOut);
					AnimInstance->Montage_SetBlendingOutDelegate(BlendingOutDelegate, MontageToPlay);

					MontageEndedDelegate.BindUObject(this, &UOWSAbilityTask_PlayMontageWait::OnMontageEnded);
					AnimInstance->Montage_SetEndDelegate(MontageEndedDelegate, MontageToPlay);

					if (WaitForAnimNotifyName != NAME_None)
					{
						AnimInstance->OnPlayMontageNotifyBegin.AddDynamic(this, &UOWSAbilityTask_PlayMontageWait::OnOWSNotifyBeginReceived);
					}

					ACharacter* Character = Cast<ACharacter>(GetAvatarActor());
					if (Character && (Character->GetLocalRole() == ROLE_Authority ||
						(Character->GetLocalRole() == ROLE_AutonomousProxy && Ability->GetNetExecutionPolicy() == EGameplayAbilityNetExecutionPolicy::LocalPredicted)))
					{
						Character->SetAnimRootMotionTranslationScale(AnimRootMotionTranslationScale);
					}

					bPlayedMontage = true;
				}
			}
			else
			{
				ABILITY_LOG(Warning, TEXT("UAbilityTask_PlayMontageAndWait call to PlayMontage failed!"));
			}
		}
	}
	else
	{
		ABILITY_LOG(Warning, TEXT("UAbilityTask_PlayMontageAndWait called on invalid AbilitySystemComponent"));
	}

	if (!bPlayedMontage)
	{
		ABILITY_LOG(Warning, TEXT("UAbilityTask_PlayMontageAndWait called in Ability %s failed to play montage %s; Task Instance Name %s."), *Ability->GetName(), *GetNameSafe(MontageToPlay), *InstanceName.ToString());
		if (ShouldBroadcastAbilityTaskDelegates())
		{
			OnCancelled.Broadcast();
		}
	}

	SetWaitingOnAvatar();
}

void UOWSAbilityTask_PlayMontageWait::ExternalCancel()
{
	check(AbilitySystemComponent.Get());

	if (ShouldBroadcastAbilityTaskDelegates())
	{
		OnCancelled.Broadcast();
	}
	Super::ExternalCancel();
}

void UOWSAbilityTask_PlayMontageWait::OnDestroy(bool AbilityEnded)
{
	// Note: Clearing montage end delegate isn't necessary since its not a multicast and will be cleared when the next montage plays.
	// (If we are destroyed, it will detect this and not do anything)

	// This delegate, however, should be cleared as it is a multicast
	if (Ability)
	{
		Ability->OnGameplayAbilityCancelled.Remove(InterruptedHandle);
		if (AbilityEnded && bStopWhenAbilityEnds)
		{
			StopPlayingMontage();
		}
	}

	const FGameplayAbilityActorInfo* ActorInfo = Ability->GetCurrentActorInfo();
	if (!ActorInfo)
	{
		Super::OnDestroy(AbilityEnded);
		return;
	}

	UAnimInstance* AnimInstance;
	if (UseAlternateSKMC)
	{
		AnimInstance = UseAlternateSKMC->GetAnimInstance();
	}
	else
	{
		AnimInstance = ActorInfo->GetAnimInstance();
	}

	if (AnimInstance != nullptr)
	{
		AnimInstance->OnPlayMontageNotifyBegin.RemoveDynamic(this, &UOWSAbilityTask_PlayMontageWait::OnOWSNotifyBeginReceived);
	}

	Super::OnDestroy(AbilityEnded);
}


void UOWSAbilityTask_PlayMontageWait::OnOWSNotifyBeginReceived(FName NotifyName, const FBranchingPointNotifyPayload& BranchingPointPayload)
{
	//UE_LOG(OWS, Verbose, TEXT("UOWSAbilityTask_PlayMontageWait::OnOWSNotifyBeginReceived - %s"), NotifyName.ToString());
	if (NotifyName == WaitForAnimNotifyName)
	{
		OnAnimNotify.Broadcast();
	}
}

bool UOWSAbilityTask_PlayMontageWait::StopPlayingMontage()
{
	const FGameplayAbilityActorInfo* ActorInfo = Ability->GetCurrentActorInfo();
	if (!ActorInfo)
	{
		return false;
	}

	UAnimInstance* AnimInstance;
	if (UseAlternateSKMC)
	{
		AnimInstance = UseAlternateSKMC->GetAnimInstance();
	}
	else
	{
		AnimInstance = ActorInfo->GetAnimInstance();
	}

	if (AnimInstance == nullptr)
	{
		return false;
	}

	// Check if the montage is still playing
	// The ability would have been interrupted, in which case we should automatically stop the montage
	if (AbilitySystemComponent.Get() && Ability)
	{
		if (AbilitySystemComponent->GetAnimatingAbility() == Ability
			&& AbilitySystemComponent->GetCurrentMontage() == MontageToPlay)
		{
			// Unbind delegates so they don't get called as well
			FAnimMontageInstance* MontageInstance = AnimInstance->GetActiveInstanceForMontage(MontageToPlay);
			if (MontageInstance)
			{
				MontageInstance->OnMontageBlendingOutStarted.Unbind();
				MontageInstance->OnMontageEnded.Unbind();
			}

			AbilitySystemComponent->CurrentMontageStop();
			return true;
		}
	}

	return false;
}

FString UOWSAbilityTask_PlayMontageWait::GetDebugString() const
{
	UAnimMontage* PlayingMontage = nullptr;
	if (Ability)
	{
		const FGameplayAbilityActorInfo* ActorInfo = Ability->GetCurrentActorInfo();

		UAnimInstance* AnimInstance;
		if (UseAlternateSKMC)
		{
			AnimInstance = UseAlternateSKMC->GetAnimInstance();
		}
		else
		{
			AnimInstance = ActorInfo->GetAnimInstance();
		}		

		if (AnimInstance != nullptr)
		{
			PlayingMontage = AnimInstance->Montage_IsActive(MontageToPlay) ? MontageToPlay : AnimInstance->GetCurrentActiveMontage();
		}
	}

	return FString::Printf(TEXT("PlayMontageAndWait. MontageToPlay: %s  (Currently Playing): %s"), *GetNameSafe(MontageToPlay), *GetNameSafe(PlayingMontage));
}
