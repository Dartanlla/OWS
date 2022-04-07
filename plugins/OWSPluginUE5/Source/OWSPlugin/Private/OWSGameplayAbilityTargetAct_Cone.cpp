// Fill out your copyright notice in the Description page of Project Settings.

#include "OWSGameplayAbilityTargetAct_Cone.h"
#include "GameFramework/Pawn.h"
#include "WorldCollision.h"
#include "Abilities/GameplayAbility.h"

AOWSGameplayAbilityTargetAct_Cone::AOWSGameplayAbilityTargetAct_Cone(const FObjectInitializer& ObjectInitializer)
	: Super(ObjectInitializer)
{
	PrimaryActorTick.bCanEverTick = true;
	PrimaryActorTick.TickGroup = TG_PrePhysics;
	ShouldProduceTargetDataOnServer = true;
}

void AOWSGameplayAbilityTargetAct_Cone::StartTargeting(UGameplayAbility* InAbility)
{
	Super::StartTargeting(InAbility);
	SourceActor = InAbility->GetCurrentActorInfo()->AvatarActor.Get();
}

void AOWSGameplayAbilityTargetAct_Cone::ConfirmTargetingAndContinue()
{
	check(ShouldProduceTargetData());
	if (SourceActor)
	{
		FVector Origin = StartLocation.GetTargetingTransform().GetLocation();
		FGameplayAbilityTargetDataHandle Handle = MakeTargetData(PerformOverlap(Origin), Origin);
		TargetDataReadyDelegate.Broadcast(Handle);
	}
}

FGameplayAbilityTargetDataHandle AOWSGameplayAbilityTargetAct_Cone::MakeTargetData(const TArray<TWeakObjectPtr<AActor>>& Actors, const FVector& Origin) const
{
	if (OwningAbility)
	{
		/** Use the source location instead of the literal origin */
		return StartLocation.MakeTargetDataHandleFromActors(Actors, false);
	}

	return FGameplayAbilityTargetDataHandle();
}

TArray<TWeakObjectPtr<AActor> >	AOWSGameplayAbilityTargetAct_Cone::PerformOverlap(const FVector& Origin)
{
	bool bTraceComplex = false;

	FCollisionQueryParams Params(SCENE_QUERY_STAT(RadiusTargetingOverlap), bTraceComplex);
	Params.bReturnPhysicalMaterial = false;

	TArray<FOverlapResult> Overlaps;

	SourceActor->GetWorld()->OverlapMultiByObjectType(Overlaps, Origin, FQuat::Identity, FCollisionObjectQueryParams(ECC_Pawn), FCollisionShape::MakeSphere(Radius), Params);

	TArray<TWeakObjectPtr<AActor>>	HitActors;

	for (int32 i = 0; i < Overlaps.Num(); ++i)
	{
		//Should this check to see if these pawns are in the AimTarget list?
		APawn* PawnActor = Cast<APawn>(Overlaps[i].GetActor());
		if (PawnActor && !HitActors.Contains(PawnActor) && Filter.FilterPassesForActor(PawnActor))
		{
			FVector ActorOrigin;
			FVector ActorBoxExtent;
			PawnActor->GetActorBounds(true, ActorOrigin, ActorBoxExtent);

			FVector Midpoint = (ActorOrigin + (ActorOrigin + ActorBoxExtent)) / 2;

			//UE_LOG(LogTemp, Error, TEXT("Is midpoint in Cone?"));

			FVector ToMidPoint = Midpoint - Origin;
			ToMidPoint = ToMidPoint.GetSafeNormal();
			float AngleToCompare = FMath::Acos(FVector::DotProduct(ToMidPoint, ForwardVector));

			//UE_LOG(LogTemp, Error, TEXT("Angle to Compare: %f"), AngleToCompare);

			if (AngleToCompare <= FMath::DegreesToRadians(HalfAngle))
			{
				//UE_LOG(LogTemp, Error, TEXT("Yes it is!"));

				HitActors.Add(PawnActor);
			}
		}
	}

	return HitActors;
}
