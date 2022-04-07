// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "Runtime/Engine/Classes/Components/SphereComponent.h"
#include "OWSProjectileMovementComponent.h"
#include "OWSPlayerController.h"
#include "OWSAdvancedProjectile.generated.h"

struct FGameplayEffectSpecHandle;
struct FPredictionKey;

/** Replicated movement data of our RootComponent.
* More efficient than engine's FRepMovement
*/
USTRUCT()
struct FRepUTProjMovement
{
	GENERATED_USTRUCT_BODY()

		UPROPERTY()
		FVector_NetQuantize LinearVelocity;

	UPROPERTY()
		FVector_NetQuantize Location;

	UPROPERTY()
		FRotator Rotation;

	FRepUTProjMovement()
		: LinearVelocity(ForceInit)
		, Location(ForceInit)
		, Rotation(ForceInit)
	{}

	bool NetSerialize(FArchive& Ar, class UPackageMap* Map, bool& bOutSuccess)
	{
		bOutSuccess = true;

		bool bOutSuccessLocal = true;

		// update location, linear velocity
		Location.NetSerialize(Ar, Map, bOutSuccessLocal);
		bOutSuccess &= bOutSuccessLocal;
		Rotation.SerializeCompressed(Ar);
		LinearVelocity.NetSerialize(Ar, Map, bOutSuccessLocal);
		bOutSuccess &= bOutSuccessLocal;

		return true;
	}

	bool operator==(const FRepUTProjMovement& Other) const
	{
		if (LinearVelocity != Other.LinearVelocity)
		{
			return false;
		}

		if (Location != Other.Location)
		{
			return false;
		}

		if (Rotation != Other.Rotation)
		{
			return false;
		}
		return true;
	}

	bool operator!=(const FRepUTProjMovement& Other) const
	{
		return !(*this == Other);
	}
};


UCLASS(meta = (ChildCanTick))
class OWSPLUGIN_API AOWSAdvancedProjectile : public AActor
{
	GENERATED_UCLASS_BODY()
	
protected:	
	// Sets default values for this actor's properties
	//AOWSAdvancedProjectile();

	/** Used for replication of our RootComponent's position and velocity */
	UPROPERTY(ReplicatedUsing = OnRep_UTProjReplicatedMovement)
		struct FRepUTProjMovement UTProjReplicatedMovement;

	/** UTProjReplicatedMovement struct replication event */
	UFUNCTION()
		virtual void OnRep_UTProjReplicatedMovement();

	virtual void GatherCurrentMovement() override;

	virtual void OnRep_Instigator() override;
	
	/** Sphere collision component */
	UPROPERTY(VisibleDefaultsOnly, BlueprintReadOnly, Category = Projectile)
		USphereComponent* CollisionComp;

	/** set to force bReplicatedMovement for the next replication pass; this is used with SendInitialReplication() to make sure the client receives the post-physics location in addition
	 * to spawning with the pre-physics location
	 */
	bool bForceNextRepMovement;

	/** re-entrancy guard */
	bool bInOverlap;

	/** This is a fake owning client projectile */
	bool bFakeClientProjectile;

	/** Fake projectile on this client providing visuals for me */
	UPROPERTY()
		AOWSAdvancedProjectile* MyFakeProjectile;

	/** If true (usually), move fake to server replicated position. */
	UPROPERTY(EditDefaultsOnly, BlueprintReadOnly, Category = Projectile)
		bool bMoveFakeToReplicatedPos;

	/** Real projectile for which this projectile is providing visuals */
	UPROPERTY()
		AOWSAdvancedProjectile* MasterProjectile;

	/** Overlap sphere for hitting pawns
	 * NOTE: intentionally hidden from defaults editor so users don't mistakenly modify this when they meant to touch OverlapRadius
	 */
	UPROPERTY(BlueprintReadOnly, Category = Projectile)
		USphereComponent* PawnOverlapSphere;

	UPROPERTY(BlueprintReadWrite, Category = Projectile)
		AController* InstigatorController;

	/** true if already exploded (to avoid recursion, etc) */
	bool bExploded;

	/** Return true if InFakeProjectile is a possible match for this projectile. */
	virtual bool CanMatchFake(AOWSAdvancedProjectile* InFakeProjectile, const FVector& VelDir) const;

	/** Synchronize replicated projectile with the associated client-side fake projectile */
	virtual void BeginFakeProjectileSynch(AOWSAdvancedProjectile* InFakeProjectile);

	/** Server catchup ticking for client's projectile */
	virtual void CatchupTick(float CatchupTickDelta);

	virtual void PreInitializeComponents() override;
	virtual void TornOff() override;
	virtual void Destroyed() override;

	UFUNCTION()
		virtual void OnOverlapBegin(UPrimitiveComponent* OverlappedComponent, AActor* OtherActor, UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep, const FHitResult& SweepResult);
	UFUNCTION()
		virtual void OnPawnSphereOverlapBegin(UPrimitiveComponent* OverlappedComponent, AActor* OtherActor, UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep, const FHitResult& SweepResult);

	/** actor we hit directly and already applied damage to */
	UPROPERTY()
		AActor* ImpactedActor;

	/** returns whether the projectile should ignore a potential overlap hit and keep going
	 * split from ProcessHit() as some projectiles want custom hit behavior but the same exclusions
	 */
	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = Projectile)
		bool ShouldIgnoreHit(AActor* OtherActor, UPrimitiveComponent* OtherComp);
	/** called when projectile hits something */
	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = Projectile)
		void ProcessHit(AActor* OtherActor, UPrimitiveComponent* OtherComp, const FHitResult& Hit);
	/** deal damage to Actor directly hit (note that this Actor will then be ignored for any radial damage) */
	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = Projectile)
		void DamageImpactedActor(AActor* OtherActor, UPrimitiveComponent* OtherComp, const FHitResult& Hit);
	UFUNCTION()
		virtual void OnStop(const FHitResult& Hit);

	UFUNCTION(BlueprintNativeEvent, BlueprintCallable, Category = Projectile)
		void Explode(const FHitResult& Hit, UPrimitiveComponent* HitComp = NULL);


	/** turns off projectile ambient effects, collision, physics, etc
	 * needed because we need a delay between explosion and actor destruction for replication purposes
	 */
	UFUNCTION(BlueprintCallable, Category = Projectile)
		virtual void ShutDown();
	/** blueprint hook for shutdown in case any blueprint-created effects need to be turned off */
	UFUNCTION(BlueprintImplementableEvent)
		void OnShutdown();

	/** True once fully spawned, to avoid destroying replicated projectiles during spawn on client */
	UPROPERTY()
		bool bHasSpawnedFully;

	// Called when the game starts or when spawned
	virtual void BeginPlay() override;

	virtual void PostNetReceiveLocationAndRotation() override;
	virtual void PostNetReceiveVelocity(const FVector& NewVelocity) override;
	virtual void PreReplication(IRepChangedPropertyTracker & ChangedPropertyTracker) override;

public:	
	// Called every frame
	virtual void Tick(float DeltaTime) override;

	/** additional Z axis speed added to projectile on spawn - NOTE: blueprint changes only work in defaults or construction script as value is applied to velocity on spawn */
	//UPROPERTY(EditDefaultsOnly, BlueprintReadWrite, Category = Projectile)
	//	float TossZ;

	/** Projectile movement component */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Projectile)
		UProjectileMovementComponent* ProjectileMovement;

	/** enables UT optimized movement replication; USE THIS INSTEAD OF bReplicateMovement */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Replication)
		bool bReplicateUTMovement;

	/** Projectile size for hitting pawns
	 * if set to zero, the extra component used for this feature will not be attached (perf improvement) but means you can't go from 0 at spawn -> 1+ after spawn
	 */
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = Projectile)
		float OverlapRadius;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Projectile)
		float ExplosionDamageRadius;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Projectile)
		float ExplosionDamageScale;

	UPROPERTY()
		FGameplayEffectSpecHandle DamageEffectOnHit;

	UPROPERTY()
		FGameplayEffectSpecHandle AoEDamageEffectOnHit;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Projectile)
		FGameplayTag ExplosionGameplayCueTag;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Projectile)
		FGameplayTag ActivateAbilityTagOnImpact;

	/*
	UPROPERTY(Replicated)
		FPredictionKey ProjectilePredictionKey;
	*/

	/** Perform any custom initialization for this projectile as fake client side projectile */
	virtual void InitFakeProjectile(class AOWSPlayerController* OwningPlayer);

	UFUNCTION(BlueprintCallable, Category = Projectile)
		void SetDamageEffectOnHit(FGameplayEffectSpecHandle DamageEffect);

	UFUNCTION(BlueprintCallable, Category = Projectile)
		void SetAoEDamageEffectOnExplosion(FGameplayEffectSpecHandle DamageEffect);

	/*UFUNCTION(Category = Projectile)
		void SetPredictionKey(FPredictionKey PredictionKey);*/

	/** Called if server isn't ticking this projectile to make up for network latency. */
	virtual void SetForwardTicked(bool bWasForwardTicked) {};
	
};
