// Fill out your copyright notice in the Description page of Project Settings.

#include "OWSAdvancedProjectile.h"
#include "OWSPlugin.h"
#include "Net/UnrealNetwork.h"
#include "Runtime/Engine/Classes/GameFramework/Volume.h"
#include "Runtime/Engine/Classes/Components/MeshComponent.h"
#include "Runtime/Engine/Classes/Particles/ParticleSystemComponent.h"
#include "Runtime/Engine/Classes/Components/AudioComponent.h"
#include "Runtime/Engine/Public/Audio.h"
#include "Runtime/Engine/Classes/Sound/SoundBase.h"
#include "Abilities/GameplayAbilityTargetTypes.h"
#include "AbilitySystemBlueprintLibrary.h"

class AOWSCharacterWithAbilities;
class UAbilitySystemComponent;
class UGameplayTagsManager;
class IAbilitySystemInterface;
class UBlueprintGeneratedClass;
struct FScopedPredictionWindow;
struct FGameplayTag;
struct FGameplayEventData;
struct FGameplayCueParameters;

AOWSAdvancedProjectile::AOWSAdvancedProjectile(const class FObjectInitializer& ObjectInitializer)
	: Super(ObjectInitializer)
{
	// Use a sphere as a simple collision representation
	CollisionComp = ObjectInitializer.CreateOptionalDefaultSubobject<USphereComponent>(this, TEXT("SphereComp"));
	if (CollisionComp != NULL)
	{
		CollisionComp->InitSphereRadius(0.0f);
		CollisionComp->BodyInstance.SetCollisionProfileName("Projectile");			// Collision profiles are defined in DefaultEngine.ini
		CollisionComp->OnComponentBeginOverlap.AddDynamic(this, &AOWSAdvancedProjectile::OnOverlapBegin);
		CollisionComp->bTraceComplexOnMove = true;
		CollisionComp->bReceivesDecals = false;
		CollisionComp->bReturnMaterialOnMove = true;
		RootComponent = CollisionComp;
	}

	OverlapRadius = 8.f;
	PawnOverlapSphere = ObjectInitializer.CreateOptionalDefaultSubobject<USphereComponent>(this, TEXT("AssistSphereComp"));
	if (PawnOverlapSphere != NULL)
	{
		PawnOverlapSphere->InitSphereRadius(OverlapRadius);
		PawnOverlapSphere->BodyInstance.SetCollisionProfileName("ProjectileOverlap");
		PawnOverlapSphere->OnComponentBeginOverlap.AddDynamic(this, &AOWSAdvancedProjectile::OnPawnSphereOverlapBegin);
		PawnOverlapSphere->bTraceComplexOnMove = false;
		PawnOverlapSphere->bReceivesDecals = false;
		PawnOverlapSphere->bReturnMaterialOnMove = true;
		PawnOverlapSphere->SetupAttachment(RootComponent);
		PawnOverlapSphere->SetShouldUpdatePhysicsVolume(false);
	}

	// Use a ProjectileMovementComponent to govern this projectile's movement
	ProjectileMovement = ObjectInitializer.CreateDefaultSubobject<UProjectileMovementComponent>(this, TEXT("ProjectileComp"));
	ProjectileMovement->UpdatedComponent = CollisionComp;
	ProjectileMovement->InitialSpeed = 1500.f;
	ProjectileMovement->MaxSpeed = 1500.f;
	ProjectileMovement->bRotationFollowsVelocity = true;
	ProjectileMovement->bShouldBounce = false;
	ProjectileMovement->OnProjectileStop.AddDynamic(this, &AOWSAdvancedProjectile::OnStop);
	//ProjectileMovement->Collsion

	bReplicates = true;
	bNetTemporary = true;

	SetReplicatingMovement(false);
	bFakeClientProjectile = false;
	bMoveFakeToReplicatedPos = true;
	
	MyFakeProjectile = NULL;
	MasterProjectile = NULL;
	bHasSpawnedFully = false;

	NetPriority = 2.f;
	MinNetUpdateFrequency = 100.0f;
}

void AOWSAdvancedProjectile::PreInitializeComponents()
{
	PawnOverlapSphere->OnComponentBeginOverlap.RemoveDynamic(this, &AOWSAdvancedProjectile::OnOverlapBegin);
	PawnOverlapSphere->OnComponentBeginOverlap.RemoveDynamic(this, &AOWSAdvancedProjectile::OnPawnSphereOverlapBegin); // delegate code asserts on duplicates...
	PawnOverlapSphere->OnComponentBeginOverlap.AddDynamic(this, &AOWSAdvancedProjectile::OnPawnSphereOverlapBegin);

	Super::PreInitializeComponents();

	if (GetInstigator() != NULL)
	{
		InstigatorController = GetInstigator()->Controller;
	}

	if (PawnOverlapSphere != NULL)
	{
		if (OverlapRadius == 0.0f)
		{
			PawnOverlapSphere->DestroyComponent();
			PawnOverlapSphere = NULL;
		}
		else
		{
			PawnOverlapSphere->SetSphereRadius(OverlapRadius);
		}
	}

	TArray<UMeshComponent*> MeshComponents;
	GetComponents<UMeshComponent>(MeshComponents);
	for (int32 i = 0; i < MeshComponents.Num(); i++)
	{
		//UE_LOG(UT, Warning, TEXT("%s found mesh %s receive decals %d cast shadow %d"), *GetName(), *MeshComponents[i]->GetName(), MeshComponents[i]->bReceivesDecals, MeshComponents[i]->CastShadow);
		MeshComponents[i]->bUseAsOccluder = false;
		MeshComponents[i]->SetCastShadow(false);
		/*if (bDoVisualOffset && !OffsetVisualComponent)
		{
			OffsetVisualComponent = MeshComponents[i];
			FinalVisualOffset = OffsetVisualComponent->RelativeLocation;
			OffsetVisualComponent->RelativeLocation = InitialVisualOffset;
		}*/
	}

	OnRep_Instigator();
}

void AOWSAdvancedProjectile::OnRep_Instigator()
{
	if (GetInstigator() != NULL)
	{
		//InstigatorTeamNum = GetTeamNum(); // this checks Instigator first

		InstigatorController = GetInstigator()->Controller;
		if (Cast<AOWSAdvancedProjectile>(GetInstigator()))
		{
			((AOWSCharacterWithAbilities*)(GetInstigator()))->LastFiredProjectile = this;
		}
	}
}

void AOWSAdvancedProjectile::BeginPlay()
{
	FString ServerOrClient;
	if (GetNetMode() == NM_DedicatedServer)
	{
		ServerOrClient = "Server";
	}
	else
	{
		ServerOrClient = "Client";
	}

	if (IsPendingKillPending())
	{
		// engine bug that we need to do this before BeginPlay is called on the base class
		return;
	}

	Super::BeginPlay();

	bHasSpawnedFully = true;
	if (GetLocalRole() == ROLE_Authority)
	{
		UE_LOG(OWS, Verbose, TEXT("%s: BeginPlay: Projectile Auth BeginPlay: %s"), *ServerOrClient, *GetName());

		/*
		UNetDriver* NetDriver = GetNetDriver();
		if (NetDriver != NULL && NetDriver->IsServer())
		{
			InitialReplicationTick.Target = this;
			InitialReplicationTick.RegisterTickFunction(GetLevel());
		}
		*/
		/*AOWSCharacterWithAbilities* MyCharacter = Cast<AOWSCharacterWithAbilities>(Instigator);
		if (MyCharacter)
		{
			ProjectilePredictionKey = FPredictionKey::CreateNewPredictionKey(MyCharacter->GetAbilitySystemComponent());
		}*/
	}
	else
	{
		AOWSPlayerController* MyPlayer = Cast<AOWSPlayerController>(InstigatorController ? InstigatorController : GEngine->GetFirstLocalPlayerController(GetWorld()));
		if (MyPlayer)
		{
			UE_LOG(OWS, Verbose, TEXT("%s: BeginPlay: Projectile Not Auth BeginPlay: %s"), *ServerOrClient, *GetName());

			// Move projectile to match where it is on server now (to make up for replication time)
			float CatchupTickDelta = MyPlayer->GetPredictionTime();
			if (CatchupTickDelta > 0.f)
			{
				CatchupTick(CatchupTickDelta);
			}

			// look for associated fake client projectile
			AOWSAdvancedProjectile* BestMatch = NULL;
			FVector VelDir = GetVelocity().GetSafeNormal();
			int32 BestMatchIndex = 0;
			float BestDist = 0.f;

			UE_LOG(OWS, Verbose, TEXT("%s: BeginPlay: Start Searching Fakes"), *ServerOrClient);

			for (int32 i = 0; i < MyPlayer->FakeProjectiles.Num(); i++)
			{
				UE_LOG(OWS, Verbose, TEXT("%s: BeginPlay: Evaluating Fake #: %d"), *ServerOrClient, i);

				AOWSAdvancedProjectile* Fake = MyPlayer->FakeProjectiles[i];
				if (!Fake || Fake->IsPendingKillPending())
				{
					UE_LOG(OWS, Verbose, TEXT("%s: BeginPlay: Invalid Fake or Pending Kill"), *ServerOrClient);

					MyPlayer->FakeProjectiles.RemoveAt(i, 1);
					i--;
				}
				else if (Fake->GetClass() == GetClass())
				{
					UE_LOG(OWS, Verbose, TEXT("%s: BeginPlay: Our Fake Class Matches"), *ServerOrClient);

					// must share direction unless falling! 
					if (CanMatchFake(Fake, VelDir))
					{
						if (BestMatch)
						{
							// see if new one is better
							float NewDist = (Fake->GetActorLocation() - GetActorLocation()).SizeSquared();
							if (BestDist > NewDist)
							{
								BestMatch = Fake;
								BestMatchIndex = i;
								BestDist = NewDist;

								UE_LOG(OWS, Verbose, TEXT("%s: BeginPlay: Projectile Not Auth Found a better Match"), *ServerOrClient);
							}
						}
						else
						{
							BestMatch = Fake;
							BestMatchIndex = i;
							BestDist = (BestMatch->GetActorLocation() - GetActorLocation()).SizeSquared();

							UE_LOG(OWS, Verbose, TEXT("%s: BeginPlay: Projectile Not Auth Found a Match"), *ServerOrClient);
						}
					}
				}
			}
			if (BestMatch)
			{
				UE_LOG(OWS, Verbose, TEXT("%s: BeginPlay: Projectile Not Auth calling BeginFakeProjectileSynch"), *ServerOrClient);

				MyPlayer->FakeProjectiles.RemoveAt(BestMatchIndex, 1);
				BeginFakeProjectileSynch(BestMatch);
			}
			else
			{
				UE_LOG(OWS, Verbose, TEXT("%s: BeginPlay: Projectile Not Auth WE DID NOT FIND A FAKE!"), *ServerOrClient);
			}
		}
	}
}

bool AOWSAdvancedProjectile::CanMatchFake(AOWSAdvancedProjectile* InFakeProjectile, const FVector& VelDir) const
{
	return (ProjectileMovement->ProjectileGravityScale > 0.f) || ((InFakeProjectile->GetVelocity().GetSafeNormal() | VelDir) > 0.95f);
}

void AOWSAdvancedProjectile::CatchupTick(float CatchupTickDelta)
{
	FString ServerOrClient;
	if (GetNetMode() == NM_DedicatedServer)
	{
		ServerOrClient = "Server";
	}
	else
	{
		ServerOrClient = "Client";
	}

	if (ProjectileMovement)
	{
		UE_LOG(OWS, Verbose, TEXT("%s: CatchupTick: %f"), *ServerOrClient, CatchupTickDelta);

		ProjectileMovement->TickComponent(CatchupTickDelta, LEVELTICK_All, NULL);
	}
}

void AOWSAdvancedProjectile::InitFakeProjectile(AOWSPlayerController* OwningPlayer)
{
	FString ServerOrClient;
	if (GetNetMode() == NM_DedicatedServer)
	{
		ServerOrClient = "Server";
	}
	else
	{
		ServerOrClient = "Client";
	}

	bFakeClientProjectile = true;
	if (OwningPlayer)
	{
		//float PredictionPing = OwningPlayer->GetProjectileSleepTime() * 1000.f;

		//ProjectileMovement->InitialSpeed = ProjectileMovement->InitialSpeed / SlowdownFactor;
		//ProjectileMovement->MaxSpeed = ProjectileMovement->MaxSpeed / SlowdownFactor;
		//ProjectileMovement->ProjectileGravityScale = ProjectileMovement->ProjectileGravityScale / SlowdownFactor;

		UE_LOG(OWS, Verbose, TEXT("%s: InitFakeProjectile: Add to Fakes List"), *ServerOrClient);

		OwningPlayer->FakeProjectiles.Add(this);
	}
}

void AOWSAdvancedProjectile::BeginFakeProjectileSynch(AOWSAdvancedProjectile* InFakeProjectile)
{
	FString ServerOrClient;
	if (GetNetMode() == NM_DedicatedServer)
	{
		ServerOrClient = "Server";
	}
	else
	{
		ServerOrClient = "Client";
	}


	if (InFakeProjectile->IsPendingKillPending() || InFakeProjectile->bExploded)
	{
		// Fake projectile is no longer valid to sync to
		return;
	}

	UE_LOG(OWS, Verbose, TEXT("%s: BeginFakeProjectileSynch Incoming Fake Projectile: %s"), *ServerOrClient, *GetNameSafe(InFakeProjectile));

	MyFakeProjectile = InFakeProjectile;
	MyFakeProjectile->MasterProjectile = this;

	float Error = (GetActorLocation() - MyFakeProjectile->GetActorLocation()).Size();

	UE_LOG(OWS, Verbose, TEXT("%s: BeginFakeProjectileSynch Error: %f %s"), *ServerOrClient, Error, *GetNameSafe(this));

	if (((GetActorLocation() - MyFakeProjectile->GetActorLocation()) | MyFakeProjectile->GetVelocity()) > 0.f)
	{
		Error *= -1.f;

		UE_LOG(OWS, Verbose, TEXT("%s: BeginFakeProjectileSynch Error *= -1.f; %s"), *ServerOrClient, *GetNameSafe(this));
	}

	UE_LOG(OWS, Verbose, TEXT("%s: BeginFakeProjectileSynch: %s CORRECTION %f in msec %f"), *ServerOrClient, *GetNameSafe(this), Error, 1000.f * Error / GetVelocity().Size());

	if (bMoveFakeToReplicatedPos)
	{
		FRepMovement RepMovement;
		RepMovement.Location = GetActorLocation();
		RepMovement.Rotation = GetActorRotation();

		//MyFakeProjectile->ReplicatedMovement.Location = GetActorLocation();
		//MyFakeProjectile->ReplicatedMovement.Rotation = GetActorRotation();
		MyFakeProjectile->SetReplicatedMovement(RepMovement);
		MyFakeProjectile->PostNetReceiveLocationAndRotation();

		UE_LOG(OWS, Verbose, TEXT("%s: BeginFakeProjectileSynch: Move Fake To Replicated Pos: %s"), *ServerOrClient, *GetNameSafe(this));
	}
	else
	{
		UE_LOG(OWS, Verbose, TEXT("%S: BeginFakeProjectileSynch: Move Replicated To Fake Pos: %s"), *ServerOrClient, *GetNameSafe(this));

		FRepMovement RepMovement;
		RepMovement.Location = MyFakeProjectile->GetActorLocation();
		RepMovement.Rotation = MyFakeProjectile->GetActorRotation();

		//ReplicatedMovement.Location = MyFakeProjectile->GetActorLocation();
		//ReplicatedMovement.Rotation = MyFakeProjectile->GetActorRotation();
		MyFakeProjectile->SetReplicatedMovement(RepMovement);
		PostNetReceiveLocationAndRotation();
	}

	AOWSPlayerController* MyPlayer = Cast<AOWSPlayerController>(InstigatorController ? InstigatorController : GEngine->GetFirstLocalPlayerController(GetWorld()));
	if (MyPlayer && (GetLifeSpan() != 0.f))
	{
		// remove forward prediction from lifespan
		float CatchupTickDelta = MyPlayer->GetPredictionTime();
		SetLifeSpan(FMath::Max(0.001f, GetLifeSpan() - CatchupTickDelta));
	}
	MyFakeProjectile->SetLifeSpan(GetLifeSpan());
	if (bNetTemporary)
	{
		UE_LOG(OWS, Verbose, TEXT("%s: BeginFakeProjectileSynch: bNetTemporary Destroy: %s"), *ServerOrClient, *GetNameSafe(this));

		// @TODO FIXMESTEVE - will have issues if there are replicated properties that haven't been received yet
		MyFakeProjectile = NULL;
		SetActorHiddenInGame(true);

		//Dart: This causes a duplicate projectile to be spawned as there must be unreplicated properties coming back.
		//Destroy();



		//UE_LOG(UT, Warning, TEXT("%s DESTROY pending kill %d"), *GetName(), IsPendingKillPending());
	}
	else
	{
		// @TODO FIXMESTEVE Can I move components instead of having two actors?
		// @TODO FIXMESTEVE if not, should interp fake projectile to my location instead of teleporting?

		UE_LOG(OWS, Verbose, TEXT("%s: BeginFakeProjectileSynch: Set Projectile Hidden: %s"), *ServerOrClient, *GetNameSafe(this));

		SetActorHiddenInGame(true);
		TArray<USceneComponent*> Components;
		GetComponents<USceneComponent>(Components);
		for (int32 i = 0; i < Components.Num(); i++)
		{
			Components[i]->SetVisibility(false);
		}
	}
}

void AOWSAdvancedProjectile::PreReplication(IRepChangedPropertyTracker & ChangedPropertyTracker)
{
	if ((bForceNextRepMovement || bReplicateUTMovement) && (GetLocalRole() == ROLE_Authority))
	{
		GatherCurrentMovement();
		bForceNextRepMovement = false;
	}
}

void AOWSAdvancedProjectile::GatherCurrentMovement()
{
	/* @TODO FIXMESTEVE support projectiles uing rigid body physics
	UPrimitiveComponent* RootPrimComp = Cast<UPrimitiveComponent>(GetRootComponent());
	if (RootPrimComp && RootPrimComp->IsSimulatingPhysics())
	{
		FRigidBodyState RBState;
		RootPrimComp->GetRigidBodyState(RBState);
		ReplicatedMovement.FillFrom(RBState);
	}
	else
	*/
	if (RootComponent != NULL)
	{
		// If we are attached, don't replicate absolute position
		if (RootComponent->GetAttachParent() != NULL)
		{
			Super::GatherCurrentMovement();
		}
		else
		{
			UTProjReplicatedMovement.Location = RootComponent->GetComponentLocation();
			UTProjReplicatedMovement.Rotation = RootComponent->GetComponentRotation();
			UTProjReplicatedMovement.LinearVelocity = GetVelocity();
		}
	}
}

void AOWSAdvancedProjectile::OnRep_UTProjReplicatedMovement()
{
	if (GetLocalRole() == ROLE_SimulatedProxy)
	{
		//ReplicatedAccel = UTReplicatedMovement.Acceleration;
		FRepMovement tempRepMovement;

		tempRepMovement.Location = UTProjReplicatedMovement.Location;
		tempRepMovement.Rotation = UTProjReplicatedMovement.Rotation;
		tempRepMovement.LinearVelocity = UTProjReplicatedMovement.LinearVelocity;
		tempRepMovement.AngularVelocity = FVector(0.f);
		tempRepMovement.bSimulatedPhysicSleep = false;
		tempRepMovement.bRepPhysics = false;

		SetReplicatedMovement(tempRepMovement);

		OnRep_ReplicatedMovement();
	}
}

void AOWSAdvancedProjectile::PostNetReceiveLocationAndRotation()
{
	FString ServerOrClient;
	if (GetNetMode() == NM_DedicatedServer)
	{
		ServerOrClient = "Server";
	}
	else
	{
		ServerOrClient = "Client";
	}

	UE_LOG(OWS, Verbose, TEXT("%s PostNetReceiveLocationAndRotation PostNetReceiveLocationAndRotation Start: %s"), *ServerOrClient, *GetNameSafe(this));

	if (!bMoveFakeToReplicatedPos && MyFakeProjectile)
	{
		// use fake proj position
		UE_LOG(OWS, Verbose, TEXT("%s PostNetReceiveLocationAndRotation: Use Fake Proj Position: %s"), *ServerOrClient, *GetNameSafe(this));

		FRepMovement tempRepMovement;
		
		tempRepMovement.Location = MyFakeProjectile->GetActorLocation();
		tempRepMovement.Rotation = MyFakeProjectile->GetActorRotation();

		SetReplicatedMovement(tempRepMovement);
	}

	Super::PostNetReceiveLocationAndRotation();

	if (!bMoveFakeToReplicatedPos && MyFakeProjectile)
	{
		UE_LOG(OWS, Verbose, TEXT("%s PostNetReceiveLocationAndRotation: Return Early: %s"), *ServerOrClient, *GetNameSafe(this));

		return;
	}

	//forward predict to get to position on server now
	if (!bFakeClientProjectile)
	{
		AOWSPlayerController* MyPlayer = Cast<AOWSPlayerController>(InstigatorController ? InstigatorController : GEngine->GetFirstLocalPlayerController(GetWorld()));
		if (MyPlayer)
		{
			UE_LOG(OWS, Verbose, TEXT("%s PostNetReceiveLocationAndRotation: Forward predict to get to position on server now: %s"), *ServerOrClient, *GetNameSafe(this));

			float CatchupTickDelta = MyPlayer->GetPredictionTime();
			if ((CatchupTickDelta > 0.f) && ProjectileMovement)
			{
				UE_LOG(OWS, Verbose, TEXT("%s PostNetReceiveLocationAndRotation: TickComponent: %s"), *ServerOrClient, *GetNameSafe(this));

				ProjectileMovement->TickComponent(CatchupTickDelta, LEVELTICK_All, NULL);
			}
		}
	}
	else
	{
		UE_LOG(OWS, Verbose, TEXT("%s PostNetReceiveLocationAndRotation: This projectile is fake: %s"), *ServerOrClient, *GetNameSafe(this));
	}

	if (MyFakeProjectile)
	{
		UE_LOG(OWS, Verbose, TEXT("%s PostNetReceiveLocationAndRotation: MyFakeProjectile: %s"), *ServerOrClient, *GetNameSafe(this));

		FRepMovement tempRepMovement;

		tempRepMovement.Location = GetActorLocation();
		tempRepMovement.Rotation = GetActorRotation();

		MyFakeProjectile->SetReplicatedMovement(tempRepMovement);
		MyFakeProjectile->PostNetReceiveLocationAndRotation();
	}
	else if (GetLocalRole() != ROLE_Authority)
	{
		UE_LOG(OWS, Verbose, TEXT("%s PostNetReceiveLocationAndRotation: Tick Particle Systems: %s"), *ServerOrClient, *GetNameSafe(this));

		// tick particle systems for e.g. SpawnPerUnit trails
		if (!GetTearOff() && !bExploded) // if torn off ShutDown() will do this
		{
			TArray<USceneComponent*> Components;
			GetComponents<USceneComponent>(Components);
			for (int32 i = 0; i < Components.Num(); i++)
			{
				UParticleSystemComponent* PSC = Cast<UParticleSystemComponent>(Components[i]);
				if (PSC != NULL)
				{
					PSC->TickComponent(0.0f, LEVELTICK_All, NULL);
				}
			}
		}
	}
}

void AOWSAdvancedProjectile::PostNetReceiveVelocity(const FVector& NewVelocity)
{
	ProjectileMovement->Velocity = NewVelocity;
	if (MyFakeProjectile)
	{
		UE_LOG(OWS, Verbose, TEXT("PostNetReceiveVelocity: This is MyFakeProjectile: %s"), *GetNameSafe(this));

		MyFakeProjectile->ProjectileMovement->Velocity = NewVelocity;
	}
}


void AOWSAdvancedProjectile::Destroyed()
{
	if (GetLocalRole() == ROLE_Authority)
	{
		UE_LOG(OWS, Verbose, TEXT("Destroyed: Server Destroyed: %s"), *GetNameSafe(this));
	}
	else
	{
		UE_LOG(OWS, Verbose, TEXT("Destroyed: Client Destroyed: %s"), *GetNameSafe(this));
	}

	if (MyFakeProjectile)
	{
		MyFakeProjectile->Destroy();
	}
	GetWorldTimerManager().ClearAllTimersForObject(this);
	Super::Destroyed();
}

void AOWSAdvancedProjectile::ShutDown()
{
	UE_LOG(OWS, Verbose, TEXT("ShutDown: %s"), *GetName());

	if (MyFakeProjectile)
	{
		MyFakeProjectile->ShutDown();
	}
	if (!IsPendingKillPending())
	{
		SetActorEnableCollision(false);
		ProjectileMovement->SetActive(false);
		// hide components that aren't particle systems; deactivate particle systems so they die off naturally; stop ambient sounds
		bool bFoundParticles = false;
		TArray<USceneComponent*> Components;
		GetComponents<USceneComponent>(Components);
		for (int32 i = 0; i < Components.Num(); i++)
		{
			UParticleSystemComponent* PSC = Cast<UParticleSystemComponent>(Components[i]);
			if (PSC != NULL)
			{
				// tick the particles one last time for e.g. SpawnPerUnit effects (particularly noticeable improvement for fast moving projectiles)
				PSC->TickComponent(0.0f, LEVELTICK_All, NULL);
				PSC->DeactivateSystem();
				PSC->bAutoDestroy = true;
				bFoundParticles = true;
			}
			else
			{
				UAudioComponent* Audio = Cast<UAudioComponent>(Components[i]);
				if (Audio != NULL)
				{
					// only stop looping (ambient) sounds - note that the just played explosion sound may be encountered here
					if (Audio->Sound != NULL && Audio->Sound->GetDuration() >= INDEFINITELY_LOOPING_DURATION)
					{
						Audio->Stop();
					}
				}
				else
				{
					Components[i]->SetHiddenInGame(true);
					Components[i]->SetVisibility(false);
				}
			}
		}
		// if some particles remain, defer destruction a bit to give them time to die on their own
		SetLifeSpan((bFoundParticles && GetNetMode() != NM_DedicatedServer) ? 2.0f : 0.2f);

		OnShutdown();
	}

	bExploded = true;
}


void AOWSAdvancedProjectile::TornOff()
{
	UE_LOG(OWS, Verbose, TEXT("TornOff: %s"), *GetName());

	if (bExploded)
	{
		ShutDown(); // make sure it took effect; LifeSpan in particular won't unless we're authority
	}
	else
	{
		FHitResult TornOffHitResult;
		TornOffHitResult.Location = GetActorLocation();
		TornOffHitResult.Normal = FVector(1.0f, 0.0f, 0.0f);
		Explode(TornOffHitResult);
		ShutDown();
	}
}

void AOWSAdvancedProjectile::OnOverlapBegin(UPrimitiveComponent* OverlappedComponent, AActor* OtherActor, UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep, const FHitResult& SweepResult)
{
	//Don't overlap with self
	if (OtherActor == this)
		return;

	//Don't overlap if you are the simulated proxy replicated one time from server to client for synch-up
	if (this->GetLocalRole() == ROLE_SimulatedProxy)
		return;

	//Don't overlap if OtherActor is the simulated proxy replicated one time from server to client for synch-up
	if (OtherActor->IsA(AOWSAdvancedProjectile::StaticClass()) && OtherActor->GetLocalRole() == ROLE_SimulatedProxy)
		return;

	//Don't hit the instigator of this projectile
	if (OtherActor == GetInstigator())
		return;

	if (!bInOverlap)
	{
		TGuardValue<bool> OverlapGuard(bInOverlap, true);
		/*
		if (Role == ROLE_Authority)
		{
			UE_LOG(OWS, Verbose, TEXT("Server: %s::OnOverlapBegin OtherActor:%s bFromSweep:%d"), *GetName(), OtherActor ? *OtherActor->GetName() : TEXT("NULL"), int32(bFromSweep));
		}
		else
		{
			UE_LOG(OWS, Verbose, TEXT("Client: %s::OnOverlapBegin OtherActor:%s bFromSweep:%d"), *GetName(), OtherActor ? *OtherActor->GetName() : TEXT("NULL"), int32(bFromSweep));
		}
		*/
		FHitResult Hit;

		if (bFromSweep)
		{
			Hit = SweepResult;
		}
		else if (CollisionComp != NULL)
		{
			USphereComponent* TestComp = (PawnOverlapSphere != NULL && PawnOverlapSphere->GetUnscaledSphereRadius() > CollisionComp->GetUnscaledSphereRadius()) ? PawnOverlapSphere : CollisionComp;
			OtherComp->SweepComponent(Hit, GetActorLocation() - GetVelocity() * 10.f, GetActorLocation() + GetVelocity(), FQuat::Identity, TestComp->GetCollisionShape(), TestComp->bTraceComplexOnMove);
		}
		else
		{
			FCollisionQueryParams LineTraceCollisionQueryParams = FCollisionQueryParams(GetClass()->GetFName(), false, this);
			LineTraceCollisionQueryParams.bReturnPhysicalMaterial = true;
			OtherComp->LineTraceComponent(Hit, GetActorLocation() - GetVelocity() * 10.f, GetActorLocation() + GetVelocity(), LineTraceCollisionQueryParams);
		}

		ProcessHit(OtherActor, OtherComp, Hit);
	}
}

void AOWSAdvancedProjectile::OnPawnSphereOverlapBegin(UPrimitiveComponent* OverlappedComponent, AActor* OtherActor, UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep, const FHitResult& SweepResult)
{
	if (OtherActor != nullptr)
	{
		FVector OtherLocation;
		if (bFromSweep)
		{
			OtherLocation = SweepResult.Location;
		}
		else
		{
			OtherLocation = OtherActor->GetActorLocation();
		}

		FCollisionQueryParams Params(FName(TEXT("PawnSphereOverlapTrace")), true, this);
		Params.AddIgnoredActor(OtherActor);

		// since PawnOverlapSphere doesn't hit blocking objects, it is possible it is touching a target through a wall
		// make sure that the hit is valid before proceeding
		if (!GetWorld()->LineTraceTestByChannel(OtherLocation, GetActorLocation(), COLLISION_TRACE_WEAPON, Params))
		{
			OnOverlapBegin(CollisionComp, OtherActor, OtherComp, OtherBodyIndex, bFromSweep, SweepResult);
		}
	}
}

void AOWSAdvancedProjectile::OnStop(const FHitResult& Hit)
{
	ProcessHit(Hit.Actor.Get(), Hit.Component.Get(), Hit);
}

bool AOWSAdvancedProjectile::ShouldIgnoreHit_Implementation(AActor* OtherActor, UPrimitiveComponent* OtherComp)
{
	// don't blow up on non-blocking volumes
	// special case not blowing up on teleporters on overlap so teleporters have the option to teleport the projectile
	// don't blow up on weapon redirectors that teleport weapons fire
	// don't blow up from our side on weapon shields; let the shield do that so it can change damage/kill credit
	// ignore client-side actors if will bounce
	// special case not blowing up on Repulsor bubble so that we can reflect / absorb projectiles

	return Cast<AVolume>(OtherActor) != NULL
		|| Cast<AOWSAdvancedProjectile>(OtherActor) != NULL
		|| ((GetLocalRole() != ROLE_Authority) && OtherActor && OtherActor->GetTearOff());
}

void AOWSAdvancedProjectile::ProcessHit_Implementation(AActor* OtherActor, UPrimitiveComponent* OtherComp, const FHitResult& Hit)
{
	//Hidden is the same as destroyed, so don't process hits
	if (IsHidden())
		return;

	//If this projectile has already exploded, stop processing hits
	if (bExploded)
		return;

	UE_LOG(OWS, Verbose, TEXT("ProcessHit_Implementation: %s::ProcessHit fake %d has master %d has fake %d OtherActor:%s"), *GetNameSafe(this), bFakeClientProjectile, (MasterProjectile != NULL), (MyFakeProjectile != NULL), OtherActor ? *OtherActor->GetName() : TEXT("NULL"));
	
	// note: on clients we assume spawn time impact is invalid since in such a case the projectile would generally have not survived to be replicated at all
	if (OtherActor != this && (OtherActor != GetInstigator() || GetInstigator() == NULL /*|| bCanHitInstigator*/) && OtherComp != NULL && !bExploded && (GetLocalRole() == ROLE_Authority || bHasSpawnedFully))
	{
		//DamageImpactedActor(OtherActor, OtherComp, HitLocation, HitNormal);

		if (ShouldIgnoreHit(OtherActor, OtherComp))
		{
			if ((GetLocalRole() != ROLE_Authority) && OtherActor && OtherActor->GetTearOff())
			{
				DamageImpactedActor(OtherActor, OtherComp, Hit);
			}
		}
		else
		{
			if (MyFakeProjectile && !MyFakeProjectile->IsPendingKillPending())
			{
				MyFakeProjectile->ProcessHit_Implementation(OtherActor, OtherComp, Hit);
				Destroy();
				return;
			}
			if (OtherActor != NULL)
			{
				DamageImpactedActor(OtherActor, OtherComp, Hit);
			}

			ImpactedActor = OtherActor;
			Explode(Hit, OtherComp);
			ImpactedActor = NULL;

			if (Cast<AOWSAdvancedProjectile>(OtherActor) != NULL)
			{
				// since we'll probably be destroyed or lose collision here, make sure we trigger the other projectile so shootable projectiles colliding is consistent (both explode)
				UPrimitiveComponent* MyCollider = CollisionComp;
				if (CollisionComp == NULL || CollisionComp->GetCollisionObjectType() != COLLISION_PROJECTILE_SHOOTABLE)
				{
					// our primary collision component isn't the shootable one; try to find one that is
					TArray<UPrimitiveComponent*> Components;
					GetComponents<UPrimitiveComponent>(Components);
					for (int32 i = 0; i < Components.Num(); i++)
					{
						if (Components[i]->GetCollisionObjectType() == COLLISION_PROJECTILE_SHOOTABLE)
						{
							MyCollider = Components[i];
							break;
						}
					}
				}

				((AOWSAdvancedProjectile*)OtherActor)->ProcessHit(this, MyCollider, Hit);
			}
		}
	}	
}

void AOWSAdvancedProjectile::Explode_Implementation(const FHitResult& Hit, UPrimitiveComponent* HitComp)
{
	if (GetLocalRole() == ROLE_Authority)
	{
		UE_LOG(OWS, Verbose, TEXT("Explode_Implementation: Server: Explode! %s"), *GetNameSafe(this));
	}
	else
	{
		UE_LOG(OWS, Verbose, TEXT("Explode_Implementation: Client: Explode! %s"), *GetNameSafe(this));
	}
	bExploded = true;

	if (FMath::IsNearlyZero(ExplosionDamageRadius) || !AoEDamageEffectOnHit.IsValid())
	{
		Destroy();
		return;
	}

	//Play gameplay cue for explosion if there is one
	if (ExplosionGameplayCueTag.IsValid())
	{
		AOWSCharacterWithAbilities* MyCharacter = Cast<AOWSCharacterWithAbilities>(GetInstigator());
		if (MyCharacter)
		{
			FGameplayCueParameters CueParams;
			CueParams.Location = Hit.Location;
			CueParams.Normal = Hit.Normal;
			CueParams.PhysicalMaterial = Hit.PhysMaterial;
			CueParams.SourceObject = this;
			CueParams.Instigator = MyCharacter;
			CueParams.EffectCauser = MyCharacter;
			MyCharacter->GetAbilitySystemComponent()->ExecuteGameplayCue(ExplosionGameplayCueTag, CueParams);
		}
	}

	//Execute GameplayEvent ActivateAbilityTagOnImpact if there is one
	if (ActivateAbilityTagOnImpact.IsValid())
	{
		FGameplayEventData Payload;
		FHitResult HitResult;

		HitResult.Location = GetActorLocation();

		FGameplayAbilityTargetData_SingleTargetHit* SingleHitTargetData = new FGameplayAbilityTargetData_SingleTargetHit();
		SingleHitTargetData->ReplaceHitWith(NULL, &HitResult);

		Payload.Instigator = this;
		Payload.Target = HitComp->GetOwner();
		Payload.EventTag = ActivateAbilityTagOnImpact;
		Payload.TargetData.Add(SingleHitTargetData);

		UE_LOG(OWS, Verbose, TEXT("ActivateAbilityTagOnImpact Activated on Server Only: %s"), *ActivateAbilityTagOnImpact.ToString());
		//FScopedPredictionWindow NewScopedWindow(GetAbilitySystemComponent(), true);
		//GetAbilitySystemComponent()->HandleGameplayEvent(Projectile->ActivateAbilityTagOnImpact, &Payload);
		UAbilitySystemBlueprintLibrary::SendGameplayEventToActor(GetInstigator(), ActivateAbilityTagOnImpact, Payload);
	}

	TArray<FOverlapResult> OverlapResults;
	FCollisionObjectQueryParams CollisionObjectQueryParams;
	FComponentQueryParams QueryParams;

	CollisionObjectQueryParams.AddObjectTypesToQuery(ECollisionChannel::ECC_Pawn);

	QueryParams.AddIgnoredActor(GetInstigator());

	GetWorld()->OverlapMultiByObjectType(OverlapResults, Hit.Location, FQuat(0,0,0,0), CollisionObjectQueryParams, FCollisionShape::MakeSphere(ExplosionDamageRadius), QueryParams);

	for (auto OverlapResult : OverlapResults)
	{
		if (OverlapResult.Actor->IsA(AOWSCharacterWithAbilities::StaticClass()))
		{
			AOWSCharacterWithAbilities* CharacterWhoWasHit = CastChecked<AOWSCharacterWithAbilities>(OverlapResult.Actor);
			if (CharacterWhoWasHit && !CharacterWhoWasHit->IsPendingKill())
			{
				CharacterWhoWasHit->HandleProjectileDamage(this, true);
			}
		}
	}

	Destroy();
}

void AOWSAdvancedProjectile::DamageImpactedActor_Implementation(AActor* OtherActor, UPrimitiveComponent* OtherComp, const FHitResult& Hit)
{	
	if (bFakeClientProjectile)
	{
		return;
	}	

	if (GetLocalRole() == ROLE_Authority)
	{
		UE_LOG(OWS, Verbose, TEXT("Server: DamageImpactedActor_Implementation! %s"), *GetNameSafe(this));
	}
	else
	{
		UE_LOG(OWS, Verbose, TEXT("Client: DamageImpactedActor_Implementation! %s"), *GetNameSafe(this));
	}

	if (OtherActor->IsA(AOWSCharacterWithAbilities::StaticClass()))
	{
		AOWSCharacterWithAbilities* CharacterWhoWasHit = CastChecked<AOWSCharacterWithAbilities>(OtherActor);
		if (CharacterWhoWasHit && !CharacterWhoWasHit->IsPendingKill())
		{
			CharacterWhoWasHit->HandleProjectileDamage(this, false);
		}
	}

	return;



	//We use the InstigatorCharacter because if we use the Hit Character we won't have an owning connecto RPC up the prediction key.
	AOWSCharacterWithAbilities* InstigatorCharacter = CastChecked<AOWSCharacterWithAbilities>(GetInstigator());

	if (InstigatorCharacter && !InstigatorCharacter->IsPendingKill())
	{
//		InstigatorCharacter->HandleProjectileEffectApplicationPrediction(this, OtherActor);

		/*IAbilitySystemInterface* AbilitySystemInterface = Cast<IAbilitySystemInterface>(CharacterWhoWasHit);
		if (AbilitySystemInterface != nullptr)
		{
			UAbilitySystemComponent* AbilitySystemComponent = AbilitySystemInterface->GetAbilitySystemComponent();
			if (AbilitySystemComponent != nullptr)
			{				
				FGameplayEventData Payload;

				Payload.Instigator = Instigator;
				Payload.Target = CharacterWhoWasHit;

				FName AdvancedProjectileDirectDamageEventTagName = FName(TEXT("GameplayEvent.AdvancedProjectile.DirectDamageEvent"));
				FGameplayTag AdvancedProjectileDirectDamageEventTag = UGameplayTagsManager::Get().RequestGameplayTag(AdvancedProjectileDirectDamageEventTagName);

				TWeakObjectPtr<UAbilitySystemComponent> ASC = AbilitySystemComponent;
				//ensureMsgf(ASC.IsValid(), TEXT("UAbilitySystemBlueprintLibrary::SendGameplayEventToActor: Invalid ability system component retrieved from Actor %s. EventTag was %s"), *Actor->GetName(), *EventTag.ToString());
				FScopedPredictionWindow NewScopedWindow(AbilitySystemComponent, true);
				AbilitySystemComponent->HandleGameplayEvent(AdvancedProjectileDirectDamageEventTag, &Payload);

				UE_LOG(OWS, Error, TEXT("Send GameplayEvent.AdvancedProjectile.DirectDamageEvent"));
			}
		}*/
	}
	else //Hit something other than an AOWSCharacterWithAbilities
	{

	}


	/*
	AController* ResolvedInstigator = InstigatorController;
	TSubclassOf<UDamageType> ResolvedDamageType = MyDamageType;
	bool bSameTeamDamage = false;
	if (FFInstigatorController != NULL && InstigatorController != NULL)
	{
		AOWSGameState* GS = GetWorld()->GetGameState<AUTGameState>();
		if (GS != NULL && GS->OnSameTeam(OtherActor, InstigatorController))
		{
			bSameTeamDamage = true;
			ResolvedInstigator = FFInstigatorController;
			if (FFDamageType != NULL)
			{
				ResolvedDamageType = FFDamageType;
			}
		}
	}
	if ((Role == ROLE_Authority) && (HitsStatsName != NAME_None) && !bSameTeamDamage)
	{
		AOWSPlayerState* PS = InstigatorController ? Cast<AUTPlayerState>(InstigatorController->PlayerState) : NULL;
		if (PS)
		{
			PS->ModifyStatsValue(HitsStatsName, StatsHitCredit);
		}
	}

	// treat as point damage if projectile has no radius
	if (DamageParams.OuterRadius > 0.0f)
	{
		FUTRadialDamageEvent Event;
		Event.BaseMomentumMag = Momentum;
		Event.Params = GetDamageParams(OtherActor, HitLocation, Event.BaseMomentumMag);
		Event.Params.MinimumDamage = Event.Params.BaseDamage; // force full damage for direct hit
		Event.DamageTypeClass = ResolvedDamageType;
		Event.Origin = HitLocation;
		new(Event.ComponentHits) FHitResult(OtherActor, OtherComp, HitLocation, HitNormal);
		Event.ComponentHits[0].TraceStart = HitLocation - GetVelocity();
		Event.ComponentHits[0].TraceEnd = HitLocation + GetVelocity();
		Event.ShotDirection = GetVelocity().GetSafeNormal();
		Event.BaseMomentumMag = ((Momentum == 0.f) && Cast<AUTCharacter>(OtherActor) && ((AUTCharacter*)(OtherActor))->IsDead()) ? 20000.f : Momentum;
		OtherActor->TakeDamage(Event.Params.BaseDamage, Event, ResolvedInstigator, this);
	}
	else
	{
		FUTPointDamageEvent Event;
		float AdjustedMomentum = Momentum;
		Event.Damage = GetDamageParams(OtherActor, HitLocation, AdjustedMomentum).BaseDamage;
		Event.DamageTypeClass = ResolvedDamageType;
		Event.HitInfo = FHitResult(OtherActor, OtherComp, HitLocation, HitNormal);
		Event.ShotDirection = GetVelocity().GetSafeNormal();
		AdjustedMomentum = ((AdjustedMomentum == 0.f) && Cast<AUTCharacter>(OtherActor) && ((AUTCharacter*)(OtherActor))->IsDead()) ? 20000.f : Momentum;
		Event.Momentum = Event.ShotDirection * AdjustedMomentum;
		OtherActor->TakeDamage(Event.Damage, Event, ResolvedInstigator, this);
	}*/
}

// Called every frame
void AOWSAdvancedProjectile::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	//Catchup


}

void AOWSAdvancedProjectile::SetDamageEffectOnHit(FGameplayEffectSpecHandle DamageEffect)
{
	DamageEffectOnHit = DamageEffect;
}

void AOWSAdvancedProjectile::SetAoEDamageEffectOnExplosion(FGameplayEffectSpecHandle DamageEffect)
{
	AoEDamageEffectOnHit = DamageEffect;
}




/*
void AOWSAdvancedProjectile::SetPredictionKey(FPredictionKey PredictionKey)
{
	UE_LOG(OWS, Error, TEXT("Server: SetPredictionKey"));
}*/

void AOWSAdvancedProjectile::GetLifetimeReplicatedProps(TArray<class FLifetimeProperty>& OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);

	UBlueprintGeneratedClass* BPClass = Cast<UBlueprintGeneratedClass>(GetClass());
	if (BPClass != NULL)
	{
		BPClass->GetLifetimeBlueprintReplicationList(OutLifetimeProps);
	}

	//DOREPLIFETIME(AActor, IsHidden());
	//DOREPLIFETIME(AActor, bTearOff);
	//DOREPLIFETIME(AActor, CanBeDamaged());

	// POLGE TODO: Fix the issues with this being private
	//DOREPLIFETIME(AActor, AttachmentReplication);

	//DOREPLIFETIME(AActor, GetInstigator());
	DOREPLIFETIME_CONDITION(AOWSAdvancedProjectile, UTProjReplicatedMovement, COND_SimulatedOrPhysics);
	//DOREPLIFETIME_CONDITION(AOWSAdvancedProjectile, ProjectilePredictionKey, COND_OwnerOnly);

	
	//DOREPLIFETIME(AOWSAdvancedProjectile, Slomo);
}
