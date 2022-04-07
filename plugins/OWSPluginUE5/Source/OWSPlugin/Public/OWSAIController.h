// Copyright 2018 Sabre Dart Studios

#pragma once

#include "CoreMinimal.h"
#include "UObject/ObjectMacros.h"
#include "UObject/UObjectGlobals.h"
#include "AIController.h"
#include "OWSAIController.generated.h"

/**
 * 
 */
UCLASS()
class OWSPLUGIN_API AOWSAIController : public AAIController
{
	GENERATED_BODY()

		virtual FGenericTeamId GetGenericTeamId() const override;

protected:
	int32 _TeamNumber;

public:
	AOWSAIController(const FObjectInitializer& ObjectInitializer = FObjectInitializer::Get());

	UFUNCTION(BlueprintCallable, Category = "Teams")
	void SetTeamNumber(const int32 TeamNumber);
};
