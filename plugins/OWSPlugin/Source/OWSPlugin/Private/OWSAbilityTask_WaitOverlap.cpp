// Copyright 2017 Sabre Dart Studios

#include "OWSAbilityTask_WaitOverlap.h"



UOWSAbilityTask_WaitOverlap::UOWSAbilityTask_WaitOverlap(const FObjectInitializer& ObjectInitializer)
	: Super(ObjectInitializer)
{

}

/*
void URPGAbilityTask_WaitOverlap::OnHitCallback(UPrimitiveComponent* HitComp, AActor* OtherActor, UPrimitiveComponent* OtherComp, FVector NormalImpulse, const FHitResult& Hit)
{
	if (OtherActor)
	{
		// Construct TargetData
		FGameplayAbilityTargetData_SingleTargetHit * TargetData = new FGameplayAbilityTargetData_SingleTargetHit(Hit);

		// Give it a handle and return
		FGameplayAbilityTargetDataHandle	Handle;
		Handle.Data.Add(TSharedPtr<FGameplayAbilityTargetData>(TargetData));
		OnOverlap.Broadcast(Handle);

		// We are done. Kill us so we don't keep getting broadcast messages
		EndTask();
	}
}
*/

void UOWSAbilityTask_WaitOverlap::OnOverlapCallback(UPrimitiveComponent* OverlappedComp, AActor* OtherActor, UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep, const FHitResult& SweepResult)
{
	if (OtherActor && OtherComp)
	{	
		if (OtherActor != GetAvatarActor())
		{
			// Construct TargetData
			FGameplayAbilityTargetData_SingleTargetHit * TargetData = new FGameplayAbilityTargetData_SingleTargetHit(SweepResult);

			// Give it a handle and return
			FGameplayAbilityTargetDataHandle	Handle;
			Handle.Data.Add(TSharedPtr<FGameplayAbilityTargetData>(TargetData));
			OnOverlap.Broadcast(Handle);

			// We are done. Kill us so we don't keep getting broadcast messages
			EndTask();
		}
	}
}

UOWSAbilityTask_WaitOverlap* UOWSAbilityTask_WaitOverlap::RPGWaitForOverlap(UGameplayAbility* OwningAbility, UPrimitiveComponent* WeaponCollisionPrimitive)
{	
	auto MyObj = NewAbilityTask<UOWSAbilityTask_WaitOverlap>(OwningAbility);
	MyObj->CollisionPrimitive = WeaponCollisionPrimitive;
	return MyObj;
}

void UOWSAbilityTask_WaitOverlap::Activate()
{
	SetWaitingOnAvatar();

	UPrimitiveComponent* PrimComponent = GetComponent();
	if (PrimComponent)
	{
		TArray<AActor*> OverlappingActors;
		PrimComponent->GetOverlappingActors(OverlappingActors, TSubclassOf<APawn>());

		if (OverlappingActors.Num() > 0)
		{
			if (OverlappingActors.Top() != GetAvatarActor())
			{
				//UE_LOG(LogTemp, Error, TEXT("Overlapping Actor: %s"), *OverlappingActors.Top()->GetName());
								
				FHitResult HitResult;

				HitResult.Actor = OverlappingActors.Top();
				HitResult.bBlockingHit = true;
				HitResult.Location = PrimComponent->GetComponentLocation();
				
				// Construct TargetData
				FGameplayAbilityTargetData_SingleTargetHit * TargetData = new FGameplayAbilityTargetData_SingleTargetHit(HitResult);

				// Give it a handle and return
				FGameplayAbilityTargetDataHandle	Handle;
				Handle.Data.Add(TSharedPtr<FGameplayAbilityTargetData>(TargetData));
				OnOverlap.Broadcast(Handle);

				// We are done. Kill us so we don't keep getting broadcast messages
				EndTask();
				return;
			}
		}

		PrimComponent->OnComponentBeginOverlap.AddDynamic(this, &UOWSAbilityTask_WaitOverlap::OnOverlapCallback);
	}
}

void UOWSAbilityTask_WaitOverlap::OnDestroy(bool AbilityEnded)
{
	UPrimitiveComponent* PrimComponent = GetComponent();
	if (PrimComponent)
	{
		PrimComponent->OnComponentBeginOverlap.RemoveDynamic(this, &UOWSAbilityTask_WaitOverlap::OnOverlapCallback);
	}

	Super::OnDestroy(AbilityEnded);
}

UPrimitiveComponent* UOWSAbilityTask_WaitOverlap::GetComponent()
{
	// TEMP - we are just using root component's collision. A real system will need more data to specify which component to use
	/*UPrimitiveComponent * PrimComponent = nullptr;
	AActor* ActorOwner = GetAvatarActor();
	if (ActorOwner)
	{
		PrimComponent = ActorOwner->FindComponentByClass<UPrimitiveComponent>();
		*/
		/*
		PrimComponent = Cast<UPrimitiveComponent>(ActorOwner->GetRootComponent());
		if (!PrimComponent)
		{			
			PrimComponent = ActorOwner->FindComponentByClass<UPrimitiveComponent>();
		}
		*/

		/*TArray<UPrimitiveComponent*> AllPrimitiveComponents;
		ActorOwner->GetComponents<UPrimitiveComponent>(AllPrimitiveComponents);

		for (int32 i = 0; i < AllPrimitiveComponents.Num(); i++)
		{
			if (AllPrimitiveComponents[i])
			{
				if (AllPrimitiveComponents[i]->IsA())
				{
					PrimComponent = AllPrimitiveComponents[i];
					break;
				}
			}
		}*/
	//}	

	//return PrimComponent;
	return CollisionPrimitive;
}


