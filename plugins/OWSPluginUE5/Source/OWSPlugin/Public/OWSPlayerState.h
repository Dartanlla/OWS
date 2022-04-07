// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "GameFramework/PlayerState.h"
#include "OWSCharacter.h"
#include "OWSPlayerState.generated.h"

/**
 * 
 */
UCLASS()
class OWSPLUGIN_API AOWSPlayerState : public APlayerState
{
	GENERATED_BODY()
	
public:
	UPROPERTY(VisibleAnywhere, BlueprintReadWrite, Category = "Player")
		FVector PlayerStartLocation;

	UPROPERTY(VisibleAnywhere, BlueprintReadWrite, Category = "Player")
		FRotator PlayerStartRotation;

	UPROPERTY(VisibleAnywhere, BlueprintReadWrite, Category = "Player")
		FString DefaultPawnClass;

	UPROPERTY(VisibleAnywhere, BlueprintReadWrite, Category = "Player")
		FString UserSessionGUID;

	UPROPERTY(VisibleAnywhere, BlueprintReadWrite, Category = "Player")
		int32 AlwaysRelevantPartyID;

	UFUNCTION(BlueprintCallable, Category = "Player")
		void SetCharacterName(FString CharacterName);

	UFUNCTION(BlueprintCallable, Category = "Player")
		AOWSCharacter* GetCurrentPawn();
};
