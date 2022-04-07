// Copyright 2018 Sabre Dart Studios

#pragma once

#include "CoreMinimal.h"
#include "Abilities/GameplayAbilityTargetActor.h"
#include "OWSGameplayAbilityTargetDatafilter.h"
#include "OWSGameplayAbilityTargetActor.generated.h"

/**
 * 
 */
UCLASS()
class OWSPLUGIN_API AOWSGameplayAbilityTargetActor : public AGameplayAbilityTargetActor
{
	GENERATED_BODY()
	
public:
	UPROPERTY(BlueprintReadWrite, Replicated, meta = (ExposeOnSpawn = true), Category = Targeting)
	FOWSGameplayTargetDataFilterHandle OWSFilter;
	
	
};
