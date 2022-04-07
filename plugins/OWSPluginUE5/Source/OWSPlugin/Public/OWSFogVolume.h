// Copyright 2018 Sabre Dart Studios

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Volume.h"
#include "Runtime/Core/Public/Templates/SharedPointer.h"
#include "Runtime/Engine/Classes/Engine/ExponentialHeightFog.h"
#include "OWSFogVolume.generated.h"

/**
 * 
 */
UCLASS(Blueprintable, BlueprintType)
class OWSPLUGIN_API AOWSFogVolume : public AVolume
{
	GENERATED_BODY()

public:

	AOWSFogVolume();

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Fog)
		float MaxFogExtinction;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Fog)
		float MinFogExtinction;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Fog)
		float TransitionRange;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Fog)
		AExponentialHeightFog* ExponentialHeightFog;

/*
	UFUNCTION(BlueprintCallable)
		float DistanceToPoint(const FVector& Point);
*/

	virtual void BeginPlay() override;
	virtual void Tick(float DeltaSeconds) override;
};
