// Fill out your copyright notice in the Description page of Project Settings.

#include "OWSProjectileMovementComponent.h"
#include "Net/UnrealNetwork.h"
#include "Engine/ScopedMovementUpdate.h"
#include "Runtime/Engine/Classes/Components/PrimitiveComponent.h"


UOWSProjectileMovementComponent::UOWSProjectileMovementComponent(const FObjectInitializer& ObjectInitializer)
	: Super(ObjectInitializer)
{
	HitZStopSimulatingThreshold = -1.1f; // default is always stop
	bPreventZHoming = false;
}

void UOWSProjectileMovementComponent::InitializeComponent()
{
	Super::InitializeComponent();

	if (bAutoRegisterUpdatedComponent && AddlUpdatedComponents.Num() == 0 && UpdatedComponent != NULL)
	{
		TArray<USceneComponent*> Components;
		UpdatedComponent->GetChildrenComponents(true, Components);
		for (int32 i = 0; i < Components.Num(); i++)
		{
			UPrimitiveComponent* Prim = Cast<UPrimitiveComponent>(Components[i]);
			if (Prim != NULL && Prim->GetCollisionEnabled())
			{
				AddlUpdatedComponents.Add(Prim);
				// if code hasn't manually set an overlap event, mirror the setting of the parent
				UPrimitiveComponent* UpdatedPrimComponent = Cast<UPrimitiveComponent>(UpdatedComponent);
				if (!Prim->OnComponentBeginOverlap.IsBound() && !Prim->OnComponentEndOverlap.IsBound() && UpdatedPrimComponent)
				{
					Prim->OnComponentBeginOverlap = UpdatedPrimComponent->OnComponentBeginOverlap;
					Prim->OnComponentEndOverlap = UpdatedPrimComponent->OnComponentEndOverlap;
				}
			}
		}
	}
}

void UOWSProjectileMovementComponent::SetUpdatedComponent(USceneComponent* NewUpdatedComponent)
{
	USceneComponent* OldUpdatedComponent = UpdatedComponent;
	APhysicsVolume* OldPhysicsVolume = UpdatedComponent ? UpdatedComponent->GetPhysicsVolume() : nullptr;
	Super::SetUpdatedComponent(NewUpdatedComponent);
	if (bKeepPhysicsVolumeWhenStopped && OldUpdatedComponent && IsValid(OldUpdatedComponent))
	{
		OldUpdatedComponent->SetPhysicsVolume(OldPhysicsVolume, true);
		OldUpdatedComponent->SetShouldUpdatePhysicsVolume(true);
	}
}


bool UOWSProjectileMovementComponent::MoveUpdatedComponentImpl(const FVector& Delta, const FQuat& NewRotation, bool bSweep, FHitResult* OutHit, ETeleportType Teleport)
{
	// if we have no extra components or we don't need to sweep, use the default behavior
	if (AddlUpdatedComponents.Num() == 0 || UpdatedComponent == NULL || !bSweep || Delta.IsNearlyZero())
	{
		return Super::MoveUpdatedComponentImpl(Delta, NewRotation, bSweep, OutHit, Teleport);
	}
	else
	{
		// make sure elements are valid
		for (int32 i = AddlUpdatedComponents.Num() - 1; i >= 0; i--)
		{
			if (!IsValid(AddlUpdatedComponents[i]))
			{
				AddlUpdatedComponents.RemoveAt(i);
			}
		}

		struct FNewableScopedMovementUpdate : public FScopedMovementUpdate
		{
		public:
			// relative transform of component prior to move
			FVector RelativeLocation;
			FRotator RelativeRotation;

			FNewableScopedMovementUpdate(USceneComponent* Component, EScopedUpdate::Type ScopeBehavior = EScopedUpdate::DeferredUpdates)
				: FScopedMovementUpdate(Component, ScopeBehavior), RelativeLocation(Component->GetRelativeLocation()), RelativeRotation(Component->GetRelativeRotation())
			{}

			// allow new and delete on FScopedMovementUpdate so we can use a dynamic number of elements
			// WARNING: these MUST all be deleted before this function returns in opposite order of creation or bad stuff will happen!
			REPLACEMENT_OPERATOR_NEW_AND_DELETE
		};

		FNewableScopedMovementUpdate* RootDeferredUpdate = new FNewableScopedMovementUpdate(UpdatedComponent);
		TArray<FNewableScopedMovementUpdate*> DeferredUpdates;
		for (int32 i = 0; i < AddlUpdatedComponents.Num(); i++)
		{
			DeferredUpdates.Add(new FNewableScopedMovementUpdate(AddlUpdatedComponents[i]));
		}

		FRotator RotChange = NewRotation.Rotator().GetNormalized() - UpdatedComponent->GetComponentToWorld().GetRotation().Rotator().GetNormalized();

		FHitResult EarliestHit;
		// move root
		bool bResult = Super::MoveUpdatedComponentImpl(Delta, NewRotation, bSweep, &EarliestHit, Teleport);

		float InitialMoveSize = Delta.Size() * EarliestHit.Time;
		float ShortestMoveSize = InitialMoveSize;
		bool bGotHit = EarliestHit.bBlockingHit;
		bool bPenetrating = EarliestHit.bStartPenetrating;
		float PenetrationDepth = (bPenetrating ? EarliestHit.PenetrationDepth : -1.0f);

		// move children
		for (int32 i = 0; i < AddlUpdatedComponents.Num(); i++)
		{
			// hack so InternalSetWorldLocationAndRotation() counts the component as moved (which will also clobber this hacked value)
			// otherwise it will pull the updated transform from the parent above and think nothing has happened, which prevents overlaps from working
			FVector ComponentRelativeLocation = AddlUpdatedComponents[i]->GetRelativeLocation();
			ComponentRelativeLocation.Z += 0.1f;
			AddlUpdatedComponents[i]->SetRelativeLocation(ComponentRelativeLocation);

			FHitResult NewHit;
			AddlUpdatedComponents[i]->MoveComponent(Delta, AddlUpdatedComponents[i]->GetComponentToWorld().GetRotation().Rotator() + RotChange, bSweep, &NewHit, MoveComponentFlags);
			if (NewHit.bBlockingHit)
			{
				if (NewHit.bStartPenetrating)
				{
					// Take the penetration that was deepest, so we move out far enough to retry the move.
					bPenetrating = true;
					if (NewHit.PenetrationDepth > PenetrationDepth)
					{
						ShortestMoveSize = 0.f;
						PenetrationDepth = NewHit.PenetrationDepth;
						EarliestHit = NewHit;
					}
				}
				else if (!bPenetrating)
				{
					float MoveSize = Delta.Size() * NewHit.Time;
					if (!bGotHit || MoveSize < ShortestMoveSize - KINDA_SMALL_NUMBER)
					{
						ShortestMoveSize = MoveSize;
						EarliestHit = NewHit;
					}
				}

				bGotHit = true;
			}
			// restore RelativeLocation and RelativeRotation after moving
			AddlUpdatedComponents[i]->SetRelativeLocationAndRotation(DeferredUpdates[i]->RelativeLocation, DeferredUpdates[i]->RelativeRotation, false);
		}
		// if we got a blocking hit, we need to revert and move everything using the shortest delta
		if (bGotHit)
		{
			static bool bRecursing = false; // recursion should be impossible but sanity check
			if (bRecursing)
			{
				// apply moves
				for (int32 i = DeferredUpdates.Num() - 1; i >= 0; i--)
				{
					delete DeferredUpdates[i];
				}
				delete RootDeferredUpdate;

				if (OutHit != NULL)
				{
					(*OutHit) = EarliestHit;
				}
				return bResult;
			}
			else
			{
				// revert moves
				for (int32 i = DeferredUpdates.Num() - 1; i >= 0; i--)
				{
					DeferredUpdates[i]->RevertMove();
					delete DeferredUpdates[i];
				}
				RootDeferredUpdate->RevertMove();
				delete RootDeferredUpdate;

				// If we are penetrating we just want to return the penetrated result, so that there can be a fixup outside of this function.
				// Otherwise let's recurse and try to move every component the shorter distance to closest impact.
				if (!bPenetrating)
				{
					// recurse
					bRecursing = true;
					bResult = MoveUpdatedComponentImpl(Delta.GetSafeNormal() * ShortestMoveSize, NewRotation, bSweep, OutHit, Teleport);
					bRecursing = false;
				}

				if (OutHit != NULL)
				{
					(*OutHit) = EarliestHit;
				}
				return bResult;
			}
		}
		else
		{
			// apply moves
			for (int32 i = DeferredUpdates.Num() - 1; i >= 0; i--)
			{
				delete DeferredUpdates[i];
			}
			delete RootDeferredUpdate;

			if (OutHit != NULL)
			{
				(*OutHit) = EarliestHit;
			}
			return bResult;
		}
	}
}

void UOWSProjectileMovementComponent::TickComponent(float DeltaTime, enum ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction)
{
	Super::TickComponent(DeltaTime, TickType, ThisTickFunction);
	UpdateState(DeltaTime);
}
void UOWSProjectileMovementComponent::HandleImpact(const FHitResult& Hit, float TimeSlice, const FVector& MoveDelta)
{
	if (!bShouldBounce && Hit.Normal.Z < HitZStopSimulatingThreshold && UpdatedComponent != NULL && UpdatedComponent->GetOwner() != NULL && TimeSlice > 0.0f)
	{
		AActor* ActorOwner = UpdatedComponent->GetOwner();
		FVector OldLocation = UpdatedComponent->GetComponentLocation();

		Velocity = MoveDelta / TimeSlice;

		FVector OldHitNormal = Hit.Normal;
		FVector Delta = (MoveDelta - Hit.Normal * (MoveDelta | Hit.Normal)) * (1.f - Hit.Time);
		if ((Delta | MoveDelta) >= 0.0f)
		{
			if (Delta.Z > 0.f) // friction slows sliding up slopes
			{
				Delta *= 0.5f;
			}
			FHitResult NewHit;
			SafeMoveUpdatedComponent(Delta, ActorOwner->GetActorRotation(), true, NewHit);
			if (UpdatedComponent != NULL && !ActorOwner->IsPendingKillPending())
			{
				if (NewHit.Time < 1.f) // hit second wall
				{
					if (NewHit.Normal.Z >= HitZStopSimulatingThreshold)
					{
						StopSimulating(NewHit);
					}
					else
					{
						// TODO: should we call anything here?
						//processHitWall(NewHit.Normal, NewHit.Actor, NewHit.Component);
						//if (bDeleteMe)
						//{
						//	return;
						//}

						TwoWallAdjust(Delta, NewHit, OldHitNormal);
						if (UpdatedComponent != NULL && !ActorOwner->IsPendingKillPending())
						{
							bool bDitch = ((OldHitNormal.Z > 0.0f) && (NewHit.Normal.Z > 0.0f) && (Delta.Z == 0.0f) && ((NewHit.Normal | OldHitNormal) < 0.0f));
							SafeMoveUpdatedComponent(Delta, ActorOwner->GetActorRotation(), true, NewHit);
							if (UpdatedComponent != NULL && !ActorOwner->IsPendingKillPending())
							{
								if (bDitch || NewHit.Normal.Z >= HitZStopSimulatingThreshold)
								{
									StopSimulating(NewHit);
								}
							}
						}
					}
				}
			}
		}
		// update velocity for actual movement that occurred
		if (UpdatedComponent != NULL)
		{
			float OldVelZ = Velocity.Z;
			Velocity = ((UpdatedComponent->GetComponentLocation() - OldLocation + MoveDelta * Hit.Time) / TimeSlice).GetClampedToMaxSize2D(Velocity.Size2D());
			Velocity.Z = OldVelZ;
		}
	}
	else
	{
		Super::HandleImpact(Hit, TimeSlice, MoveDelta);
	}
}

void UOWSProjectileMovementComponent::UpdateState(float DeltaSeconds)
{
	/*AUTRemoteRedeemer* MyRedeemer = UpdatedComponent ? Cast<AUTRemoteRedeemer>(UpdatedComponent->GetOwner()) : NULL;
	if (MyRedeemer)
	{
		if (MyRedeemer->IsLocallyControlled())
		{
			// Can only start sending moves if our controllers are synced up over the network, otherwise we flood the reliable buffer.
			APlayerController* PC = Cast<APlayerController>(MyRedeemer->GetController());
			if (PC && PC->AcknowledgedPawn != MyRedeemer)
			{
				return;
			}

			ServerUpdateState(Acceleration);
		}
		else
		{
			Acceleration = ReplicatedAcceleration;
		}
	}*/
}

void UOWSProjectileMovementComponent::GetLifetimeReplicatedProps(TArray< class FLifetimeProperty > & OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);

	DOREPLIFETIME(UOWSProjectileMovementComponent, ReplicatedAcceleration);
}

bool UOWSProjectileMovementComponent::ServerUpdateState_Validate(FVector InAcceleration)
{
	return true;
}

void UOWSProjectileMovementComponent::ServerUpdateState_Implementation(FVector InAcceleration)
{
	bool bServerReadyForClient = true;
	/*AUTRemoteRedeemer* MyRedeemer = UpdatedComponent ? Cast<AUTRemoteRedeemer>(UpdatedComponent->GetOwner()) : NULL;
	if (MyRedeemer)
	{
		APlayerController* PC = Cast<APlayerController>(MyRedeemer->GetController());
		if (PC)
		{
			bServerReadyForClient = PC->NotifyServerReceivedClientData(MyRedeemer, 0.0f);
		}
	}*/

	if (!bServerReadyForClient)
	{
		return;
	}

	Acceleration = InAcceleration;
	ReplicatedAcceleration = Acceleration;
}

FVector UOWSProjectileMovementComponent::ComputeHomingAcceleration(const FVector& InVelocity, float DeltaTime) const
{
	FVector HomingAcceleration = (HomingTargetComponent.IsValid() && UpdatedComponent) ? ((HomingTargetComponent->GetComponentLocation() - UpdatedComponent->GetComponentLocation()).GetSafeNormal() * HomingAccelerationMagnitude) : FVector(0.f);
	if (bPreventZHoming)
	{
		HomingAcceleration.Z = 0.f;
	}
	return HomingAcceleration;
}

