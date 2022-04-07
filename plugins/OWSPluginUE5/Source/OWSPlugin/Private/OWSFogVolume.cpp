// Copyright 2018 Sabre Dart Studios


#include "OWSFogVolume.h"
#include "Runtime/Engine/Classes/GameFramework/Character.h"
#include "Runtime/Engine/Classes/Kismet/GameplayStatics.h"
#include "Runtime/Engine/Classes/Components/ExponentialHeightFogComponent.h"
#include "Components/BrushComponent.h"

AOWSFogVolume::AOWSFogVolume()
{
	PrimaryActorTick.bCanEverTick = true;
	PrimaryActorTick.bStartWithTickEnabled = true;
	//Don't tick on the server.  This is client side only
	PrimaryActorTick.bAllowTickOnDedicatedServer = false;
	PrimaryActorTick.bTickEvenWhenPaused = true;
	PrimaryActorTick.TickGroup = TG_DuringPhysics;
}

void AOWSFogVolume::BeginPlay()
{
	// Call the base class  
	Super::BeginPlay();
	
	SetActorTickEnabled(true);
	SetActorTickInterval(0.5f);

	TArray<AActor*> FoundActors;
	UGameplayStatics::GetAllActorsOfClass(GetWorld(), AExponentialHeightFog::StaticClass(), FoundActors);

	if (FoundActors.Num() > 0)
	{
		ExponentialHeightFog = Cast<AExponentialHeightFog>(FoundActors[0]);
	}
}

/*
float AOWSFogVolume::DistanceToPoint(const FVector& Point)
{
	float OutDistanceToPoint = -1.f;

	if (GetBrushComponent())
	{
#if WITH_PHYSX
		FVector ClosestPoint;
		float DistanceSqr;

		if (GetBrushComponent()->GetSquaredDistanceToCollision(Point, DistanceSqr, ClosestPoint) == false)
		{
			OutDistanceToPoint = -1.f;
			return OutDistanceToPoint;
		}
#else
		FBoxSphereBounds Bounds = GetBrushComponent()->CalcBounds(GetBrushComponent()->GetComponentTransform());
		const float DistanceSqr = Bounds.GetBox().ComputeSquaredDistanceToPoint(Point);
#endif

		OutDistanceToPoint = FMath::Sqrt(DistanceSqr);		
	}

	return OutDistanceToPoint;
}
*/

void AOWSFogVolume::Tick(float DeltaSeconds)
{
	Super::Tick(DeltaSeconds);

	if (ExponentialHeightFog)
	{
		ACharacter* MyCharacter = UGameplayStatics::GetPlayerCharacter(GetWorld(), 0);
		float Distance = 0.f;

		if (MyCharacter)
		{
			bool IsInside = EncompassesPoint(MyCharacter->GetActorLocation(), 1.f, &Distance);

			if (IsInside)
			{
				ExponentialHeightFog->GetComponent()->SetVolumetricFogExtinctionScale(MaxFogExtinction);
			}
			else if (Distance < TransitionRange)
			{
				float LerpedFogExtincition = 0.f;
				LerpedFogExtincition = FMath::Lerp(MaxFogExtinction, MinFogExtinction, Distance / TransitionRange);
				ExponentialHeightFog->GetComponent()->SetVolumetricFogExtinctionScale(LerpedFogExtincition);
			}
		}
	}
}