// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/ProjectileMovementComponent.h"
#include "OWSProjectileMovementComponent.generated.h"
/*
#define REPLACEMENT_OPERATOR_NEW_AND_DELETE \
	OPERATOR_NEW_MSVC_PRAGMA void* operator new  ( size_t Size                        ) OPERATOR_NEW_THROW_SPEC      { return FMemory::Malloc( Size ); } \
	OPERATOR_NEW_MSVC_PRAGMA void* operator new[]( size_t Size                        ) OPERATOR_NEW_THROW_SPEC      { return FMemory::Malloc( Size ); } \
	OPERATOR_NEW_MSVC_PRAGMA void* operator new  ( size_t Size, const std::nothrow_t& ) OPERATOR_NEW_NOTHROW_SPEC    { return FMemory::Malloc( Size ); } \
	OPERATOR_NEW_MSVC_PRAGMA void* operator new[]( size_t Size, const std::nothrow_t& ) OPERATOR_NEW_NOTHROW_SPEC    { return FMemory::Malloc( Size ); } \
	void operator delete  ( void* Ptr )                                                 OPERATOR_DELETE_THROW_SPEC   { FMemory::Free( Ptr ); } \
	void operator delete[]( void* Ptr )                                                 OPERATOR_DELETE_THROW_SPEC   { FMemory::Free( Ptr ); } \
	void operator delete  ( void* Ptr, const std::nothrow_t& )                          OPERATOR_DELETE_NOTHROW_SPEC { FMemory::Free( Ptr ); } \
	void operator delete[]( void* Ptr, const std::nothrow_t& )                          OPERATOR_DELETE_NOTHROW_SPEC { FMemory::Free( Ptr ); }
*/
/**
 * 
 */
UCLASS(ClassGroup = Movement, meta = (BlueprintSpawnableComponent))
class OWSPLUGIN_API UOWSProjectileMovementComponent : public UProjectileMovementComponent
{
	GENERATED_UCLASS_BODY()

	/** linear acceleration in the direction of current velocity */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Projectile)
		float AccelRate;

	/** Don't clear physics volume when stopped */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Projectile)
		bool bKeepPhysicsVolumeWhenStopped;

	/** explicit acceleration vector (additive with AccelRate) */
	UPROPERTY(EditInstanceOnly, BlueprintReadWrite, Category = Projectile)
		FVector Acceleration;

	UPROPERTY(replicated)
		FVector ReplicatedAcceleration;
	
	/** stop only if HitNormal.Z is greater than this value, otherwise continue moving after removing velocity in the impact direction
	 * primarily this is used to only stop the projectile on hitting a floor
	 * no effect if bBounce is true
	 */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Projectile, Meta = (EditCondition = "!bBounce"))
		float HitZStopSimulatingThreshold;

	/** additional components that should be moved along with the main UpdatedComponent. Defaults to all colliding children of UpdatedComponent.
	 * closest blocking hit of all components is used for blocking collision
	 *
	 * Ultimately this is a workaround for UPrimitiveComponent::MoveComponent() not sweeping children.
	 */
	UPROPERTY(BlueprintReadOnly, Category = MovementComponent)
		TArray<UPrimitiveComponent*> AddlUpdatedComponents;

	virtual void InitializeComponent() override;

	virtual FVector ComputeVelocity(FVector InitialVelocity, float DeltaTime) const override
	{
		InitialVelocity += InitialVelocity.GetSafeNormal() * AccelRate * DeltaTime + Acceleration * DeltaTime;
		return Super::ComputeVelocity(InitialVelocity, DeltaTime);
	}

	virtual void TickComponent(float DeltaTime, enum ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction) override;

	UFUNCTION(Server, WithValidation, Reliable)
		void ServerUpdateState(FVector InAcceleration);

	void UpdateState(float DeltaSeconds);

	/** If true, projectile does not home in Z axis */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Projectile, Meta = (EditCondition = "!bBounce"))
		bool bPreventZHoming;

	virtual FVector ComputeHomingAcceleration(const FVector& InVelocity, float DeltaTime) const override;

	virtual void SetUpdatedComponent(USceneComponent* NewUpdatedComponent) override;

	// public access to HandleImpact() for handling hits caused by other actors (e.g. lifts) that we want to process as if the projectile move caused the impact
	void SimulateImpact(const FHitResult& Hit)
	{
		if (UpdatedComponent != nullptr)
		{
			HandleImpact(Hit);
		}
	}

protected:

	virtual void HandleImpact(const FHitResult& Hit, float TimeSlice = 0.0f, const FVector& MoveDelta = FVector::ZeroVector) override;

	virtual bool MoveUpdatedComponentImpl(const FVector& Delta, const FQuat& NewRotation, bool bSweep, FHitResult* OutHit, ETeleportType Teleport = ETeleportType::None) override;
	
};
