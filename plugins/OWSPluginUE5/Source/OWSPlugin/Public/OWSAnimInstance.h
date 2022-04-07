// Copyright 2021 Sabre Dart Studios

#pragma once

#include "CoreMinimal.h"
#include "Animation/AnimInstance.h"
#include "OWSAnimInstance.generated.h"

/**
 * 
 */
UCLASS()
class OWSPLUGIN_API UOWSAnimInstance : public UAnimInstance
{
	GENERATED_BODY()

public:

	UFUNCTION(BlueprintCallable, Category = "Movement")
		float GetStartTimeByDistance(UAnimSequence* AnimSequence, float distance);


	
};
