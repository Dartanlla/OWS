// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "GameplayModMagnitudeCalculation.h"
#include "OWSAttributeSet.h"
#include "OWSModMagnitudeCalculation.generated.h"

/**
 * 
 */
UCLASS()
class OWSPLUGIN_API UOWSModMagnitudeCalculation : public UGameplayModMagnitudeCalculation
{
	GENERATED_UCLASS_BODY()

public:
	UFUNCTION(BlueprintCallable, Category = "Calculation")
		float GetAttributeCapture(const FGameplayEffectSpec& Spec, const FGameplayAttribute Attribute) const;

	
	
	
	
};
