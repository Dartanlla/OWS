// Fill out your copyright notice in the Description page of Project Settings.


#include "OWSFloorTileSpawner.h"

// Sets default values
AOWSFloorTileSpawner::AOWSFloorTileSpawner()
{
 	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = false;

	RootComp = CreateDefaultSubobject<USceneComponent>(TEXT("Root"));
	SetRootComponent(RootComp);
}

void AOWSFloorTileSpawner::OnConstruction(const FTransform& Transform)
{
	Super::OnConstruction(Transform);

	UInstancedStaticMeshComponent* PrimaryISM = NewObject<UInstancedStaticMeshComponent>(this, FName("PrimaryISM"));

	if (PrimaryISM)
	{
		PrimaryISM->AttachToComponent(RootComponent, FAttachmentTransformRules::KeepRelativeTransform);
		PrimaryISM->RegisterComponent();
		PrimaryISM->SetStaticMesh(PrimaryStaticMeshToSpawn);
		PrimaryISM->SetIsReplicated(true);

		for (int x = 0; x < NumberToSpawnX; x++)
		{
			for (int y = 0; y < NumberToSpawnY; y++)
			{
				int CurrentTileNumber = (y * NumberToSpawnX) + x;

				if (TilesNotToSpawn.Find(CurrentTileNumber) == INDEX_NONE)
				{
					FTransform InstanceToAddTransform;
					FVector InstanceToAddVector;
					InstanceToAddVector.X = x * DistanceBetweenMeshes;
					InstanceToAddVector.Y = y * DistanceBetweenMeshes;
					InstanceToAddVector.Z = 0.f;
					InstanceToAddTransform.SetLocation(InstanceToAddVector);
					FRotator InstanceToAddRotator;
					InstanceToAddRotator.Yaw = (CurrentTileNumber * FixedRotationPerMesh) + FixedRotationPerMesh2;
					InstanceToAddTransform.SetRotation(InstanceToAddRotator.Quaternion());
					PrimaryISM->AddInstance(InstanceToAddTransform);
				}
			}
		}
	}
}

// Called when the game starts or when spawned
void AOWSFloorTileSpawner::BeginPlay()
{
	Super::BeginPlay();	
	
}

