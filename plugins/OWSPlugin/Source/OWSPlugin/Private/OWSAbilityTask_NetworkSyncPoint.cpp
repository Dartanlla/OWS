// Copyright 2017 Sabre Dart Studios

#include "OWSAbilityTask_NetworkSyncPoint.h"
#include "AbilitySystemComponent.h"

UOWSAbilityTask_NetworkSyncPoint::UOWSAbilityTask_NetworkSyncPoint(const FObjectInitializer& ObjectInitializer)
	: Super(ObjectInitializer)
{
	ReplicatedEventToListenFor = EAbilityGenericReplicatedEvent::MAX;
}

void UOWSAbilityTask_NetworkSyncPoint::OnSignalCallback()
{
	if (AbilitySystemComponent)
	{
		AbilitySystemComponent->ConsumeGenericReplicatedEvent(ReplicatedEventToListenFor, GetAbilitySpecHandle(), GetActivationPredictionKey());
	}
	SyncFinished();
}

UOWSAbilityTask_NetworkSyncPoint* UOWSAbilityTask_NetworkSyncPoint::RPGWaitNetSync(class UGameplayAbility* OwningAbility, ERPGAbilityTaskNetSyncType InSyncType)
{
	auto MyObj = NewAbilityTask<UOWSAbilityTask_NetworkSyncPoint>(OwningAbility);
	MyObj->SyncType = InSyncType;
	return MyObj;
}

void UOWSAbilityTask_NetworkSyncPoint::Activate()
{
	FScopedPredictionWindow ScopedPrediction(AbilitySystemComponent, IsPredictingClient());

	if (AbilitySystemComponent)
	{
		if (IsPredictingClient())
		{
			if (SyncType != ERPGAbilityTaskNetSyncType::OnlyServerWait)
			{
				// As long as we are waiting (!= OnlyServerWait), listen for the GenericSignalFromServer event
				ReplicatedEventToListenFor = EAbilityGenericReplicatedEvent::GenericSignalFromServer;
			}
			if (SyncType != ERPGAbilityTaskNetSyncType::OnlyClientWait)
			{
				// As long as the server is waiting (!= OnlyClientWait), send the Server and RPC for this signal
				AbilitySystemComponent->ServerSetReplicatedEvent(EAbilityGenericReplicatedEvent::GenericSignalFromClient, GetAbilitySpecHandle(), GetActivationPredictionKey(), AbilitySystemComponent->ScopedPredictionKey);
			}

		}
		//else if (IsForRemoteClient())
		else if (!IsPredictingClient())
		{
			if (SyncType != ERPGAbilityTaskNetSyncType::OnlyClientWait)
			{
				// As long as we are waiting (!= OnlyClientWait), listen for the GenericSignalFromClient event
				ReplicatedEventToListenFor = EAbilityGenericReplicatedEvent::GenericSignalFromClient;
			}
			if (SyncType != ERPGAbilityTaskNetSyncType::OnlyServerWait)
			{
				// As long as the client is waiting (!= OnlyServerWait), send the Server and RPC for this signal
				AbilitySystemComponent->ClientSetReplicatedEvent(EAbilityGenericReplicatedEvent::GenericSignalFromServer, GetAbilitySpecHandle(), GetActivationPredictionKey());
			}
		}

		if (ReplicatedEventToListenFor != EAbilityGenericReplicatedEvent::MAX)
		{
			CallOrAddReplicatedDelegate(ReplicatedEventToListenFor, FSimpleMulticastDelegate::FDelegate::CreateUObject(this, &UOWSAbilityTask_NetworkSyncPoint::OnSignalCallback));
		}
		else
		{
			// We aren't waiting for a replicated event, so the sync is complete.
			SyncFinished();
		}
	}
}

void UOWSAbilityTask_NetworkSyncPoint::OnDestroy(bool AbilityEnded)
{
	Super::OnDestroy(AbilityEnded);
}

void UOWSAbilityTask_NetworkSyncPoint::SyncFinished()
{
	if (!IsPendingKill())
	{
		OnSync.Broadcast();
		EndTask();
	}
}
