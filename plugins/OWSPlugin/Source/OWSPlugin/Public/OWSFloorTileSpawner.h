// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "OWSFloorTileSpawner.generated.h"

UCLASS()
class OWSPLUGIN_API AOWSFloorTileSpawner : public AActor
{
	GENERATED_BODY()
	
public:	
	// Sets default values for this actor's properties
	AOWSFloorTileSpawner();

	UPROPERTY(BlueprintReadWrite, EditAnywhere)
		int NumberToSpawnX;

	UPROPERTY(BlueprintReadWrite, EditAnywhere)
		int NumberToSpawnY;

	UPROPERTY(BlueprintReadWrite, EditDefaultsOnly)
		USceneComponent* RootComp;

	UPROPERTY(BlueprintReadWrite, EditAnywhere)
		UStaticMesh* PrimaryStaticMeshToSpawn;

	UPROPERTY(BlueprintReadWrite, EditAnywhere)
		float DistanceBetweenMeshes;

	UPROPERTY(BlueprintReadWrite, EditAnywhere)
		float FixedRotationPerMesh;

	UPROPERTY(BlueprintReadWrite, EditAnywhere)
		float FixedRotationPerMesh2;

	UPROPERTY(BlueprintReadWrite, EditAnywhere)
		float RandomZVariance;

	UPROPERTY(BlueprintReadWrite, EditAnywhere)
		TArray<int> TilesNotToSpawn;

	UPROPERTY(BlueprintReadWrite, EditAnywhere)
		int RandomSeed;

protected:
	// Called when the game starts or when spawned
	virtual void BeginPlay() override;

public:	
	// Called every frame
	virtual void OnConstruction(const FTransform& Transform) override;
};
