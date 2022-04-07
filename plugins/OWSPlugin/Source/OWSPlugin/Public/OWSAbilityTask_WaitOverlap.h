// Copyright 2017 Sabre Dart Studios

#pragma once

#include "CoreMinimal.h"
#include "UObject/ObjectMacros.h"
#include "Abilities/Tasks/AbilityTask.h"
#include "OWSAbilityTask_WaitOverlap.generated.h"

DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FWaitOverlapDelegate, const FGameplayAbilityTargetDataHandle&, TargetData);

class AActor;
class UPrimitiveComponent;

/**
 * 
 */
UCLASS()
class OWSPLUGIN_API UOWSAbilityTask_WaitOverlap : public UAbilityTask
{
	GENERATED_UCLASS_BODY()

	UPROPERTY(BlueprintAssignable)
	FWaitOverlapDelegate	OnOverlap;

	/*UFUNCTION()
		void OnHitCallback(UPrimitiveComponent* HitComp, AActor* OtherActor, UPrimitiveComponent* OtherComp, FVector NormalImpulse, const FHitResult& Hit);*/
	UFUNCTION()
		void OnOverlapCallback(UPrimitiveComponent* OverlappedComp, AActor* OtherActor, UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep, const FHitResult& SweepResult);

	virtual void Activate() override;

	/** Wait until an overlap (not hit) occurs. */
	UFUNCTION(BlueprintCallable, Category = "Ability|Tasks", meta = (HidePin = "OwningAbility", DefaultToSelf = "OwningAbility", BlueprintInternalUseOnly = "TRUE"))
		static UOWSAbilityTask_WaitOverlap* RPGWaitForOverlap(UGameplayAbility* OwningAbility, UPrimitiveComponent* WeaponCollisionPrimitive);

private:
	//TSubclassOf<UPrimitiveComponent> P

	virtual void OnDestroy(bool AbilityEnded) override;

	UPrimitiveComponent* GetComponent();	

	UPrimitiveComponent* CollisionPrimitive;
};


