// Copyright 2018 Sabre Dart Studios

#include "OWSCharacterMovementComponent.h"
#include "Components/CapsuleComponent.h"
#include "DrawDebugHelpers.h"
#include "Kismet/GameplayStaticsTypes.h"
#include "Kismet/GameplayStatics.h"

UOWSCharacterMovementComponent::UOWSCharacterMovementComponent(const class FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer)
{
	bCanLookAroundWhileClimbing = true;
	IsSprinting = false;
	MaxMaxWalkSpeed = 2000.f;
	bIsClimbing = false;
	bIsExitingClimb = false;
}


void UOWSCharacterMovementComponent::StartSprinting()
{
	bRequestToStartSprinting = true;
}

void UOWSCharacterMovementComponent::StopSprinting()
{
	bRequestToStartSprinting = false;
}

float UOWSCharacterMovementComponent::GetMaxSpeed() const
{
	float MaxSpeed = Super::GetMaxSpeed();

	if (IsMovingForward() && bRequestToStartSprinting)
	{
		MaxSpeed *= SprintSpeedMultiplier;
	}

	return MaxSpeed;
}

float UOWSCharacterMovementComponent::GetMaxAcceleration() const
{
	float MaxAccel = Super::GetMaxAcceleration();

	if (IsMovingForward() && bRequestToStartSprinting)
	{
		MaxAccel *= SprintAccelerationMultiplier;
	}

	return MaxAccel;
}

bool UOWSCharacterMovementComponent::IsMovingForward() const
{
	if (!PawnOwner)
	{
		return false;
	}

	FVector Forward = PawnOwner->GetActorForwardVector();
	FVector TempMoveDirection = Velocity.GetSafeNormal();

	//Ignore vertical movement
	Forward.Z = 0.0f;
	TempMoveDirection.Z = 0.0f;

	float VelocityDot = FVector::DotProduct(Forward, TempMoveDirection);
	return VelocityDot > 0.7f; //Check to make sure difference between headings is not too great.
}

void UOWSCharacterMovementComponent::OnMovementUpdated(float DeltaTime, const FVector& OldLocation, const FVector& OldVelocity)
{
	Super::OnMovementUpdated(DeltaTime, OldLocation, OldVelocity);

	if (!CharacterOwner)
	{
		return;
	}

	//Set Max Walk Speed
	if (bRequestMaxWalkSpeedChange)
	{
		bRequestMaxWalkSpeedChange = false;
		MaxWalkSpeed = MyNewMaxWalkSpeed;
	}

	//Dodge
	if (bWantsToDodge)
	{
		bWantsToDodge = false;

		//Only dodge if on the ground (in the air causes problems trying to get the two modes to line up due to friction)
		if (IsMovingOnGround())
		{
			MoveDirection.Normalize();
			FVector DodgeVelocity = MoveDirection * DodgeStrength;
			//Set Z component to zero so we don't go up
			DodgeVelocity.Z = 0.0f;

			Launch(DodgeVelocity);
		}
	}

	//Climbing
	if (bWantsToClimb)
	{
		FVector CheckPoint;
		CheckPoint = CharacterOwner->GetActorForwardVector();
		CheckPoint.Z = 0.f;
		FVector CheckNorm = CheckPoint.GetSafeNormal();
		/*FVector ClimbingWallNorm = UpdatedComponent->GetForwardVector().GetSafeNormal();
		ClimbingWallNorm.Z = 0.f;
		float dp = FVector::DotProduct(CheckNorm, ClimbingWallNorm);

		if (dp > 0.f)
		{
			UE_LOG(LogTemp, Warning, TEXT("Opposite Direction to Climb: %f"), dp);
			bWantsToClimb = false;
			return;
		}*/

		StopActiveMovement();

		SetMovementMode(MOVE_Custom);
		bIsClimbing = true;
		bOrientRotationToMovement = false;
		CharacterOwner->bUseControllerRotationYaw = false;

		float PawnCapsuleRadius, PawnCapsuleHalfHeight;
		CharacterOwner->GetCapsuleComponent()->GetScaledCapsuleSize(PawnCapsuleRadius, PawnCapsuleHalfHeight);
		CheckPoint = UpdatedComponent->GetComponentLocation() + 1.2f * PawnCapsuleRadius * 10 * CheckNorm;
		CheckPoint += FVector(0.0f, 0.0f, -5.0f);
		FVector Extent(PawnCapsuleRadius, PawnCapsuleRadius, PawnCapsuleHalfHeight);
		FHitResult HitInfo(1.f);
		FCollisionQueryParams CapsuleParams(FName(TEXT("CheckWaterJump")), false, CharacterOwner);
		FCollisionResponseParams ResponseParam;
		InitCollisionParams(CapsuleParams, ResponseParam);
		FCollisionShape CapsuleShape = GetPawnCapsuleCollisionShape(SHRINK_None);
		const ECollisionChannel CollisionChannel = UpdatedComponent->GetCollisionObjectType();
		bool bHit = GetWorld()->SweepSingleByChannel(HitInfo, UpdatedComponent->GetComponentLocation(), CheckPoint, FQuat::Identity, CollisionChannel, CapsuleShape, CapsuleParams, ResponseParam);

		if (bHit && HitInfo.Distance > 100.0f)
		{
			FVector NewLocation;
			NewLocation = CharacterOwner->GetActorLocation() + (CharacterOwner->GetActorForwardVector().GetSafeNormal() * (HitInfo.Distance));
			CharacterOwner->SetActorLocation(NewLocation, true);
		}

		if (!bCanLookAroundWhileClimbing)
		{
			APlayerController* OurPlayerController = Cast<APlayerController>(CharacterOwner->GetController());
			if (OurPlayerController)
			{
				OurPlayerController->InputYawScale = 0.f;
			}
		}

		bUseControllerDesiredRotation = false;

		bWantsToClimb = false;
	}

	if (bWantsToExitClimb && bIsClimbing)
	{
		bIsClimbing = false;

		APlayerController* OurPlayerController = Cast<APlayerController>(CharacterOwner->GetController());
		if (OurPlayerController)
		{
			OurPlayerController->InputYawScale = 1.f;
		}

		bUseControllerDesiredRotation = true;
		if (bLaunchForwardOnExitClimb)
		{
			FVector forwardDir = CharacterOwner->GetActorForwardVector();
			FVector vLaunch = forwardDir * 150;
			vLaunch.Z = 500.0f;
			Launch(vLaunch);
		}
		else
		{
			SetMovementMode(MOVE_Walking);
		}
		bWantsToExitClimb = false;
		bLaunchForwardOnExitClimb = true;
	}
}


bool UOWSCharacterMovementComponent::HandlePendingLaunch()
{
	if (!PendingLaunchVelocity.IsZero() && HasValidData())
	{
		Velocity = PendingLaunchVelocity;
		SetMovementMode(MOVE_Falling);
		PendingLaunchVelocity = FVector::ZeroVector;
		bForceNextFloorCheck = true;
		return true;
	}

	return false;
}

//============================================================================================
//Replication
//============================================================================================

//Set input flags on character from saved inputs
void UOWSCharacterMovementComponent::UpdateFromCompressedFlags(uint8 Flags)//Client only
{
	Super::UpdateFromCompressedFlags(Flags);

	//The Flags parameter contains the compressed input flags that are stored in the saved move.
	//UpdateFromCompressed flags simply copies the flags from the saved move into the movement component.
	//It basically just resets the movement component to the state when the move was made so it can simulate from there.
	bRequestToStartSprinting = (Flags&FSavedMove_Character::FLAG_Custom_0) != 0;
	bWantsToDodge = (Flags&FSavedMove_Character::FLAG_Custom_1) != 0;
	bRequestMaxWalkSpeedChange = (Flags&FSavedMove_Character::FLAG_Custom_2) != 0;
	bWantsToClimb = (Flags&FSavedMove_Character::FLAG_Custom_3) != 0;
	bWantsToExitClimb = (Flags&FSavedMove_Character::FLAG_Reserved_2) != 0;
}

class FNetworkPredictionData_Client* UOWSCharacterMovementComponent::GetPredictionData_Client() const
{
	check(PawnOwner != NULL);
	check(PawnOwner->GetLocalRole() < ROLE_Authority);

	if (!ClientPredictionData)
	{
		UOWSCharacterMovementComponent* MutableThis = const_cast<UOWSCharacterMovementComponent*>(this);

		MutableThis->ClientPredictionData = new FNetworkPredictionData_Client_OWS(*this);
		MutableThis->ClientPredictionData->MaxSmoothNetUpdateDist = 92.f;
		MutableThis->ClientPredictionData->NoSmoothNetUpdateDist = 140.f;
	}

	return ClientPredictionData;
}

void UOWSCharacterMovementComponent::FSavedMove_OWS::Clear()
{
	Super::Clear();

	bSavedRequestToStartSprinting = false;
	bSavedRequestMaxWalkSpeedChange = false;
	SavedWalkSpeed = 0.f;
	bSavedWantsToDodge = false;
	SavedMoveDirection = FVector::ZeroVector;
	bSavedWantsToClimb = false;
	bSavedWantsToExitClimb = false;
}

uint8 UOWSCharacterMovementComponent::FSavedMove_OWS::GetCompressedFlags() const
{
	uint8 Result = Super::GetCompressedFlags();

	if (bSavedRequestToStartSprinting)
	{
		Result |= FLAG_Custom_0;
	}
	
	if (bSavedWantsToDodge)
	{
		Result |= FLAG_Custom_1;
	}

	if (bSavedRequestMaxWalkSpeedChange)
	{
		Result |= FLAG_Custom_2;
	}

	if (bSavedWantsToClimb)
	{
		Result |= FLAG_Custom_3;
	}

	if (bSavedWantsToExitClimb)
	{
		Result |= FLAG_Reserved_2;
	}
	
	return Result;
}

bool UOWSCharacterMovementComponent::FSavedMove_OWS::CanCombineWith(const FSavedMovePtr& NewMove, ACharacter* Character, float MaxDelta) const
{
	//Set which moves can be combined together. This will depend on the bit flags that are used.
	if (bSavedRequestToStartSprinting != ((FSavedMove_OWS*)&NewMove)->bSavedRequestToStartSprinting)
	{
		return false;
	}
	if (bSavedRequestMaxWalkSpeedChange != ((FSavedMove_OWS*)&NewMove)->bSavedRequestMaxWalkSpeedChange)
	{
		return false;
	}
	if (SavedWalkSpeed != ((FSavedMove_OWS*)&NewMove)->SavedWalkSpeed)
	{
		return false;
	}
	if (bSavedWantsToDodge != ((FSavedMove_OWS*)&NewMove)->bSavedWantsToDodge)
	{
		return false;
	}
	if (SavedMoveDirection != ((FSavedMove_OWS*)&NewMove)->SavedMoveDirection)
	{
		return false;
	}
	if (bSavedWantsToClimb != ((FSavedMove_OWS*)&NewMove)->bSavedWantsToClimb)
	{
		return false;
	}
	if (bSavedWantsToExitClimb != ((FSavedMove_OWS*)&NewMove)->bSavedWantsToExitClimb)
	{
		return false;
	}

	return Super::CanCombineWith(NewMove, Character, MaxDelta);
}

void UOWSCharacterMovementComponent::FSavedMove_OWS::SetMoveFor(ACharacter* Character, float InDeltaTime, FVector const& NewAccel, class FNetworkPredictionData_Client_Character & ClientData)
{
	Super::SetMoveFor(Character, InDeltaTime, NewAccel, ClientData);

	UOWSCharacterMovementComponent* CharacterMovement = Cast<UOWSCharacterMovementComponent>(Character->GetCharacterMovement());
	if (CharacterMovement)
	{
		bSavedRequestToStartSprinting = CharacterMovement->bRequestToStartSprinting;
		bSavedRequestMaxWalkSpeedChange = CharacterMovement->bRequestMaxWalkSpeedChange;
		SavedWalkSpeed = CharacterMovement->MyNewMaxWalkSpeed;
		bSavedWantsToDodge = CharacterMovement->bWantsToDodge;
		SavedMoveDirection = CharacterMovement->MoveDirection;
		bSavedWantsToClimb = CharacterMovement->bWantsToClimb;
		bSavedWantsToExitClimb = CharacterMovement->bWantsToExitClimb;
	}
}

void UOWSCharacterMovementComponent::FSavedMove_OWS::PrepMoveFor(class ACharacter* Character)
{
	Super::PrepMoveFor(Character);

	UOWSCharacterMovementComponent* CharacterMovement = Cast<UOWSCharacterMovementComponent>(Character->GetCharacterMovement());
	if (CharacterMovement)
	{
		CharacterMovement->MoveDirection = SavedMoveDirection;
		CharacterMovement->MyNewMaxWalkSpeed = SavedWalkSpeed;
	}
}

UOWSCharacterMovementComponent::FNetworkPredictionData_Client_OWS::FNetworkPredictionData_Client_OWS(const UCharacterMovementComponent& ClientMovement)
	: Super(ClientMovement)
{

}

FSavedMovePtr UOWSCharacterMovementComponent::FNetworkPredictionData_Client_OWS::AllocateNewMove()
{
	return FSavedMovePtr(new FSavedMove_OWS());
}


//Set Max Walk Speed RPC to transfer the current Max Walk Speed from the Owning Client to the Server
bool UOWSCharacterMovementComponent::Server_SetMaxWalkSpeed_Validate(const float NewMaxWalkSpeed)
{
	if (NewMaxWalkSpeed < 0.f || NewMaxWalkSpeed > MaxMaxWalkSpeed)
		return false;
	else
		return true;
}

void UOWSCharacterMovementComponent::Server_SetMaxWalkSpeed_Implementation(const float NewMaxWalkSpeed)
{
	MyNewMaxWalkSpeed = NewMaxWalkSpeed;
}

void UOWSCharacterMovementComponent::SetMaxWalkSpeed(float NewMaxWalkSpeed)
{
	if (PawnOwner->IsLocallyControlled())
	{
		MyNewMaxWalkSpeed = NewMaxWalkSpeed;
		Server_SetMaxWalkSpeed(NewMaxWalkSpeed);
	}

	bRequestMaxWalkSpeedChange = true;
}

//Dodge RPC to transfer the current Move Direction from the Owning Client to the Server
bool UOWSCharacterMovementComponent::Server_MoveDirection_Validate(const FVector& MoveDir)
{
	return true;
}

void UOWSCharacterMovementComponent::Server_MoveDirection_Implementation(const FVector& MoveDir)
{
	MoveDirection = MoveDir;
}

//Trigger the Dodge ability on the Owning Client
void UOWSCharacterMovementComponent::DoDodge()
{
	if (PawnOwner->IsLocallyControlled())
	{
		MoveDirection = PawnOwner->GetLastMovementInputVector();
		Server_MoveDirection(MoveDirection);
	}

	bWantsToDodge = true;
}


//Climbing
void UOWSCharacterMovementComponent::PhysCustom(float deltaTime, int32 Iterations)
{
	switch (CustomMovementMode)
	{
	case TESTMOVE_Climbing:
		bIsClimbing = true;
		PhysCustomClimb(deltaTime, Iterations);
		break;
	case TESTMOVE_Walking:
		PhysCustomWalk(deltaTime, Iterations);
		break;
	default:
		break;
	}
}

void UOWSCharacterMovementComponent::PhysCustomClimb(float deltaTime, int32 Iterations)
{
	//GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Yellow, TEXT("Climb"));
	if (deltaTime < MIN_TICK_TIME)
	{
		return;
	}

	RestorePreAdditiveRootMotionVelocity();

	if (!HasAnimRootMotion() && !CurrentRootMotion.HasOverrideVelocity())
	{
		if (bCheatFlying && Acceleration.IsZero())
		{
			Velocity = FVector::ZeroVector;
		}
		const float Friction = BrakingFriction;//0.5f * GetPhysicsVolume()->FluidFriction;
		CalcVelocity(deltaTime, Friction, true, BrakingDecelerationWalking);
	}

	ApplyRootMotionToVelocity(deltaTime);

	Iterations++;
	bJustTeleported = false;
	if (CharacterOwner)
	{
		FVector wallNormal = FVector(0.0f);
		FVector forwardDir = CharacterOwner->GetActorForwardVector();
		if (CheckForExitToClimbing(forwardDir * 500, wallNormal))
		{
			bLaunchForwardOnExitClimb = true;
			SetClimbing(false, FRotator::ZeroRotator);
			bIsExitingClimb = true;
			bWantsToExitClimb = true;
		}
		else
		{
			Velocity.X = 0.0f;
			Velocity.Y = 0.0f;
		}
	}



	FVector OldLocation = UpdatedComponent->GetComponentLocation();
	const FVector Adjusted = Velocity * deltaTime;
	FHitResult Hit(1.f);
	SafeMoveUpdatedComponent(Adjusted, UpdatedComponent->GetComponentQuat(), true, Hit);


	if (Hit.Time < 1.f)
	{
		const FVector PawnLocation = UpdatedComponent->GetComponentLocation();
		FFindFloorResult FloorResult;
		FindFloor(PawnLocation, FloorResult, false);
		if (FloorResult.IsWalkableFloor() && IsValidLandingSpot(PawnLocation, FloorResult.HitResult))
		{
			SetClimbing(false, FRotator::ZeroRotator);
			return;
		}

		const FVector GravDir = FVector(0.f, 0.f, -1.f);
		const FVector VelDir = Velocity.GetSafeNormal();
		const float UpDown = GravDir | VelDir;

		bool bSteppedUp = false;
		if ((FMath::Abs(Hit.ImpactNormal.Z) < 0.2f) && (UpDown < 0.5f) && (UpDown > -0.2f) && CanStepUp(Hit))
		{
			float stepZ = UpdatedComponent->GetComponentLocation().Z;
			bSteppedUp = StepUp(GravDir, Adjusted * (1.f - Hit.Time), Hit);
			if (bSteppedUp)
			{
				OldLocation.Z = UpdatedComponent->GetComponentLocation().Z + (OldLocation.Z - stepZ);
			}
		}

		if (!bSteppedUp)
		{
			//adjust and try again
			HandleImpact(Hit, deltaTime, Adjusted);
			SlideAlongSurface(Adjusted, (1.f - Hit.Time), Hit.Normal, Hit, true);
		}
	}

	/*
	if (Hit.Time < 1.f)
	{
		const FVector GravDir = FVector(0.f, 0.f, -1.f);
		const FVector VelDir = Velocity.GetSafeNormal();
		const float UpDown = GravDir | VelDir;

		bool bSteppedUp = false;
		if ((FMath::Abs(Hit.ImpactNormal.Z) < 0.2f) && (UpDown < 0.5f) && (UpDown > -0.2f) && CanStepUp(Hit))
		{
			float stepZ = UpdatedComponent->GetComponentLocation().Z;
			bSteppedUp = StepUp(GravDir, Adjusted * (1.f - Hit.Time), Hit);
			if (bSteppedUp)
			{
				OldLocation.Z = UpdatedComponent->GetComponentLocation().Z + (OldLocation.Z - stepZ);
			}
		}

		if (!bSteppedUp)
		{
			//adjust and try again
			HandleImpact(Hit, deltaTime, Adjusted);
			SlideAlongSurface(Adjusted, (1.f - Hit.Time), Hit.Normal, Hit, true);
		}
	}
	*/

	/*if (!bJustTeleported && !HasAnimRootMotion() && !CurrentRootMotion.HasOverrideVelocity())
	{
		Velocity = (UpdatedComponent->GetComponentLocation() - OldLocation) / deltaTime;
	}*/
}

bool UOWSCharacterMovementComponent::CheckForExitToClimbing(FVector CheckPoint, FVector& WallNormal)
{
	// check if there is a wall directly in front of the climbing pawn
	CheckPoint.Z = 0.f;
	FVector CheckNorm = CheckPoint.GetSafeNormal();
	float PawnCapsuleRadius, PawnCapsuleHalfHeight;
	CharacterOwner->GetCapsuleComponent()->GetScaledCapsuleSize(PawnCapsuleRadius, PawnCapsuleHalfHeight);
	CheckPoint = UpdatedComponent->GetComponentLocation() + 1.2f * PawnCapsuleRadius * 5 * CheckNorm;
	CheckPoint += FVector(0.0f, 0.0f, -5.0f);
	FVector Extent(PawnCapsuleRadius, PawnCapsuleRadius, PawnCapsuleHalfHeight);
	FHitResult HitInfo(1.f);
	FCollisionQueryParams CapsuleParams(FName(TEXT("CheckWaterJump")), false, CharacterOwner);
	FCollisionResponseParams ResponseParam;
	InitCollisionParams(CapsuleParams, ResponseParam);
	FCollisionShape CapsuleShape = GetPawnCapsuleCollisionShape(SHRINK_None);
	const ECollisionChannel CollisionChannel = UpdatedComponent->GetCollisionObjectType();
	bool bHit = GetWorld()->SweepSingleByChannel(HitInfo, UpdatedComponent->GetComponentLocation(), CheckPoint, FQuat::Identity, CollisionChannel, CapsuleShape, CapsuleParams, ResponseParam);

	//DrawDebugLine(GetWorld(), UpdatedComponent->GetComponentLocation(), CheckPoint, FColor(255, 0, 0), false, -1, 0, 12.333);

	if (!bHit)
	{
		UE_LOG(OWS, Verbose, TEXT("Can Exit Climbing"));
		return true;
	}

	return false;
}


void UOWSCharacterMovementComponent::ProcessLanded(const FHitResult& Hit, float remainingTime, int32 Iterations)
{
	Super::ProcessLanded(Hit, remainingTime, Iterations);

	SetMovementMode(MOVE_Walking);

	if (CharacterOwner && CharacterOwner->GetController())
	{
		APlayerController* OurPlayerController = Cast<APlayerController>(CharacterOwner->GetController());

		if (OurPlayerController)
		{
			OurPlayerController->InputYawScale = 1.f;
		}
	}

	bUseControllerDesiredRotation = true;

	bIsExitingClimb = false;
}


bool UOWSCharacterMovementComponent::DoJump(bool bReplayingMoves)
{
	if (CharacterOwner && CharacterOwner->CanJump())
	{
		// Don't jump if we can't move up/down.
		if (!bConstrainToPlane || FMath::Abs(PlaneConstraintNormal.Z) != 1.f)
		{
			Velocity.Z = FMath::Max(Velocity.Z, JumpZVelocity);

			if (MovementMode == MOVE_Custom)
			{
				bIsExitingClimb = true;
				bWantsToExitClimb = 1;
				bLaunchForwardOnExitClimb = false;
			}
			else
			{
				SetMovementMode(MOVE_Falling);
			}

			return true;
		}
	}

	return false;
}

void UOWSCharacterMovementComponent::PhysCustomWalk(float deltaTime, int32 Iterations)
{
	//GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Yellow, TEXT("Walk"));
}



void UOWSCharacterMovementComponent::SetClimbing(bool bClimbing, FRotator newRotation)
{
	if (bClimbing && !bIsClimbing)
	{
		/*if (CharacterOwner->GetLocalRole() < ROLE_Authority)
		{
			UE_LOG(LogTemp, Warning, TEXT("Client - Set Climbing ON"));
		}
		else {
			UE_LOG(LogTemp, Warning, TEXT("Server - Set Climbing ON"));
		}*/
		bWantsToClimb = bClimbing;

		CharacterOwner->SetActorRotation(newRotation);
	}
	else if (!bClimbing && bIsClimbing)
	{
		/*if (CharacterOwner->GetLocalRole() < ROLE_Authority)
		{
			UE_LOG(LogTemp, Warning, TEXT("Client - Set Climbing OFF"));
		}
		else {
			UE_LOG(LogTemp, Warning, TEXT("Server - Set Climbing OFF"));
		}*/
		bWantsToClimb = bClimbing;
		bWantsToExitClimb = true;
		bLaunchForwardOnExitClimb = false;

		/*if (CharacterOwner->GetLocalRole() == ROLE_Authority)
		{
			SetMovementMode(MOVE_Walking);
		}
		bIsClimbing = false;*/

		//bOrientRotationToMovement = true;
		//CharacterOwner->bUseControllerRotationYaw = false;
		//Cast<APlayerController>(CharacterOwner->GetController())->InputYawScale = 1.f;
		//bUseControllerDesiredRotation = true;
	}
}


//Animation Distance Matching
void UOWSCharacterMovementComponent::PredictJumpApex(const FVector& CharacterLocation, FVector& outApexLocation, FVector& outLandLocation, float& outTimeToApex, bool Debug)
{
	const float Gravity = GetGravityZ();
	float height = GetMaxJumpHeight();
	outTimeToApex = JumpZVelocity / Gravity * -1;

	//calculate Apex by adding forward motion
	outApexLocation = CharacterLocation + (FVector(0, 0, 1) * height) + (Velocity * outTimeToApex);

	//calculate launch velocity for use in predicted land
	FVector LaunchVelocity = Velocity + FVector(0, 0, 1) * JumpZVelocity;

	//Predict landing location using PredictProjectilePath
	FPredictProjectilePathParams Params = FPredictProjectilePathParams(1, CharacterLocation + FVector(0, 0, -90), LaunchVelocity, 2);
	Params.TraceChannel = ECC_Visibility;
	Params.bTraceWithChannel = true;
	Params.bTraceWithCollision = true;
	FPredictProjectilePathResult OutResults;

	bool bhit = UGameplayStatics::PredictProjectilePath(this, Params, OutResults);

	if (bhit) {
		outLandLocation = OutResults.HitResult.Location;
		if (Debug) DrawDebugSphere(GetWorld(), outLandLocation + FVector(0, 0, 90), 20, 26, FColor::Orange, true);
	}

	if (Debug) DrawDebugSphere(GetWorld(), outApexLocation, 20, 26, FColor::Blue, true);

}

FVector UOWSCharacterMovementComponent::GetStoppingDistance(const FVector& CharacterLocation, float Local_WorldDeltaSecond, bool Debug)
{
	if (Debug) DrawDebugSphere(GetWorld(), CharacterLocation, 20, 26, FColor::Green, true);

	// Small number break loop when velocity is less than this value
	float SmallVelocity = 10.f * FMath::Square(Local_WorldDeltaSecond);

	// Current velocity at current frame in unit/frame
	FVector CurrentVelocityInFrame = Velocity * Local_WorldDeltaSecond;

	// Store velocity direction for later use
	FVector CurrentVelocityDirection = Velocity.GetSafeNormal2D();

	// Current deacceleration at current frame in unit/fame^2
	FVector CurrentDeaccelerationInFrame = (CurrentVelocityDirection * BrakingDecelerationWalking) * FMath::Square(Local_WorldDeltaSecond);

	// Calculate number of frames needed to reach zero velocity and gets its int value
	int StopFrameCount = CurrentVelocityInFrame.Size() / CurrentDeaccelerationInFrame.Size();

	// float variable use to store distance to targeted stop location
	float StoppingDistance = 0.0f;

	// Do Stop calculation go through all frames and calculate stop distance in each frame and stack them
	for (int i = 0; i <= StopFrameCount; i++)
	{
		//Update velocity
		CurrentVelocityInFrame -= CurrentDeaccelerationInFrame;

		// if velocity in XY plane is small break loop for safety
		if (CurrentVelocityInFrame.Size2D() <= SmallVelocity)
		{
			break;
		}

		// Calculate distance travel in current frame and add to previous distance
		StoppingDistance += CurrentVelocityInFrame.Size2D();
	}

	// return stopping distance from player position in previous frame
	if (Debug) DrawDebugSphere(GetWorld(), CharacterLocation + CurrentVelocityDirection * StoppingDistance, 20, 26, FColor::Red, true);

	return CharacterLocation + CurrentVelocityDirection * StoppingDistance;
}