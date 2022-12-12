// Copyright 2017 Sabre Dart Studios

#include "OWSAbilityTask_WaitCastTime.h"
#include "TimerManager.h"
#include "AbilitySystemGlobals.h"
#include "AbilitySystemComponent.h"

UOWSAbilityTask_WaitCastTime::UOWSAbilityTask_WaitCastTime(const FObjectInitializer& ObjectInitializer)
	: Super(ObjectInitializer)
{
	CastTime = 0.f;
	TimeStarted = 0.f;
	RegisteredCallbacks = false;
}

UOWSAbilityTask_WaitCastTime* UOWSAbilityTask_WaitCastTime::RPGWaitCastTime(UGameplayAbility* OwningAbility, float CastTime)
{
	UAbilitySystemGlobals::NonShipping_ApplyGlobalAbilityScaler_Duration(CastTime);

	auto MyObj = NewAbilityTask<UOWSAbilityTask_WaitCastTime>(OwningAbility);
	MyObj->CastTime = CastTime;
	return MyObj;
}

void UOWSAbilityTask_WaitCastTime::OnCancelCallback()
{
	if (AbilitySystemComponent.Get())
	{
		AbilitySystemComponent->ConsumeGenericReplicatedEvent(EAbilityGenericReplicatedEvent::GenericCancel, GetAbilitySpecHandle(), GetActivationPredictionKey());
		Cancelled.Broadcast();
		EndTask();
	}
}

void UOWSAbilityTask_WaitCastTime::OnLocalCancelCallback()
{
	FScopedPredictionWindow ScopedPrediction(AbilitySystemComponent.Get(), IsPredictingClient());

	if (AbilitySystemComponent.Get() && IsPredictingClient())
	{
		AbilitySystemComponent->ServerSetReplicatedEvent(EAbilityGenericReplicatedEvent::GenericCancel, GetAbilitySpecHandle(), GetActivationPredictionKey(), AbilitySystemComponent->ScopedPredictionKey);
	}
	OnCancelCallback();
}

void UOWSAbilityTask_WaitCastTime::Activate()
{
	UWorld* World = GetWorld();
	TimeStarted = World->GetTimeSeconds();

	FTimerHandle TimerHandleClient;
	FTimerHandle TimerHandle;
	

	if (AbilitySystemComponent.Get())
	{
		const FGameplayAbilityActorInfo* Info = Ability->GetCurrentActorInfo();

		if (IsPredictingClient())
		{
			// We have to wait for the callback from the AbilitySystemComponent.
			AbilitySystemComponent.Get()->GenericLocalCancelCallbacks.AddDynamic(this, &UOWSAbilityTask_WaitCastTime::OnLocalCancelCallback);	// Tell me if the cancel input is pressed
			World->GetTimerManager().SetTimer(TimerHandleClient, this, &UOWSAbilityTask_WaitCastTime::OnCastFinishClient, CastTime, false);
			RegisteredCallbacks = true;
		}
		else
		{
			CallOrAddReplicatedDelegate(EAbilityGenericReplicatedEvent::GenericCancel, FSimpleMulticastDelegate::FDelegate::CreateUObject(this, &UOWSAbilityTask_WaitCastTime::OnCancelCallback));
			RegisteredCallbacks = true;
			World->GetTimerManager().SetTimer(TimerHandle, this, &UOWSAbilityTask_WaitCastTime::OnCastFinish, CastTime, false);
		}
	}
}

void UOWSAbilityTask_WaitCastTime::OnDestroy(bool AbilityEnded)
{
	if (RegisteredCallbacks && AbilitySystemComponent.Get())
	{
		AbilitySystemComponent->GenericLocalCancelCallbacks.RemoveDynamic(this, &UOWSAbilityTask_WaitCastTime::OnLocalCancelCallback);
	}

	Super::OnDestroy(AbilityEnded);
}

void UOWSAbilityTask_WaitCastTime::OnCastFinish()
{
	UE_LOG(LogTemp, Error, TEXT("OnCastFinish"));
	OnFinish.Broadcast();
	EndTask();
}

void UOWSAbilityTask_WaitCastTime::OnCastFinishClient()
{
	UE_LOG(LogTemp, Error, TEXT("OnCastFinishClient"));
	OnClientFinish.Broadcast();
	EndTask();
}

FString UOWSAbilityTask_WaitCastTime::GetDebugString() const
{
	float TimeLeft = CastTime - GetWorld()->TimeSince(TimeStarted);
	return FString::Printf(TEXT("WaitCastTime. Time: %.2f. TimeLeft: %.2f"), CastTime, TimeLeft);
}

