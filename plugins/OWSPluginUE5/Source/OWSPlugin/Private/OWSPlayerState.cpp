// Fill out your copyright notice in the Description page of Project Settings.

#include "OWSPlayerState.h"




void AOWSPlayerState::SetCharacterName(FString CharacterName)
{
	SetPlayerName(CharacterName);
}

AOWSCharacter* AOWSPlayerState::GetCurrentPawn()
{
	return Cast<AOWSCharacter>(GetPawn());
}