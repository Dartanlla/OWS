// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "GameFramework/Actor.h"
#include "OWSPlayerControllerComponent.h"
#include "Runtime/Online/HTTP/Public/Http.h"
#include "OWSTravelToMapActor.generated.h"

UENUM(BlueprintType)
enum DynamicAxisType
{
	XAxis UMETA(DisplayName = "X Axis"),
	YAxis UMETA(DisplayName = "Y Axis")		
};

UCLASS()
class OWSPLUGIN_API AOWSTravelToMapActor : public AActor
{
	GENERATED_BODY()

	FHttpModule* Http;
	
public:	
	// Sets default values for this actor's properties
	AOWSTravelToMapActor();

	// Called when the game starts or when spawned
	virtual void BeginPlay() override;
	
	// Called every frame
	virtual void Tick( float DeltaSeconds ) override;

	UPROPERTY()
		UOWSPlayerControllerComponent* OWSPlayerControllerComponent;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Map")
		FString ZoneName;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Map")
		FVector LocationOnMap;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Map")
		FRotator StartingRotation;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Map")
		bool UseDynamicSpawnLocation;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Map")
		bool InvertDynamicAxisOffset;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Map")
		TEnumAsByte<DynamicAxisType> DynamicSpawnAxis;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Map")
		bool UseDynamicSpawnRotation;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Map")
		FRotator DynamicSpawnRotationOffeset;

	UFUNCTION(BlueprintCallable, Category = "Travel")
		void GetMapServerToTravelTo(APlayerController* PlayerController, TEnumAsByte<ERPGSchemeToChooseMap::SchemeToChooseMap> SelectedSchemeToChooseMap, int32 WorldServerID);

	UFUNCTION(BlueprintCallable, Category = "Math")
		FVector2D GetSphericalFromCartesian(FVector CartesianVector);

	UFUNCTION(BlueprintCallable, Category = "Math")
		FVector GetCartesianFromSpherical(FVector2D ShpericalVector);

	UFUNCTION(BlueprintImplementableEvent, Category = "Travel")
		void NotifyMapServerToTravelTo(const FString &ServerAndPort);
	UFUNCTION(BlueprintImplementableEvent, Category = "Travel")
		void ErrorMapServerToTravelTo(const FString &ErrorMsg);
	
};
