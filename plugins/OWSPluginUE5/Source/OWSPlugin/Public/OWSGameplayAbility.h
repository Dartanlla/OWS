// Copyright 2018 Sabre Dart Studios

#pragma once

#include "CoreMinimal.h"
#include "OWSCharacterWithAbilities.h"
#include "OWSPlayerController.h"
#include "Abilities/GameplayAbility.h"
#include "Runtime/Engine/Classes/Camera/CameraComponent.h"
#include "OWSGameplayAbility.generated.h"

/**
 * 
 */
UCLASS()
class OWSPLUGIN_API UOWSGameplayAbility : public UGameplayAbility
{
	GENERATED_BODY()
	
protected:

	UFUNCTION(BlueprintPure, Category = "Ability|OWS")
		AOWSCharacterWithAbilities* GetOWSAvatarActor();
	
	UFUNCTION(BlueprintPure, Category = "Ability|OWS")
		AOWSPlayerController* GetOWSPlayerController();

	UFUNCTION(BlueprintPure, Category = "Ability|OWS")
		FGameplayAbilityTargetingLocationInfo MakeTargetLocationInfoFromCamera(const UCameraComponent* FollowCamera);

	UFUNCTION(BlueprintCallable, Category = "Ability|OWS")
		void OnlyEndAbilityOnServer();

};
