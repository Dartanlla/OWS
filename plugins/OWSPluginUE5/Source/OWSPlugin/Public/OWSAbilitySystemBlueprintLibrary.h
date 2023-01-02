// Copyright 2018 Sabre Dart Studios

#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "GameplayEffectAggregator.h"
#include "GameplayEffect.h"
#include "AbilitySystemGlobals.h"
#include "AbilitySystemComponent.h"
#include "AbilitySystemInterface.h"
#include "OWSGameplayAbilityTargetDataFilter.h"
#include "OWSAbilitySystemBlueprintLibrary.generated.h"


USTRUCT(BlueprintType)
struct FGameplayAbilityCastingTargetingLocationInfo : public FGameplayAbilityTargetData
{
	GENERATED_USTRUCT_BODY()

	/** Amount of time the ability has been charged */
	UPROPERTY(BlueprintReadWrite, EditAnywhere, Category = Targeting)
		float ChargeTime = 0.f;

	/** The ID of the Ability that is performing targeting */
	UPROPERTY()
		uint32 UniqueID = -1;

	/** Generic location data for source */
	UPROPERTY(BlueprintReadWrite, EditAnywhere, Category = Targeting)
		FGameplayAbilityTargetingLocationInfo SourceLocation;

	/** Generic location data for target */
	UPROPERTY(BlueprintReadWrite, EditAnywhere, Category = Targeting)
		FGameplayAbilityTargetingLocationInfo TargetLocation;

	// -------------------------------------

	FGameplayAbilityCastingTargetingLocationInfo()
	{

	}

	virtual bool HasOrigin() const override
	{
		return true;
	}

	virtual FTransform GetOrigin() const override
	{
		return SourceLocation.GetTargetingTransform();
	}

	// -------------------------------------

	virtual bool HasEndPoint() const override
	{
		return true;
	}

	virtual FVector GetEndPoint() const override
	{
		return TargetLocation.GetTargetingTransform().GetLocation();
	}

	virtual FTransform GetEndPointTransform() const override
	{
		return TargetLocation.GetTargetingTransform();
	}

	// -------------------------------------

	virtual UScriptStruct* GetScriptStruct() const override
	{
		return FGameplayAbilityCastingTargetingLocationInfo::StaticStruct();
	}

	virtual FString ToString() const override
	{
		return TEXT("FGameplayAbilityCastingTargetingLocationInfo");
	}

	bool NetSerialize(FArchive& Ar, class UPackageMap* Map, bool& bOutSuccess);
};

template<>
struct TStructOpsTypeTraits<FGameplayAbilityCastingTargetingLocationInfo> : public TStructOpsTypeTraitsBase2<FGameplayAbilityCastingTargetingLocationInfo>
{
	enum
	{
		WithNetSerializer = true	// For now this is REQUIRED for FGameplayAbilityTargetDataHandle net serialization to work
	};
};

/**
 * 
 */
UCLASS()
class OWSPLUGIN_API UOWSAbilitySystemBlueprintLibrary : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()
	
public:	
	UFUNCTION(BlueprintPure, Category = "Ability|TargetData")
		static FGameplayAbilityTargetDataHandle FilterTargetData(const FGameplayAbilityTargetDataHandle& TargetDataHandle, FOWSGameplayTargetDataFilterHandle FilterHandle);

	UFUNCTION(BlueprintPure, Category = "Filter")
		static FOWSGameplayTargetDataFilterHandle MakeFilterHandle(FOWSGameplayTargetDataFilter Filter, AActor* FilterActor);
	
	UFUNCTION(BlueprintPure, Category = "Ability|TargetData")
		static float GetOWSChargeTimeFromTargetData(const FGameplayAbilityTargetDataHandle& TargetData, int32 Index);
};
