// Fill out your copyright notice in the Description page of Project Settings.

#include "OWSPlayerState.h"
#include "Character/Player/PlayerCharacterBase.h"



void AOWSPlayerState::SetCharacterName(FString CharacterName)
{
	SetPlayerName(CharacterName);
}

APlayerCharacter* AOWSPlayerState::GetCurrentPawn()
{
	return Cast<APlayerCharacter>(GetPawn());
}