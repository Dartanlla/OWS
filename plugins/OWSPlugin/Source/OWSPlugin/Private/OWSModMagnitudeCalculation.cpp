// Fill out your copyright notice in the Description page of Project Settings.

#include "OWSModMagnitudeCalculation.h"


UOWSModMagnitudeCalculation::UOWSModMagnitudeCalculation(const FObjectInitializer& ObjectInitializer)
	: Super(ObjectInitializer)
{

}


float UOWSModMagnitudeCalculation::GetAttributeCapture(const FGameplayEffectSpec& Spec, const FGameplayAttribute Attribute) const
{
	float Magnitude = 0.f;

	FAggregatorEvaluateParameters EvaluationParameters;
	EvaluationParameters.SourceTags = Spec.CapturedSourceTags.GetAggregatedTags();
	EvaluationParameters.TargetTags = Spec.CapturedTargetTags.GetAggregatedTags();

	const FGameplayEffectAttributeCaptureDefinition* MatchingDef = RelevantAttributesToCapture.FindByPredicate(
		[&](const FGameplayEffectAttributeCaptureDefinition& Element)
	{
		return Element.AttributeToCapture == Attribute;
	});

	//*RelevantAttributesToCapture.GetData()
	if (MatchingDef)
	{
		GetCapturedAttributeMagnitude(*MatchingDef, Spec, EvaluationParameters, Magnitude);
	}

	return Magnitude;
}