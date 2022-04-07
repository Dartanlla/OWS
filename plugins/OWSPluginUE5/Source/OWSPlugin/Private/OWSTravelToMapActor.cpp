// Fill out your copyright notice in the Description page of Project Settings.

#include "OWSTravelToMapActor.h"
#include "Runtime/Engine/Classes/GameFramework/PlayerState.h"
#include "Runtime/Core/Public/Misc/ConfigCacheIni.h"


// Sets default values
AOWSTravelToMapActor::AOWSTravelToMapActor()
{
 	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

	//Create UOWSPlayerControllerComponent and bind delegates
	OWSPlayerControllerComponent = CreateDefaultSubobject<UOWSPlayerControllerComponent>(TEXT("OWS Player Controller Component"));

	OWSPlayerControllerComponent->OnNotifyGetZoneServerToTravelToDelegate.BindUObject(this, &AOWSTravelToMapActor::NotifyMapServerToTravelTo);
	OWSPlayerControllerComponent->OnErrorGetZoneServerToTravelToDelegate.BindUObject(this, &AOWSTravelToMapActor::ErrorMapServerToTravelTo);
}

// Called when the game starts or when spawned
void AOWSTravelToMapActor::BeginPlay()
{
	Super::BeginPlay();
	
}

// Called every frame
void AOWSTravelToMapActor::Tick( float DeltaTime )
{
	Super::Tick( DeltaTime );

}


void AOWSTravelToMapActor::GetMapServerToTravelTo(APlayerController* PlayerController, TEnumAsByte<ERPGSchemeToChooseMap::SchemeToChooseMap> SelectedSchemeToChooseMap, int32 WorldServerID)
{
	FString CharacterName = PlayerController->PlayerState->GetPlayerName();
	OWSPlayerControllerComponent->GetZoneServerToTravelTo(CharacterName, SelectedSchemeToChooseMap, WorldServerID, ZoneName);
}

FVector2D AOWSTravelToMapActor::GetSphericalFromCartesian(FVector CartesianVector)
{
	return CartesianVector.UnitCartesianToSpherical();
}

FVector AOWSTravelToMapActor::GetCartesianFromSpherical(FVector2D ShpericalVector)
{
	return ShpericalVector.SphericalToUnitCartesian();
}