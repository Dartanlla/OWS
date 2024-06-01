// Fill out your copyright notice in the Description page of Project Settings.

#include "OWSGameplayAbilityTargetActor_P.h"
#include "Engine/World.h"
#include "AbilitySystemComponent.h" 
#include "Abilities/GameplayAbility.h"
#include "AbilitySystemLog.h"
#include "Abilities/GameplayAbilityWorldReticle_ActorVisualization.h"



AOWSGameplayAbilityTargetActor_P::AOWSGameplayAbilityTargetActor_P(const FObjectInitializer& ObjectInitializer)
	: Super(ObjectInitializer)
{
}

void AOWSGameplayAbilityTargetActor_P::EndPlay(const EEndPlayReason::Type EndPlayReason)
{
	if (ActorVisualizationReticle.IsValid())
	{
		ActorVisualizationReticle.Get()->Destroy();
	}

	Super::EndPlay(EndPlayReason);
}

void AOWSGameplayAbilityTargetActor_P::StartTargeting(UGameplayAbility* InAbility)
{
	Super::StartTargeting(InAbility);
	
	if (AActor *VisualizationActor = GetWorld()->SpawnActor(PlacedActorClass))
	{
		ActorVisualizationReticle = GetWorld()->SpawnActor<AOWSGameplayAbilityWorldReticle>();
		ActorVisualizationReticle->InitializeReticleVisualizationInformation(this, VisualizationActor, PlacedActorMaterial);
		GetWorld()->DestroyActor(VisualizationActor);
	}
	if (AGameplayAbilityWorldReticle* CachedReticleActor = ReticleActor.Get())
	{
		ActorVisualizationReticle->AttachToActor(CachedReticleActor, FAttachmentTransformRules::KeepRelativeTransform);
	}
	else
	{
		ReticleActor = ActorVisualizationReticle;
		ActorVisualizationReticle = nullptr;
	}
}

bool AOWSGameplayAbilityTargetActor_P::ClipCameraRayToAbilityRange(FVector CameraLocation, FVector CameraDirection, FVector AbilityCenter, float AbilityRange, float MinimumTargetingDistance, FVector& ClippedPosition)
{
	FVector CameraToCenter = AbilityCenter - CameraLocation;
	float DotToCenter = FVector::DotProduct(CameraToCenter, CameraDirection);
	if (DotToCenter >= 0)		//If this fails, we're pointed away from the center, but we might be inside the sphere and able to find a good exit point.
	{
		float DistanceSquared = CameraToCenter.SizeSquared() - (DotToCenter * DotToCenter);
		float RadiusSquared = (AbilityRange * AbilityRange);
		if (DistanceSquared <= RadiusSquared /*&& DistanceSquared >= MinimumTargetingDistance*/)
		{
			float DistanceFromCamera = FMath::Sqrt(RadiusSquared - DistanceSquared);
			float DistanceAlongRay = DotToCenter + DistanceFromCamera;						//Subtracting instead of adding will get the other intersection point
			ClippedPosition = CameraLocation + (DistanceAlongRay * CameraDirection);		//Cam aim point clipped to range sphere
			return true;
		}
	}
	return false;
}

void AOWSGameplayAbilityTargetActor_P::AimWithPlayerController(const AActor* InSourceActor, FCollisionQueryParams Params, const FVector& TraceStart, FVector& OutTraceEnd, bool bIgnorePitch) const
{
	if (!OwningAbility) // Server and launching client only
	{
		return;
	}

	//APlayerController* PC = OwningAbility->GetCurrentActorInfo()->PlayerController.Get();

	APawn* PWN = Cast<APawn>(OwningAbility->GetCurrentActorInfo()->OwnerActor.Get());
	check(PWN);
	APlayerController* PC = (APlayerController*)PWN->GetController();

	//APlayerController* PC = (APlayerController*)OwningAbility->GetCurrentActorInfo()->OwnerActor->GetController();

	check(PC);

	if (AimAtMouseCursor)
	{
		FHitResult Hit;
		PC->GetHitResultUnderCursor(ECollisionChannel::ECC_WorldStatic, false, Hit);

		if (Hit.bBlockingHit) {
			OutTraceEnd = Hit.Location;
		}
		else
		{
			OutTraceEnd = TraceStart;
		}
	}
	else
	{
		FVector ViewStart;
		FRotator ViewRot;
		PC->GetPlayerViewPoint(ViewStart, ViewRot);

		const FVector ViewDir = ViewRot.Vector();
		FVector ViewEnd = ViewStart + (ViewDir * MaxRange);

		ClipCameraRayToAbilityRange(ViewStart, ViewDir, TraceStart, MaxRange, MinimumTargetingDistance, ViewEnd);

		FHitResult HitResult;
		LineTraceWithFilter(HitResult, InSourceActor->GetWorld(), Filter, ViewStart, ViewEnd, TraceProfile.Name, Params);

		const bool bUseTraceResult = HitResult.bBlockingHit && (FVector::DistSquared(TraceStart, HitResult.Location) <= (MaxRange * MaxRange));

		const FVector AdjustedEnd = (bUseTraceResult) ? HitResult.Location : ViewEnd;

		FVector AdjustedAimDir = (AdjustedEnd - TraceStart).GetSafeNormal();
		if (AdjustedAimDir.IsZero())
		{
			AdjustedAimDir = ViewDir;
		}

		if (!bTraceAffectsAimPitch && bUseTraceResult)
		{
			FVector OriginalAimDir = (ViewEnd - TraceStart).GetSafeNormal();

			if (!OriginalAimDir.IsZero())
			{
				// Convert to angles and use original pitch
				const FRotator OriginalAimRot = OriginalAimDir.Rotation();

				FRotator AdjustedAimRot = AdjustedAimDir.Rotation();
				AdjustedAimRot.Pitch = OriginalAimRot.Pitch;

				AdjustedAimDir = AdjustedAimRot.Vector();
			}
		}

		OutTraceEnd = TraceStart + (AdjustedAimDir * MaxRange);
	}
}

//Might want to override this function to allow for a radius check against the ground, possibly including a height check. Or might want to do it in ground trace.
//FHitResult AGameplayAbilityTargetActor_ActorPlacement::PerformTrace(AActor* InSourceActor) const

FHitResult AOWSGameplayAbilityTargetActor_P::PerformTrace(AActor* InSourceActor)
{
	static const FName LineTraceSingleName(TEXT("AGameplayAbilityTargetActor_GroundTrace"));
	bool bTraceComplex = false;

	FCollisionQueryParams Params(LineTraceSingleName, bTraceComplex);
	Params.bReturnPhysicalMaterial = true;
	Params.AddIgnoredActor(InSourceActor);

	FVector TraceStart = StartLocation.GetTargetingTransform().GetLocation();// InSourceActor->GetActorLocation();
	FVector TraceEnd;
	AimWithPlayerController(InSourceActor, Params, TraceStart, TraceEnd, true);		//Effective on server and launching client only

	// ------------------------------------------------------

	FHitResult ReturnHitResult;

	if (AimAtMouseCursor)
	{
		ReturnHitResult.Location = TraceEnd;
	}
	else
	{
		//Use a line trace initially to see where the player is actually pointing
		LineTraceWithFilter(ReturnHitResult, InSourceActor->GetWorld(), Filter, TraceStart, TraceEnd, TraceProfile.Name, Params);
		//Default to end of trace line if we don't hit anything.
		if (!ReturnHitResult.bBlockingHit)
		{
			ReturnHitResult.Location = TraceEnd;
		}
	}

	//CollisionHeightOffset = 0.0f;
	//Second trace, straight down. Consider using InSourceActor->GetWorld()->NavigationSystem->ProjectPointToNavigation() instead of just going straight down in the case of movement abilities (flag/bool).
	TraceStart = ReturnHitResult.Location - (TraceEnd - TraceStart).GetSafeNormal();		//Pull back very slightly to avoid scraping down walls
	TraceEnd = TraceStart;
	TraceStart.Z += CollisionHeightOffset;
	TraceEnd.Z -= 99999.0f;
	LineTraceWithFilter(ReturnHitResult, InSourceActor->GetWorld(), Filter, TraceStart, TraceEnd, TraceProfile.Name, Params);
	//if (!ReturnHitResult.bBlockingHit) then our endpoint may be off the map. Hopefully this is only possible in debug maps.

	bLastTraceWasGood = true;		//So far, we're good. If we need a ground spot and can't find one, we'll come back.

									//Use collision shape to find a valid ground spot, if appropriate
	/*if (CollisionShape.ShapeType != ECollisionShape::Line)
	{
		ReturnHitResult.Location.Z += CollisionHeightOffset;		//Rise up out of the ground
		TraceStart = InSourceActor->GetActorLocation();
		TraceEnd = ReturnHitResult.Location;
		TraceStart.Z += CollisionHeightOffset;
		bLastTraceWasGood = AdjustCollisionResultForShape(TraceStart, TraceEnd, Params, ReturnHitResult);
		if (bLastTraceWasGood)
		{
			ReturnHitResult.Location.Z -= CollisionHeightOffset;	//Undo the artificial height adjustment
		}
	}*/

	if (AGameplayAbilityWorldReticle* LocalReticleActor = ReticleActor.Get())
	{
		LocalReticleActor->SetIsTargetValid(bLastTraceWasGood);
		LocalReticleActor->SetActorLocation(ReturnHitResult.Location);
	}

	// Reset the trace start so the target data uses the correct origin
	ReturnHitResult.TraceStart = StartLocation.GetTargetingTransform().GetLocation();

	return ReturnHitResult;
}

void AOWSGameplayAbilityTargetActor_P::ConfirmTargeting()
{
	const FGameplayAbilityActorInfo* ActorInfo = (OwningAbility ? OwningAbility->GetCurrentActorInfo() : nullptr);
	UAbilitySystemComponent* ASC = (ActorInfo ? ActorInfo->AbilitySystemComponent.Get() : nullptr);
	if (ASC)
	{
		ASC->AbilityReplicatedEventDelegate(EAbilityGenericReplicatedEvent::GenericConfirm, OwningAbility->GetCurrentAbilitySpecHandle(), OwningAbility->GetCurrentActivationInfo().GetActivationPredictionKey()).Remove(GenericConfirmHandle);
	}
	else
	{
		ABILITY_LOG(Warning, TEXT("AGameplayAbilityTargetActor::ConfirmTargeting called with null Ability/ASC! Actor %s"), *GetName());
	}

	if (IsConfirmTargetingAllowed())
	{
		ConfirmTargetingAndContinue();
		if (bDestroyOnConfirmation)
		{
			Destroy();
		}
	}
}

void AOWSGameplayAbilityTargetActor_P::BindToConfirmCancelInputs()
{
	check(OwningAbility);

	UAbilitySystemComponent* ASC = OwningAbility->GetCurrentActorInfo()->AbilitySystemComponent.Get();
	if (ASC)
	{
		const FGameplayAbilityActorInfo* Info = OwningAbility->GetCurrentActorInfo();

		if (Info->IsLocallyControlled())
		{
			// We have to wait for the callback from the AbilitySystemComponent. Which will always be instigated locally
			ASC->GenericLocalConfirmCallbacks.AddDynamic(this, &AGameplayAbilityTargetActor::ConfirmTargeting);	// Tell me if the confirm input is pressed
			ASC->GenericLocalCancelCallbacks.AddDynamic(this, &AGameplayAbilityTargetActor::CancelTargeting);	// Tell me if the cancel input is pressed

																												// Save off which ASC we bound so that we can error check that we're removing them later
			GenericDelegateBoundASC = ASC;
		}
		else
		{
			FGameplayAbilitySpecHandle Handle = OwningAbility->GetCurrentAbilitySpecHandle();
			FPredictionKey PredKey = OwningAbility->GetCurrentActivationInfo().GetActivationPredictionKey();

			GenericConfirmHandle = ASC->AbilityReplicatedEventDelegate(EAbilityGenericReplicatedEvent::GenericConfirm, Handle, PredKey).AddUObject(this, &AGameplayAbilityTargetActor::ConfirmTargeting);
			GenericCancelHandle = ASC->AbilityReplicatedEventDelegate(EAbilityGenericReplicatedEvent::GenericCancel, Handle, PredKey).AddUObject(this, &AGameplayAbilityTargetActor::CancelTargeting);

			if (ASC->CallReplicatedEventDelegateIfSet(EAbilityGenericReplicatedEvent::GenericConfirm, Handle, PredKey))
			{
				return;
			}

			if (ASC->CallReplicatedEventDelegateIfSet(EAbilityGenericReplicatedEvent::GenericCancel, Handle, PredKey))
			{
				return;
			}
		}
	}
}

void AOWSGameplayAbilityTargetActor_P::ConfirmTarget()
{
	ConfirmTargeting();
}

void AOWSGameplayAbilityTargetActor_P::CancelTarget()
{
	CancelTargeting();
}


