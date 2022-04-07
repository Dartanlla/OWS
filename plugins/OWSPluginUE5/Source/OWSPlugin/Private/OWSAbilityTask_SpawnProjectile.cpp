// Fill out your copyright notice in the Description page of Project Settings.

#include "OWSAbilityTask_SpawnProjectile.h"
#include "OWSPlugin.h"
#include "EngineGlobals.h"
#include "Engine/Engine.h"
#include "TimerManager.h"
#include "AbilitySystemComponent.h"
#include "Runtime/Core/Public/Math/TransformNonVectorized.h"
#include "OWSPlayerController.h"



UOWSAbilityTask_SpawnProjectile::UOWSAbilityTask_SpawnProjectile(const FObjectInitializer& ObjectInitializer)
	: Super(ObjectInitializer)
{

}

UOWSAbilityTask_SpawnProjectile* UOWSAbilityTask_SpawnProjectile::SpawnProjectile(UGameplayAbility* OwningAbility, FGameplayAbilityTargetDataHandle TargetData, TSubclassOf<AActor> InClass,
	bool UseAimCamera, bool IgnoreAimCameraPitch, FName StartingSocketName, float ForwardOffsetFromSocket, bool UseFixedStartingLocationRotation, FVector StartingLocation, 
	FRotator StartingRotation, FGameplayEffectSpecHandle DirectDamageEffect, FGameplayEffectSpecHandle AOEDamageEffect, FGameplayTag ActivateAbilityTagOnImpact)
{
	UOWSAbilityTask_SpawnProjectile* MyObj = NewAbilityTask<UOWSAbilityTask_SpawnProjectile>(OwningAbility);
	MyObj->ProjectileClass = InClass;
	MyObj->CachedTargetDataHandle = MoveTemp(TargetData);
	MyObj->bUseAimCamera = UseAimCamera;
	MyObj->bIgnoreAimCameraPitch = IgnoreAimCameraPitch;
	MyObj->nameStartingSocketName = StartingSocketName;
	MyObj->fForwardOffsetFromSocket = ForwardOffsetFromSocket;
	MyObj->bUseFixedStartingLocationRotation = UseFixedStartingLocationRotation;
	MyObj->StartingLocation = StartingLocation;
	MyObj->StartingRotation = StartingRotation;
	MyObj->geshDirectDamageEffect = DirectDamageEffect;
	MyObj->geshAOEDamageEffect = AOEDamageEffect;
	MyObj->tagActivateAbilityTagOnImpact = ActivateAbilityTagOnImpact;
	return MyObj;
}

// ---------------------------------------------------------------------------------------
void UOWSAbilityTask_SpawnProjectile::Activate()
{
	
}


bool UOWSAbilityTask_SpawnProjectile::BeginSpawningActor(UGameplayAbility* OwningAbility, FGameplayAbilityTargetDataHandle TargetData, TSubclassOf<AActor> InClass, AActor*& SpawnedActor)
{
	AOWSPlayerController* OwningPlayer = Cast<AOWSPlayerController>(Ability->GetCurrentActorInfo()->PlayerController);
	if (OwningPlayer)
	{
		UWorld* const World = GEngine->GetWorldFromContextObject(OwningPlayer, EGetWorldErrorMode::LogAndReturnNull);
		if (World)
		{
			AOWSPlayerController* MyOwningPlayer = Cast<AOWSPlayerController>(Ability->GetCurrentActorInfo()->PlayerController);

			float CatchupTickDelta = (MyOwningPlayer ? MyOwningPlayer->GetPredictionTime() : 0.f);

			if ((CatchupTickDelta > 0.f) && !Ability->GetCurrentActorInfo()->IsNetAuthority())
			{
				float SleepTime = MyOwningPlayer->GetProjectileSleepTime();
				if (SleepTime > 0.f)
				{
					UE_LOG(OWS, Verbose, TEXT("BeginSpawningActor: Client SleepTime > 0"));

					//UWorld* World = GetWorld();
					// lag is so high need to delay spawn
					if (!World->GetTimerManager().IsTimerActive(SpawnDelayedFakeProjHandle))
					{
						FTransform SpawnTransform;
						GetAimTransform(SpawnTransform);
						DelayedProjectile.ProjectileClass = ProjectileClass;
						DelayedProjectile.SpawnLocation = SpawnTransform.GetLocation();
						DelayedProjectile.SpawnRotation = SpawnTransform.Rotator();

						UE_LOG(OWS, Verbose, TEXT("BeginSpawningActor: Schedule Delayed Fake Projectile for %f"), SleepTime);
						World->GetTimerManager().SetTimer(SpawnDelayedFakeProjHandle, this, &UOWSAbilityTask_SpawnProjectile::SpawnDelayedFakeProjectile, SleepTime, false);

						if (IsLocallyControlled())
						{
							UE_LOG(OWS, Verbose, TEXT("BeginSpawningActor NULL Client Owner: %s"), *OwningPlayer->GetName());
						}
						else
						{
							UE_LOG(OWS, Verbose, TEXT("BeginSpawningActor NULL Server Owner: %s"), *OwningPlayer->GetName());
						}

						return false;
					}
				}
			}


			if (Ability->GetCurrentActorInfo()->IsNetAuthority() || (CatchupTickDelta > 0.f))
			{
				APawn* MyPawn = Cast<APawn>(Ability->GetCurrentActorInfo()->AvatarActor);
				FActorSpawnParameters Params;
				Params.Instigator = MyPawn;
				Params.Owner = MyPawn;
				Params.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
				
				FTransform SpawnTransform;
				GetAimTransform(SpawnTransform);

				AOWSAdvancedProjectile* NewProjectile = GetWorld()->SpawnActor<AOWSAdvancedProjectile>(ProjectileClass, SpawnTransform.GetLocation(), SpawnTransform.GetRotation().Rotator(), Params);
				if (NewProjectile)
				{
					if (Ability->GetCurrentActorInfo()->IsNetAuthority())
					{
						UE_LOG(OWS, Verbose, TEXT("Server Spawned"));

						NewProjectile->SetInstigator(MyPawn);
						NewProjectile->SetOwner(MyPawn);

						if ((CatchupTickDelta > 0.f) && NewProjectile->ProjectileMovement)
						{
							NewProjectile->TickActor(CatchupTickDelta, LEVELTICK_All, NewProjectile->PrimaryActorTick);

							NewProjectile->ProjectileMovement->TickComponent(CatchupTickDelta, LEVELTICK_All, NULL);
							NewProjectile->SetForwardTicked(true);

							UE_LOG(OWS, Verbose, TEXT("Server Old Life Span: %f"), NewProjectile->GetLifeSpan());

							if (NewProjectile->GetLifeSpan() > 0.f)
							{
								float NewLifeSpan = 0.1f + FMath::Max(0.01f, NewProjectile->GetLifeSpan() - CatchupTickDelta);
								UE_LOG(OWS, Verbose, TEXT("Server New Life Span: %f"), NewLifeSpan);
								NewProjectile->SetLifeSpan(NewLifeSpan);
							}
						}
						else
						{
							UE_LOG(OWS, Verbose, TEXT("Client Spawned NOW!"));

							NewProjectile->SetForwardTicked(false);
						}
					}
					else
					{
						NewProjectile->InitFakeProjectile(OwningPlayer);
						float NewLifeSpan = FMath::Min(NewProjectile->GetLifeSpan(), 2.0f * FMath::Max(0.f, CatchupTickDelta));

						UE_LOG(OWS, Verbose, TEXT("Client New Life Span: %f"), NewLifeSpan);

						//NewProjectile->SetLifeSpan(NewLifeSpan);
					}

					if (geshDirectDamageEffect.IsValid())
					{
						NewProjectile->SetDamageEffectOnHit(geshDirectDamageEffect);
					}

					if (geshAOEDamageEffect.IsValid())
					{
						NewProjectile->SetAoEDamageEffectOnExplosion(geshAOEDamageEffect);
					}

					if (tagActivateAbilityTagOnImpact.IsValid())
					{
						NewProjectile->ActivateAbilityTagOnImpact = tagActivateAbilityTagOnImpact;
					}

					if (ShouldBroadcastAbilityTaskDelegates())
					{
						Success.Broadcast(NewProjectile);
					}

					EndTask();
				}
				else
				{
					if (ShouldBroadcastAbilityTaskDelegates())
					{
						DidNotSpawn.Broadcast(nullptr);
					}
					return false;
				}
			}

		}
	}

	return false;
/*
	if (!Ability)
	{
		return false;
	}

	if (Ability->GetCurrentActorInfo()->IsNetAuthority() && ShouldBroadcastAbilityTaskDelegates())
	{
		UWorld* const World = GEngine->GetWorldFromContextObject(OwningAbility, EGetWorldErrorMode::LogAndReturnNull);
		if (World)
		{
			UE_LOG(OWS, Verbose, TEXT("BeginSpawningActor: Spawn Actor on Server"));
			SpawnedActor = World->SpawnActorDeferred<AActor>(InClass, FTransform::Identity, NULL, NULL, ESpawnActorCollisionHandlingMethod::AlwaysSpawn);
		}
	}
	else
	{
		UE_LOG(OWS, Verbose, TEXT("BeginSpawningActor: Client Start"));

		UWorld* const World = GEngine->GetWorldFromContextObject(OwningAbility, EGetWorldErrorMode::LogAndReturnNull);
		if (World)
		{
			AOWSPlayerController* OwningPlayer = Cast<AOWSPlayerController>(Ability->GetCurrentActorInfo()->PlayerController);

			float CatchupTickDelta = (OwningPlayer ? OwningPlayer->GetPredictionTime() : 0.f);


			if ((CatchupTickDelta > 0.f))
			{
				UE_LOG(OWS, Verbose, TEXT("BeginSpawningActor: Client CatchupTickDelta > 0"));

				float SleepTime = OwningPlayer->GetProjectileSleepTime();
				if (SleepTime > 0.f)
				{
					UE_LOG(OWS, Verbose, TEXT("BeginSpawningActor: Client SleepTime > 0"));

					UWorld* World = GetWorld();
					// lag is so high need to delay spawn
					if (!World->GetTimerManager().IsTimerActive(SpawnDelayedFakeProjHandle))
					{
						FTransform SpawnTransform;
						GetAimTransform(SpawnTransform);
						DelayedProjectile.ProjectileClass = InClass;
						DelayedProjectile.SpawnLocation = SpawnTransform.GetLocation();
						DelayedProjectile.SpawnRotation = SpawnTransform.Rotator();

						UE_LOG(OWS, Verbose, TEXT("BeginSpawningActor: Schedule Delayed Fake Projectile for %f"), SleepTime);
						World->GetTimerManager().SetTimer(SpawnDelayedFakeProjHandle, this, &UOWSAbilityTask_SpawnProjectile::SpawnDelayedFakeProjectile, SleepTime, false);

						if (IsLocallyControlled())
						{
							UE_LOG(OWS, Verbose, TEXT("BeginSpawningActor NULL Client Owner: %s"), *OwningPlayer->GetName());
						}
						else
						{
							UE_LOG(OWS, Verbose, TEXT("BeginSpawningActor NULL Server Owner: %s"), *OwningPlayer->GetName());
						}

						SpawnedActor = NULL;
						return false;
					}
				}
				else
				{
					UE_LOG(OWS, Verbose, TEXT("BeginSpawningActor: Spawn fake projectile on Client Now!"));

					SpawnedActor = World->SpawnActorDeferred<AActor>(InClass, FTransform::Identity, NULL, NULL, ESpawnActorCollisionHandlingMethod::AlwaysSpawn);
				}
			}
			else
			{
				UE_LOG(OWS, Verbose, TEXT("BeginSpawningActor: Client won't spawn a fake!"));

				return false;
			}
		}
	}

	if (SpawnedActor == nullptr)
	{
		if (ShouldBroadcastAbilityTaskDelegates())
		{
			DidNotSpawn.Broadcast(nullptr);
		}
		return false;
	}

	return true;*/
}


void UOWSAbilityTask_SpawnProjectile::SpawnDelayedFakeProjectile()
{
	AOWSPlayerController* OwningPlayer = Cast<AOWSPlayerController>(Ability->GetCurrentActorInfo()->PlayerController);
	if (OwningPlayer)
	{
		if (IsLocallyControlled())
		{
			UE_LOG(OWS, Verbose, TEXT("SpawnDelayedFakeProjectile Client Owner: %s"), *OwningPlayer->GetName());
		}
		else
		{
			UE_LOG(OWS, Verbose, TEXT("SpawnDelayedFakeProjectile Server Owner: %s"), *OwningPlayer->GetName());
		}

		float CatchupTickDelta = (OwningPlayer ? OwningPlayer->GetPredictionTime() : 0.f);

		APawn* MyPawn = Cast<APawn>(Ability->GetCurrentActorInfo()->AvatarActor);
		FActorSpawnParameters Params;
		Params.Instigator = MyPawn;
		Params.Owner = MyPawn;
		Params.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
		AOWSAdvancedProjectile* NewProjectile = GetWorld()->SpawnActor<AOWSAdvancedProjectile>(DelayedProjectile.ProjectileClass, DelayedProjectile.SpawnLocation, DelayedProjectile.SpawnRotation, Params);
		if (NewProjectile)
		{	
			NewProjectile->InitFakeProjectile(OwningPlayer);

			UE_LOG(OWS, Verbose, TEXT("Delayed fake Old Life Span: %f"), NewProjectile->GetLifeSpan());

			//float NewLifeSpan = FMath::Min(NewProjectile->GetLifeSpan(), 2.f * FMath::Max(0.f, CatchupTickDelta));

			float NewLifeSpan = FMath::Min(NewProjectile->GetLifeSpan(), 0.002f * FMath::Max(0.f, OwningPlayer->MaxPredictionPing + OwningPlayer->PredictionFudgeFactor));

			UE_LOG(OWS, Verbose, TEXT("Delayed fake Life Span: %f"), NewLifeSpan);

			NewProjectile->SetLifeSpan(NewLifeSpan);

			if (ShouldBroadcastAbilityTaskDelegates())
			{
				Success.Broadcast(NewProjectile);
			}
		}
	}

	EndTask();
}


void UOWSAbilityTask_SpawnProjectile::FinishSpawningActor(UGameplayAbility* OwningAbility, FGameplayAbilityTargetDataHandle TargetData, AActor* SpawnedActor)
{
	/*if (SpawnedActor)
	{
		UE_LOG(OWS, Verbose, TEXT("FinishSpawningActor has SpawnedActor"));

		AOWSAdvancedProjectile* SpawnedProjectile = Cast<AOWSAdvancedProjectile>(SpawnedActor);
		if (SpawnedProjectile)
		{
			AOWSPlayerController* OwningPlayer = Cast<AOWSPlayerController>(Ability->GetCurrentActorInfo()->PlayerController);
			if (OwningPlayer)
			{
				FTransform SpawnTransform;
				GetAimTransform(SpawnTransform);

				if (IsLocallyControlled())
				{
					UE_LOG(OWS, Verbose, TEXT("FinishSpawning called on Client"));
				}
				else
				{
					UE_LOG(OWS, Verbose, TEXT("FinishSpawning called on Server"));
				}

				SpawnedActor->FinishSpawning(SpawnTransform);

				if (IsLocallyControlled())
				{
					UE_LOG(OWS, Verbose, TEXT("FinishSpawningActor Client Owner: %s"), *OwningPlayer->GetName());
				}
				else
				{
					UE_LOG(OWS, Verbose, TEXT("FinishSpawningActor Server Owner: %s"), *OwningPlayer->GetName());
				}

				float CatchupTickDelta = (OwningPlayer ? OwningPlayer->GetPredictionTime() : 0.f);

				//UE_LOG(LogTemp, Error, TEXT("CatchupTickDelta: %f"), CatchupTickDelta);

				APawn* MyPawn = Cast<APawn>(Ability->GetCurrentActorInfo()->AvatarActor);
				SpawnedProjectile->Instigator = MyPawn;

				if (Ability && Ability->GetCurrentActorInfo()->IsNetAuthority())
				{
					if ((CatchupTickDelta > 0.f) && SpawnedProjectile->ProjectileMovement)
					{
						SpawnedProjectile->TickActor(CatchupTickDelta, LEVELTICK_All, SpawnedActor->PrimaryActorTick);

						SpawnedProjectile->ProjectileMovement->TickComponent(CatchupTickDelta, LEVELTICK_All, NULL);
						SpawnedProjectile->SetForwardTicked(true);

						if (IsLocallyControlled())
						{
							UE_LOG(OWS, Verbose, TEXT("Client Old Life Span: %f"), SpawnedProjectile->GetLifeSpan());
						}
						else
						{
							UE_LOG(OWS, Verbose, TEXT("Server Old Life Span: %f"), SpawnedProjectile->GetLifeSpan());
						}

						if (SpawnedProjectile->GetLifeSpan() > 0.f)
						{
							float NewLifeSpan = 0.1f + FMath::Max(0.01f, SpawnedProjectile->GetLifeSpan() - CatchupTickDelta);

							if (IsLocallyControlled())
							{
								UE_LOG(OWS, Verbose, TEXT("Client New Life Span: %f"), NewLifeSpan);
							}
							else
							{
								UE_LOG(OWS, Verbose, TEXT("Server New Life Span: %f"), NewLifeSpan);
							}

							SpawnedProjectile->SetLifeSpan(NewLifeSpan);
						}
					}
					else
					{
						SpawnedProjectile->SetForwardTicked(false);
					}

					//Set the direct damage effect.  Direct damage is when the projectile directly hits a damagable actor.
					SpawnedProjectile->SetDamageEffectOnHit(geshDirectDamageEffect);
				}
				else
				{
					SpawnedProjectile->InitFakeProjectile(OwningPlayer);
					SpawnedProjectile->SetLifeSpan(FMath::Min(SpawnedProjectile->GetLifeSpan(), 2.f * FMath::Max(0.f, CatchupTickDelta)));

					//Set the direct damage effect.  Direct damage is when the projectile directly hits a damagable actor.
					SpawnedProjectile->SetDamageEffectOnHit(geshDirectDamageEffect);
				}
			}

			
		}

		if (ShouldBroadcastAbilityTaskDelegates())
		{
			Success.Broadcast(SpawnedActor);
		}
	}

	EndTask();*/
}


// ---------------------------------------------------------------------------------------


void UOWSAbilityTask_SpawnProjectile::GetAimTransform(FTransform& SpawnTransform)
{
	bool bTransformSet = false;
	if (bUseAimCamera)
	{
		AOWSPlayerController* OwningPlayer = Cast<AOWSPlayerController>(Ability->GetCurrentActorInfo()->PlayerController);
		check(OwningPlayer)

			if (OwningPlayer)
			{
				FVector ViewStart;
				FRotator ViewRot;

				OwningPlayer->GetPlayerViewPoint(ViewStart, ViewRot);

				//Set pitch to zero to ignore it
				if (bIgnoreAimCameraPitch)
				{
					ViewRot.Pitch = 0.f;
				}

				const FVector ViewDirUnit = ViewRot.Vector().GetSafeNormal();

				ACharacter* MyCharacter = Cast<ACharacter>(Ability->GetCurrentActorInfo()->AvatarActor);

				if (MyCharacter)
				{
					FVector SocketLocation = MyCharacter->GetMesh()->GetSocketLocation(nameStartingSocketName);

					SpawnTransform.SetLocation(SocketLocation + (ViewDirUnit * fForwardOffsetFromSocket));
				}

				SpawnTransform.SetRotation(ViewRot.Quaternion());

				bTransformSet = true;
			}
	}
	else if (bUseFixedStartingLocationRotation)
	{
		SpawnTransform.SetLocation(StartingLocation);

		SpawnTransform.SetRotation(StartingRotation.Quaternion());

		bTransformSet = true;
	}

	if (!bTransformSet)
	{
		if (FGameplayAbilityTargetData* LocationData = CachedTargetDataHandle.Get(0))		//Hardcode to use data 0. It's OK if data isn't useful/valid.
		{
			//Set location. Rotation is unaffected.
			if (LocationData->HasHitResult())
			{
				SpawnTransform.SetLocation(LocationData->GetHitResult()->Location);
				SpawnTransform.SetRotation(FQuat(LocationData->GetHitResult()->Normal.X, LocationData->GetHitResult()->Normal.Y, LocationData->GetHitResult()->Normal.Z, 0.0f));
				bTransformSet = true;
			}
			else if (LocationData->HasEndPoint())
			{
				SpawnTransform = LocationData->GetEndPointTransform();
				bTransformSet = true;
			}
		}
		if (!bTransformSet)
		{
			SpawnTransform = AbilitySystemComponent->GetOwner()->GetTransform();
		}
	}
}