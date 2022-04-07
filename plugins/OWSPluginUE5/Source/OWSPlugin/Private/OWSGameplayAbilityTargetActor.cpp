// Copyright 2018 Sabre Dart Studios

#include "OWSGameplayAbilityTargetActor.h"
#include "GameplayAbilitySpec.h"
#include "GameFramework/PlayerController.h"
#include "AbilitySystemComponent.h"
#include "Net/UnrealNetwork.h"

void AOWSGameplayAbilityTargetActor::GetLifetimeReplicatedProps(TArray< FLifetimeProperty > & OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);

	DOREPLIFETIME(AOWSGameplayAbilityTargetActor, OWSFilter);
}

