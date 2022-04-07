// Copyright 2018 Sabre Dart Studios

#pragma once

#include "CoreMinimal.h"
#include "UObject/ObjectMacros.h"
#include "OWSGameplayAbilityTargetActor_Tr.h"
#include "Engine/EngineTypes.h"
#include "OWSGameplayAbilityTargetActor_Ln.generated.h"

/**
 * 
 */
UCLASS()
class OWSPLUGIN_API AOWSGameplayAbilityTargetActor_Ln : public AOWSGameplayAbilityTargetActor_Tr
{
	GENERATED_UCLASS_BODY()	
	
protected:
	virtual FHitResult PerformTrace(AActor* InSourceActor) override;
	
};
