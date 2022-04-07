// Copyright 1998-2018 Epic Games, Inc. All Rights Reserved.

#include "OWSTask_ApplyRootMotion_Base.h"
#include "AbilitySystemComponent.h"
#include "Net/UnrealNetwork.h"
#include "GameFramework/RootMotionSource.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "GameFramework/Character.h"

//FOWSOnTargetActorSwapped UOWSTask_ApplyRootMotion_Base::OnTargetActorSwapped;

UOWSTask_ApplyRootMotion_Base::UOWSTask_ApplyRootMotion_Base(const FObjectInitializer& ObjectInitializer)
	: Super(ObjectInitializer)
{
	bTickingTask = true;
	bSimulatedTask = true;

	ForceName = NAME_None;
	FinishVelocityMode = ERootMotionFinishVelocityMode::MaintainLastRootMotionVelocity;
	FinishSetVelocity = FVector::ZeroVector;
	FinishClampVelocity = 0.0f;
	MovementComponent = nullptr;
	RootMotionSourceID = (uint16)ERootMotionSourceID::Invalid;
	bIsFinished = false;
	StartTime = 0.0f;
	EndTime = 0.0f;
}

void UOWSTask_ApplyRootMotion_Base::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
	DOREPLIFETIME(UOWSTask_ApplyRootMotion_Base, ForceName);
	DOREPLIFETIME(UOWSTask_ApplyRootMotion_Base, FinishVelocityMode);
	DOREPLIFETIME(UOWSTask_ApplyRootMotion_Base, FinishSetVelocity);
	DOREPLIFETIME(UOWSTask_ApplyRootMotion_Base, FinishClampVelocity);
}

void UOWSTask_ApplyRootMotion_Base::InitSimulatedTask(UGameplayTasksComponent& InGameplayTasksComponent)
{
	Super::InitSimulatedTask(InGameplayTasksComponent);

	SharedInitAndApply();
}

bool UOWSTask_ApplyRootMotion_Base::HasTimedOut() const
{
	const TSharedPtr<FRootMotionSource> RMS = (MovementComponent ? MovementComponent->GetRootMotionSourceByID(RootMotionSourceID) : nullptr);
	if (!RMS.IsValid())
	{
		return true;
	}

	return RMS->Status.HasFlag(ERootMotionSourceStatusFlags::Finished);
}

