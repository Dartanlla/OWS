// Fill out your copyright notice in the Description page of Project Settings.

#include "OWSPlayerController.h"
#include "OWSPlugin.h"
#include "Net/UnrealNetwork.h"
#include "Runtime/Online/HTTP/Public/Http.h"
#include "Net/UnrealNetwork.h"
#include "OWSCharacterWithAbilities.h"
#include "OWSCharacter.h"
#include "OWSGameMode.h"
#include "OWSGameInstance.h"
#include "GameFramework/PlayerInput.h"
#include "Runtime/Engine/Classes/GameFramework/PlayerState.h"
#include "Runtime/Core/Public/Misc/ConfigCacheIni.h"
#include "Runtime/Engine/Classes/Components/StaticMeshComponent.h"
//#include "Runtime/HeadMountedDisplay/Public/IXRTrackingSystem.h"


AOWSPlayerController::AOWSPlayerController()
{
	GConfig->GetString(
		TEXT("/Script/EngineSettings.GeneralProjectSettings"),
		TEXT("RPGAPICustomerKey"),
		RPGAPICustomerKey,
		GGameIni
	);

	GConfig->GetString(
		TEXT("/Script/EngineSettings.GeneralProjectSettings"),
		TEXT("OWS2APIPath"),
		OWS2APIPath,
		GGameIni
	);

	GConfig->GetString(
		TEXT("/Script/EngineSettings.GeneralProjectSettings"),
		TEXT("OWSEncryptionKey"),
		OWSEncryptionKey,
		GGameIni
	);

	MaxPredictionPing = 120.f;
	bEnableClickEvents = true;

	//Create UOWSPlayerControllerComponent and bind delegates
	OWSPlayerControllerComponent = CreateDefaultSubobject<UOWSPlayerControllerComponent>(TEXT("OWS Player Controller Component"));

	OWSPlayerControllerComponent->OnNotifyGetAllCharactersDelegate.BindUObject(this, &AOWSPlayerController::NotifyGetAllCharacters);
	OWSPlayerControllerComponent->OnErrorGetAllCharactersDelegate.BindUObject(this, &AOWSPlayerController::ErrorGetAllCharacters);
	OWSPlayerControllerComponent->OnNotifyGetChatGroupsForPlayerDelegate.BindUObject(this, &AOWSPlayerController::NotifyGetChatGroupsForPlayer);
	OWSPlayerControllerComponent->OnErrorGetChatGroupsForPlayerDelegate.BindUObject(this, &AOWSPlayerController::ErrorGetChatGroupsForPlayer);
	OWSPlayerControllerComponent->OnNotifyGetCharacterStatsDelegate.BindUObject(this, &AOWSPlayerController::NotifyGetCharacterStats);
	OWSPlayerControllerComponent->OnNotifyUpdateCharacterStatsDelegate.BindUObject(this, &AOWSPlayerController::NotifyUpdateCharacterStats);
	OWSPlayerControllerComponent->OnNotifyGetCustomCharacterDataDelegate.BindUObject(this, &AOWSPlayerController::NotifyGetCustomCharacterData);
	OWSPlayerControllerComponent->OnErrorGetCustomCharacterDataDelegate.BindUObject(this, &AOWSPlayerController::ErrorCustomCharacterData);
	OWSPlayerControllerComponent->OnNotifyGetCharacterDataAndCustomDataDelegate.BindUObject(this, &AOWSPlayerController::NotifyGetCharacterDataAndCustomData2);
	OWSPlayerControllerComponent->OnErrorGetCharacterDataAndCustomDataDelegate.BindUObject(this, &AOWSPlayerController::ErrorGetCharacterDataAndCustomData);
	OWSPlayerControllerComponent->OnNotifyAddAbilityToCharacterDelegate.BindUObject(this, &AOWSPlayerController::NotifyAddAbilityToCharacter);
	OWSPlayerControllerComponent->OnErrorAddAbilityToCharacterDelegate.BindUObject(this, &AOWSPlayerController::ErrorAddAbilityToCharacter);
	OWSPlayerControllerComponent->OnNotifyGetCharacterAbilitiesDelegate.BindUObject(this, &AOWSPlayerController::NotifyGetCharacterAbilities);
	OWSPlayerControllerComponent->OnErrorGetCharacterAbilitiesDelegate.BindUObject(this, &AOWSPlayerController::ErrorGetCharacterAbilities);
	OWSPlayerControllerComponent->OnNotifyGetAbilityBarsDelegate.BindUObject(this, &AOWSPlayerController::NotifyGetAbilityBars);
	OWSPlayerControllerComponent->OnErrorGetAbilityBarsDelegate.BindUObject(this, &AOWSPlayerController::ErrorGetAbilityBars);
	OWSPlayerControllerComponent->OnNotifyUpdateAbilityOnCharacterDelegate.BindUObject(this, &AOWSPlayerController::NotifyUpdateAbilityOnCharacter);
	OWSPlayerControllerComponent->OnErrorUpdateAbilityOnCharacterDelegate.BindUObject(this, &AOWSPlayerController::ErrorUpdateAbilityOnCharacter);
	OWSPlayerControllerComponent->OnNotifyRemoveAbilityFromCharacterDelegate.BindUObject(this, &AOWSPlayerController::NotifyRemoveAbilityFromCharacter);
	OWSPlayerControllerComponent->OnErrorRemoveAbilityFromCharacterDelegate.BindUObject(this, &AOWSPlayerController::ErrorRemoveAbilityFromCharacter);
	OWSPlayerControllerComponent->OnNotifyPlayerLogoutDelegate.BindUObject(this, &AOWSPlayerController::NotifyPlayerLogout);
	OWSPlayerControllerComponent->OnErrorPlayerLogoutDelegate.BindUObject(this, &AOWSPlayerController::ErrorPlayerLogout);
	OWSPlayerControllerComponent->OnNotifyCreateCharacterDelegate.BindUObject(this, &AOWSPlayerController::NotifyCreateCharacter);
	OWSPlayerControllerComponent->OnErrorCreateCharacterDelegate.BindUObject(this, &AOWSPlayerController::ErrorCreateCharacter);
	OWSPlayerControllerComponent->OnNotifyRemoveCharacterDelegate.BindUObject(this, &AOWSPlayerController::NotifyRemoveCharacter);
	OWSPlayerControllerComponent->OnErrorRemoveCharacterDelegate.BindUObject(this, &AOWSPlayerController::ErrorRemoveCharacter);
	OWSPlayerControllerComponent->OnNotifyGetZoneServerToTravelToDelegate.BindUObject(this, &AOWSPlayerController::NotifyMapServerToTravelTo);
	OWSPlayerControllerComponent->OnErrorGetZoneServerToTravelToDelegate.BindUObject(this, &AOWSPlayerController::ErrorMapServerToTravelTo);
	OWSPlayerControllerComponent->OnNotifyGetPlayerGroupsCharacterIsInDelegate.BindUObject(this, &AOWSPlayerController::NotifyGetPlayerGroupsCharacterIsIn);
	OWSPlayerControllerComponent->OnErrorGetPlayerGroupsCharacterIsInDelegate.BindUObject(this, &AOWSPlayerController::ErrorGetPlayerGroupsCharacterIsIn);
	OWSPlayerControllerComponent->OnNotifyLaunchZoneInstanceDelegate.BindUObject(this, &AOWSPlayerController::NotifyLaunchDungeon);
	OWSPlayerControllerComponent->OnErrorLaunchZoneInstanceDelegate.BindUObject(this, &AOWSPlayerController::ErrorLaunchDungeon);
	OWSPlayerControllerComponent->OnNotifyCreateCharacterUsingDefaultCharacterValuesDelegate.BindUObject(this, &AOWSPlayerController::NotifyCreateCharacterUsingDefaultCharacterValues);
	OWSPlayerControllerComponent->OnErrorCreateCharacterUsingDefaultCharacterValuesDelegate.BindUObject(this, &AOWSPlayerController::ErrorCreateCharacterUsingDefaultCharacterValues);
	OWSPlayerControllerComponent->OnNotifyLogoutDelegate.BindUObject(this, &AOWSPlayerController::NotifyLogout);
	OWSPlayerControllerComponent->OnErrorLogoutDelegate.BindUObject(this, &AOWSPlayerController::ErrorLogout);

}

void AOWSPlayerController::NotifyGetCharacterStats(TSharedPtr<FJsonObject> JsonObject)
{
	UE_LOG(OWS, VeryVerbose, TEXT("AOWSPlayerController - NotifyGetCharacterStats Started"));
	AOWSCharacterWithAbilities* OWSCharacterWithAbilities = Cast<AOWSCharacterWithAbilities>(GetPawn());

	if (OWSCharacterWithAbilities)
	{
		OWSCharacterWithAbilities->LoadCharacterStatsFromJSON(JsonObject);
		OWSCharacterWithAbilities->LoadAttributesFromJSON(JsonObject);

		if (OWSCharacterWithAbilities->bShouldAutoLoadCustomCharacterStats)
		{
			OWSCharacterWithAbilities->LoadCustomCharacterStats();
		}
		OWSCharacterWithAbilities->UpdateCharacterStatsAfterLoading();
		OWSCharacterWithAbilities->OnOWSAttributeInitalizationComplete();
	}
	else
	{
		AOWSCharacter* OWSCharacter = Cast<AOWSCharacter>(GetPawn());
		OWSCharacter->LoadCharacterStatsFromJSON(JsonObject);
	}
}

void AOWSPlayerController::NotifyUpdateCharacterStats()
{
	UE_LOG(OWS, VeryVerbose, TEXT("AOWSPlayerController - NotifyUpdateCharacterStats Started"));

}

void AOWSPlayerController::NotifyGetCustomCharacterData(TSharedPtr<FJsonObject> JsonObject)
{
	UE_LOG(OWS, VeryVerbose, TEXT("AOWSPlayerController - NotifyGetCustomCharacterData Started"));
	AOWSCharacter* OWSCharacter = Cast<AOWSCharacter>(GetPawn());

	OWSCharacter->ProcessCustomCharacterData(JsonObject);
}

void AOWSPlayerController::ErrorCustomCharacterData(const FString& ErrorMsg)
{
	UE_LOG(OWS, VeryVerbose, TEXT("AOWSPlayerController - ErrorCustomCharacterData Started"));
	AOWSCharacter* OWSCharacter = Cast<AOWSCharacter>(GetPawn());

	OWSCharacter->ErrorGetCustomCharacterData(ErrorMsg);
}

void AOWSPlayerController::NotifyGetCharacterAbilities(const TArray<FAbility>& Abilities)
{
	UE_LOG(OWS, VeryVerbose, TEXT("AOWSPlayerController - NotifyGetCharacterAbilitiesData Started"));
	AOWSCharacter* OWSCharacter = Cast<AOWSCharacter>(GetPawn());

	OWSCharacter->GetCharacterAbilitiesComplete(Abilities);
}

void AOWSPlayerController::ErrorGetCharacterAbilities(const FString& ErrorMsg)
{
	UE_LOG(OWS, VeryVerbose, TEXT("AOWSPlayerController - ErrorGetCharacterAbilitiesData Started"));
	AOWSCharacter* OWSCharacter = Cast<AOWSCharacter>(GetPawn());

	OWSCharacter->GetCharacterAbilitiesError(ErrorMsg);
}

void AOWSPlayerController::NotifyGetAbilityBars(const TArray<FAbilityBar>& AbilityBars)
{
	UE_LOG(OWS, VeryVerbose, TEXT("AOWSPlayerController - NotifyGetAbilityBars Started"));
	AOWSCharacter* OWSCharacter = Cast<AOWSCharacter>(GetPawn());

	OWSCharacter->GetAbilityBarsComplete(AbilityBars);
}

void AOWSPlayerController::ErrorGetAbilityBars(const FString& ErrorMsg)
{
	UE_LOG(OWS, VeryVerbose, TEXT("AOWSPlayerController - ErrorGetAbilityBars Started"));
	AOWSCharacter* OWSCharacter = Cast<AOWSCharacter>(GetPawn());

	OWSCharacter->GetAbilityBarsError(ErrorMsg);
}

void AOWSPlayerController::NotifyGetCharacterDataAndCustomData2(TSharedPtr<FJsonObject> JsonObject)
{
	TArray<FCustomCharacterDataStruct> CustomData;

	if (JsonObject->HasField(TEXT("CustomCharacterDataRows")))
	{
		TArray<TSharedPtr<FJsonValue>> Rows = JsonObject->GetArrayField(TEXT("CustomCharacterDataRows"));

		for (int RowNum = 0; RowNum != Rows.Num(); RowNum++) {
			FCustomCharacterDataStruct tempCustomData;
			TSharedPtr<FJsonObject> tempRow = Rows[RowNum]->AsObject();
			tempCustomData.CustomFieldName = tempRow->GetStringField(TEXT("CustomFieldName"));
			tempCustomData.FieldValue = tempRow->GetStringField(TEXT("FieldValue"));

			CustomData.Add(tempCustomData);
		}
	}

	NotifyGetCharacterDataAndCustomData(CustomData);

}


void AOWSPlayerController::TravelToMap(const FString& URL, const bool SeamlessTravel)
{
	UE_LOG(OWS, Warning, TEXT("TravelToMap: %s"), *URL);
	ClientTravel(URL, TRAVEL_Absolute, false, FGuid());
}

void AOWSPlayerController::TravelToMap2(const FString& ServerAndPort, const float X, const float Y, const float Z, const float RX, const float RY, 
	const float RZ, const FString& PlayerName, const bool SeamlessTravel)
{
	if (!GetWorld())
	{
		return;
	}

	UOWSGameInstance* GameInstance = Cast<UOWSGameInstance>(GetWorld()->GetGameInstance());

	if (!GameInstance)
	{
		return;
	}

	if (!GetOWSPlayerState())
	{
		UE_LOG(OWS, Error, TEXT("Invalid OWS Player State!  You can no longer use TravelToMap2 to connect to a server from the Select Character screen!"));

		return;
	}

	FString IDData = FString::SanitizeFloat(X)
		+ "|" + FString::SanitizeFloat(Y)
		+ "|" + FString::SanitizeFloat(Z)
		+ "|" + FString::SanitizeFloat(RX)
		+ "|" + FString::SanitizeFloat(RY)
		+ "|" + FString::SanitizeFloat(RZ)
		+ "|" + FGenericPlatformHttp::UrlEncode(GetOWSPlayerState()->GetPlayerName())
		+ "|" + GetOWSPlayerState()->UserSessionGUID;

	FString EncryptedIDData = GameInstance->EncryptWithAES(IDData, OWSEncryptionKey);

	FString URL = ServerAndPort
		+ FString(TEXT("?ID=")) + EncryptedIDData;

	UE_LOG(OWS, Warning, TEXT("TravelToMap: %s"), *URL);
	ClientTravel(URL, TRAVEL_Absolute, false, FGuid());
}

void AOWSPlayerController::SetSelectedCharacterAndConnectToLastZone(FString UserSessionGUID, FString SelectedCharacterName)
{
	//Set character name and get user session
	OWSPlayerControllerComponent->SetSelectedCharacterAndConnectToLastZone(UserSessionGUID, SelectedCharacterName);
}

void AOWSPlayerController::TravelToLastZoneServer(FString CharacterName)
{
	OWSPlayerControllerComponent->TravelToLastZoneServer(CharacterName);
}


float AOWSPlayerController::GetPredictionTime()
{
	// exact ping is in msec, divide by 1000 to get time in seconds
	//if (Role == ROLE_Authority) { UE_LOG(UT, Warning, TEXT("Server ExactPing %f"), PlayerState->ExactPing); }
	//UE_LOG(LogTemp, Error, TEXT("ExactPing: %f"), PlayerState->ExactPing);

	return (PlayerState && (GetNetMode() != NM_Standalone)) ? (0.0005f*FMath::Clamp(PlayerState->ExactPing - PredictionFudgeFactor, 0.f, MaxPredictionPing)) : 0.f;
}

float AOWSPlayerController::GetProjectileSleepTime()
{
	return 0.001f * FMath::Max(0.f, PlayerState->ExactPing - PredictionFudgeFactor - MaxPredictionPing);
}

UTexture2D * AOWSPlayerController::LoadTextureReference(const FString& TexturePath)
{
	FSoftObjectPath TextureReference(TexturePath);
	return Cast<UTexture2D>(TextureReference.TryLoad());
}

void AOWSPlayerController::SetSelectedCharacter(AOWSCharacter* RPGCharacter)
{
	if (SelectedCharacter)
	{
		ClearSelectionOnCharacter(SelectedCharacter);
	}

	if (RPGCharacter)
	{
		TArray<UActorComponent*> SMcomps;

		RPGCharacter->GetComponents(SMcomps);
		for (int i = 0; i < SMcomps.Num(); ++i) //Because there may be more components
		{
			UStaticMeshComponent* thisSMComp = Cast<UStaticMeshComponent>(SMcomps[i]); //try to cast to static mesh component
			if (thisSMComp)
			{
				thisSMComp->SetRenderCustomDepth(true);
				thisSMComp->SetCustomDepthStencilValue(252);
			}

			USkeletalMeshComponent* thisSKMComp = Cast<USkeletalMeshComponent>(SMcomps[i]); //try to cast to static mesh component
			if (thisSKMComp)
			{
				thisSKMComp->SetRenderCustomDepth(true);
				thisSKMComp->SetCustomDepthStencilValue(252);
			}
		}

		SelectedCharacter = RPGCharacter;
	}
}

void AOWSPlayerController::ClearSelectedCharacter()
{
	if (SelectedCharacter)
	{
		AOWSCharacter* RPGCharacter = SelectedCharacter;
		TArray<UActorComponent*> SMcomps;

		RPGCharacter->GetComponents(SMcomps);
		for (int i = 0; i < SMcomps.Num(); ++i) //Because there may be more components
		{
			UStaticMeshComponent* thisSMComp = Cast<UStaticMeshComponent>(SMcomps[i]); //try to cast to static mesh component
			if (thisSMComp)
			{
				//if (thisSMComp->CustomDepthStencilValue != 254)
				//{
				thisSMComp->SetRenderCustomDepth(false);
				thisSMComp->SetCustomDepthStencilValue(0);
				//}
			}

			USkeletalMeshComponent* thisSKMComp = Cast<USkeletalMeshComponent>(SMcomps[i]); //try to cast to static mesh component
			if (thisSKMComp)
			{
				//if (thisSKMComp->CustomDepthStencilValue != 254)
				//{
				thisSKMComp->SetRenderCustomDepth(false);
				thisSKMComp->SetCustomDepthStencilValue(0);
				//}
			}
		}

		SelectedCharacter = NULL;
	}
}

void AOWSPlayerController::ClearSelectionOnCharacter(AOWSCharacter* RPGCharacter)
{
	if (RPGCharacter)
	{
		TArray<UActorComponent*> SMcomps;

		RPGCharacter->GetComponents(SMcomps);
		for (int i = 0; i < SMcomps.Num(); ++i) //Because there may be more components
		{
			UStaticMeshComponent* thisSMComp = Cast<UStaticMeshComponent>(SMcomps[i]); //try to cast to static mesh component
			if (thisSMComp)
			{
				//if (thisSMComp->CustomDepthStencilValue != 254)
				//{
					thisSMComp->SetRenderCustomDepth(false);
					thisSMComp->SetCustomDepthStencilValue(0);
				//}
			}

			USkeletalMeshComponent* thisSKMComp = Cast<USkeletalMeshComponent>(SMcomps[i]); //try to cast to static mesh component
			if (thisSKMComp)
			{
				//if (thisSKMComp->CustomDepthStencilValue != 254)
				//{
					thisSKMComp->SetRenderCustomDepth(false);
					thisSKMComp->SetCustomDepthStencilValue(0);
				//}
			}
		}

		SelectedCharacter = NULL;
	}
}


void AOWSPlayerController::PawnLeavingGame()
{
	AOWSCharacter* MyCharacter = Cast<AOWSCharacter>(GetPawn());

	if (MyCharacter)
	{
		UE_LOG(OWS, Verbose, TEXT("NotifyPawnLeavingGame"));
		NotifyPawnLeavingGame(MyCharacter);
	}

	Super::PawnLeavingGame();
}

AOWSPlayerState* AOWSPlayerController::GetOWSPlayerState() const
{
	return GetPlayerState<AOWSPlayerState>();
}

bool AOWSPlayerController::InputAxis(FKey Key, float Delta, float DeltaTime, int32 NumSamples, bool bGamepad)
{
	bool bResult = false;

	if (PlayerInput)
	{
		bResult = PlayerInput->InputKey(FInputKeyParams(Key, Delta, DeltaTime, NumSamples, bGamepad));
	}

	return bResult;
}

void AOWSPlayerController::SavePlayerLocation()
{
	OWSPlayerControllerComponent->SavePlayerLocation();
}

void AOWSPlayerController::OnSavePlayerLocationResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		UE_LOG(OWS, Verbose, TEXT("OnSavePlayerLocationResponseReceived Success!"));
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnSavePlayerLocationResponseReceived Error accessing server!"));
	}
}


void AOWSPlayerController::SaveAllPlayerData()
{
	OWSPlayerControllerComponent->SavePlayerLocation();
}

void AOWSPlayerController::OnSaveAllPlayerDataResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		UE_LOG(OWS, Verbose, TEXT("OnSaveAllPlayerDataResponseReceived Success!"));
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnSaveAllPlayerDataResponseReceived Error accessing server!"));
	}
}

//Add Player to Group
void AOWSPlayerController::AddPlayerToGroup(FString PlayerGroupName, ERPGPlayerGroupType::PlayerGroupType GroupType, FString CharacterNameToAdd)
{
	/*
	Http = &FHttpModule::Get();

	//FString PlayerName = "Test";
	FString PlayerName = PlayerState->GetPlayerName();
	PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

	AOWSCharacter* MyRPGCharacter = Cast<AOWSCharacter>(GetPawn());

	TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
	Request->OnProcessRequestComplete().BindUObject(this, &AOWSPlayerController::OnAddPlayerToGroupResponseReceived);
	//This is the url on which to process the request
	FString url = FString(TEXT("http://" +  + "/Groups/AddPlayerToGroupJSON"));

	FString PostParameters = FString(TEXT("id=")) + PlayerName
		+ FString(TEXT("&GroupName=")) + PlayerGroupName
		+ FString(TEXT("&GroupType=")) + FString::FromInt(GroupType)
		+ FString(TEXT("&CharacterNameToAdd=")) + CharacterNameToAdd
		+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

	Request->SetURL(url);
	Request->SetVerb("POST");
	Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
	Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
	Request->SetContentAsString(PostParameters);
	//UE_LOG(LogTemp, Warning, TEXT("Sent %s"), *PostParameters);
	Request->ProcessRequest();
	*/
}

void AOWSPlayerController::OnAddPlayerToGroupResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	/*
	if (bWasSuccessful)
	{
		UE_LOG(OWS, Verbose, TEXT("OnAddPlayerGroupResponseReceived Success: %s"), *Response->GetContentAsString());
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			FString CharacterNameAddedToGroup = JsonObject->GetStringField("characternameaddedtogroup");
			int32 PlayerGroupID = JsonObject->GetNumberField("playergroupid");
			FString PlayerGroupName = JsonObject->GetStringField("playergroupname");
			int32 PlayerGroupTypeID = JsonObject->GetNumberField("playergrouptype");

			FPlayerGroup tempPlayerGroup;

			tempPlayerGroup.PlayerGroupID = PlayerGroupID;
			tempPlayerGroup.PlayerGroupName = PlayerGroupName;
			tempPlayerGroup.PlayerGroupTypeID = PlayerGroupTypeID;

			PlayerGroupsPlayerIsIn.Add(tempPlayerGroup);

			//Add self
			GetOWSPlayerState()->AlwaysRelevantPartyID = PlayerGroupID;
			GetReplicationGraph()->AddPlayerToParty(GetOWSPlayerState());

			//Add other player
			GetOWSPlayerState()->AlwaysRelevantPartyID = PlayerGroupID;
			AOWSGameMode* MyGameMode = Cast<AOWSGameMode>(GetWorld()->GetAuthGameMode());
			if (MyGameMode)
			{
				AOWSPlayerState* OtherPlayerState = MyGameMode->GetPlayerControllerFromCharacterName(CharacterNameAddedToGroup)->GetOWSPlayerState();
				if (OtherPlayerState)
				{
					OtherPlayerState->AlwaysRelevantPartyID = PlayerGroupID;
					GetReplicationGraph()->AddPlayerToParty(OtherPlayerState);
				}
			}

			NotifyAddPlayerToGroup(CharacterNameAddedToGroup);
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnSavePlayerLocationResponseReceived Server returned no data!"));
		}	
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnAddPlayerGroupResponseReceived Error accessing server!"));
	}
	*/
}


//Remove Player from Group
void AOWSPlayerController::RemovePlayerFromGroup(FString PlayerGroupName, FString CharacterNameToRemove)
{
	/*
	Http = &FHttpModule::Get();

	//FString PlayerName = "Test";
	FString PlayerName = PlayerState->GetPlayerName();
	PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

	AOWSCharacter* MyRPGCharacter = Cast<AOWSCharacter>(GetPawn());

	TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
	Request->OnProcessRequestComplete().BindUObject(this, &AOWSPlayerController::OnRemovePlayerFromGroupResponseReceived);
	//This is the url on which to process the request
	FString url = FString(TEXT("http://" +  + "/Groups/RemovePlayerFromGroupJSON"));

	FString PostParameters = FString(TEXT("id=")) + PlayerName
		+ FString(TEXT("&GroupName=")) + PlayerGroupName
		+ FString(TEXT("&CharacterNameToRemove=")) + CharacterNameToRemove
		+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

	Request->SetURL(url);
	Request->SetVerb("POST");
	Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
	Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
	Request->SetContentAsString(PostParameters);
	//UE_LOG(LogTemp, Warning, TEXT("Sent %s"), *PostParameters);
	Request->ProcessRequest();
	*/
}

void AOWSPlayerController::OnRemovePlayerFromGroupResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	/*
	if (bWasSuccessful)
	{
		UE_LOG(OWS, Verbose, TEXT("OnRemovePlayerFromGroupResponseReceived Success: %s"), *Response->GetContentAsString());
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			FString CharacterNameAddedToGroup = JsonObject->GetStringField("characternameaddedtogroup");
			int32 PlayerGroupID = JsonObject->GetNumberField("playergroupid");

			PlayerGroupsPlayerIsIn.RemoveAll([PlayerGroupID](const FPlayerGroup& InPlayerGroup)
			{
				return InPlayerGroup.PlayerGroupID == PlayerGroupID;
			});

			NotifyRemovePlayerFromGroup(CharacterNameAddedToGroup);
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnRemovePlayerFromGroupResponseReceived Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnRemovePlayerFromGroupResponseReceived Error accessing server!"));
	}
	*/
}



void AOWSPlayerController::LaunchDungeon(FString MapName, ERPGPlayerGroupType::PlayerGroupType GroupType)
{
	FString CharacterName = PlayerState->GetPlayerName();
	OWSPlayerControllerComponent->LaunchZoneInstance(CharacterName, MapName, GroupType);
}


void AOWSPlayerController::PlayerLogout()
{
	FString CharacterName = PlayerState->GetPlayerName();
	OWSPlayerControllerComponent->PlayerLogout(CharacterName);
}


void AOWSPlayerController::GetChatGroupsForPlayer()
{
	OWSPlayerControllerComponent->GetChatGroupsForPlayer();
}

void AOWSPlayerController::IsPlayerOnline(FString PlayerName)
{
	/*
	Http = &FHttpModule::Get();

	PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

	TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
	Request->OnProcessRequestComplete().BindUObject(this, &AOWSPlayerController::OnIsPlayerOnlineResponseReceived);
	//This is the url on which to process the request
	FString url = FString(TEXT("http://" +  + "/RPGServer/IsPlayerOnline"));

	FString PostParameters = FString(TEXT("id=")) + PlayerName
		+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

	Request->SetURL(url);
	Request->SetVerb("POST");
	Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
	Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
	Request->SetContentAsString(PostParameters);
	Request->ProcessRequest();
	*/
}

void AOWSPlayerController::OnIsPlayerOnlineResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	/*
	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			if (JsonObject->GetStringField("success") == "true")
			{
				bool IsOnline = JsonObject->GetBoolField("IsOnline");

				NotifyIsPlayerOnline(IsOnline);
			}
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnIsPlayerOnlineResponseReceived Error accessing login server!"));
		ErrorIsPlayerOnline(TEXT("OnIsPlayerOnlineResponseReceived Error accessing login server!"));
	}
	*/
}



void AOWSPlayerController::GetAllCharacters(FString UserSessionGUID)
{
	OWSPlayerControllerComponent->GetAllCharacters(UserSessionGUID);
}



void AOWSPlayerController::CreateCharacter(FString UserSessionGUID, FString CharacterName, FString ClassName)
{
	OWSPlayerControllerComponent->CreateCharacter(UserSessionGUID, CharacterName, ClassName);
}

void AOWSPlayerController::CreateCharacterUsingDefaultCharacterValues(FString UserSessionGUID, FString CharacterName, FString DefaultSetName)
{
	OWSPlayerControllerComponent->CreateCharacterUsingDefaultCharacterValues(UserSessionGUID, CharacterName, DefaultSetName);
}

void AOWSPlayerController::Logout(FString UserSessionGUID)
{
	OWSPlayerControllerComponent->Logout(UserSessionGUID);
}


//Get Cosmetic Custom Character Data
void AOWSPlayerController::GetCosmeticCustomCharacterData(FString UserSessionGUID, FString CharacterName)
{
	/*
	Http = &FHttpModule::Get();

	//AOWSPlayerController* PC = Cast<AOWSPlayerController>(this->Controller);
	FString PlayerName = CharacterName;
	PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

	TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
	Request->OnProcessRequestComplete().BindUObject(this, &AOWSPlayerController::OnGetCosmeticCustomCharacterDataResponseReceived);
	//This is the url on which to process the request
	FString url = FString(TEXT("http://" +  + "/RPGServer/GetCustomCharacterData"));

	FString PostParameters = FString(TEXT("id="))  + PlayerName
		+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

	Request->SetURL(url);
	Request->SetVerb("POST");
	Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
	Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
	Request->SetContentAsString(PostParameters);
	Request->ProcessRequest();
	*/
}

void AOWSPlayerController::OnGetCosmeticCustomCharacterDataResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	/*
	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			TArray<FCustomCharacterDataStruct> CustomCharacterData;

			if (JsonObject->HasField("rows"))
			{
				TArray<TSharedPtr<FJsonValue>> Rows = JsonObject->GetArrayField("rows");

				for (int RowNum = 0; RowNum != Rows.Num(); RowNum++) {
					FCustomCharacterDataStruct tempCustomData;
					TSharedPtr<FJsonObject> tempRow = Rows[RowNum]->AsObject();
					tempCustomData.CustomFieldName = tempRow->GetStringField("CustomFieldName").Replace(TEXT("COSMETIC_"), TEXT(""));
					tempCustomData.FieldValue = tempRow->GetStringField("FieldValue");

					CustomCharacterData.Add(tempCustomData);
				}
			}

			NotifyGetCosmeticCustomCharacterData(CustomCharacterData);
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnGetCharacterStatusesResponseReceived Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnGetCharacterStatusesResponseReceived Error accessing server!"));
	}
	*/
}


void AOWSPlayerController::AddOrUpdateCosmeticCustomCharacterData(FString UserSessionGUID, FString CharacterName, FString CustomFieldName, FString CustomValue)
{
	/*
	TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
	Request->OnProcessRequestComplete().BindUObject(this, &AOWSPlayerController::OnAddOrUpdateCosmeticCustomCharacterDataResponseReceived);

	CharacterName = CharacterName.Replace(TEXT(" "), TEXT("%20"));
	//This is the url on which to process the request
	FString url = FString(TEXT("http://" +  + "/RPGServer/AddOrUpdateCosmeticCharacterData"));

	FString PostParameters = FString(TEXT("id=")) + CharacterName
		+ FString(TEXT("&UserSessionGUID=")) + UserSessionGUID
		+ FString(TEXT("&FieldName=")) + CustomFieldName
		+ FString(TEXT("&FieldValue=")) + CustomValue
		+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

	UE_LOG(LogTemp, Log, TEXT("OnAddOrUpdateCosmeticCustomCharacterDataResponseReceived - PlayerName: %s, FieldName: %s, Value: %s"), *CharacterName, *CustomFieldName, *CustomValue);

	Request->SetURL(url);
	Request->SetVerb("POST");
	Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
	Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
	Request->SetContentAsString(PostParameters);
	Request->ProcessRequest();
	*/
}

void AOWSPlayerController::OnAddOrUpdateCosmeticCustomCharacterDataResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	/*
	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			UE_LOG(OWS, Verbose, TEXT("OnAddOrUpdateCosmeticCustomCharacterDataResponseReceived Success!"));
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnAddOrUpdateCosmeticCustomCharacterDataResponseReceived Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnAddOrUpdateCosmeticCustomCharacterDataResponseReceived Error accessing server!"));
	}
	*/
}


void AOWSPlayerController::GetPlayerGroupsCharacterIsIn(FString UserSessionGUID, FString CharacterName, int32 PlayerGroupTypeID)
{
	/*
	//AOWSPlayerController* PC = Cast<AOWSPlayerController>(this->Controller);
	FString PlayerName = CharacterName;
	//PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20")); //Removed for OWS2

	TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
	Request->OnProcessRequestComplete().BindUObject(this, &AOWSPlayerController::OnGetPlayerGroupsCharacterIsInResponseReceived);
	//This is the url on which to process the request
	FString url = FString(OWS2APIPath + "api/Users/GetPlayerGroupsCharacterIsIn");

	//Trim whitespace
	PlayerName.TrimStartAndEndInline();

	TArray<FStringFormatArg> FormatParams;
	FormatParams.Add(UserSessionGUID);
	FormatParams.Add(CharacterName);
	FormatParams.Add(PlayerGroupTypeID);
	FString PostParameters = FString::Format(TEXT("{ \"UserSessionGUID\": \"{0}\", \"CharacterName\": \"{1}\",  \"PlayerGroupTypeID\": \"{2}\" }"), FormatParams);

	Request->SetURL(url);
	Request->SetVerb("POST");
	Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
	Request->SetHeader("Content-Type", TEXT("application/json"));
	Request->SetHeader(TEXT("X-CustomerGUID"), RPGAPICustomerKey);
	Request->SetContentAsString(PostParameters);
	Request->ProcessRequest();
	*/

	OWSPlayerControllerComponent->GetPlayerGroupsCharacterIsIn(UserSessionGUID, CharacterName, PlayerGroupTypeID);
}

void AOWSPlayerController::GetMapServerToTravelTo(FString ZoneName)
{
	FString CharacterName = PlayerState->GetPlayerName();
	OWSPlayerControllerComponent->GetZoneServerToTravelTo(CharacterName, ERPGSchemeToChooseMap::SchemeToChooseMap::Default, 0, ZoneName);
}


//Remove Character
void AOWSPlayerController::RemoveCharacter(FString UserSessionGUID, FString CharacterName)
{
	OWSPlayerControllerComponent->RemoveCharacter(UserSessionGUID, CharacterName);
}


AOWSGameMode* AOWSPlayerController::GetGameMode()
{
	return (AOWSGameMode*)GetWorld()->GetAuthGameMode();
}

void AOWSPlayerController::SynchUpLocalMeshItemsMap()
{
	UOWSGameInstance* GameInstance = Cast<UOWSGameInstance>(GetWorld()->GetGameInstance());

	if (!GameInstance)
		return;

	AOWSGameMode* OWSGameMode = GetGameMode();

	if (!OWSGameMode)
		return;

	for (auto& MapItem : OWSGameMode->MeshItemsMap)
	{
		AddItemToLocalMeshItemsMap(MapItem.Key, MapItem.Value);
	}
}

void AOWSPlayerController::AddItemToLocalMeshItemsMap(const FString& ItemName, const int32 ItemMeshID)
{
	UOWSGameInstance* GameInstance = Cast<UOWSGameInstance>(GetWorld()->GetGameInstance());

	if (!GameInstance)
		return;

	if (!LocalMeshItemsMap.Contains(ItemName))
	{
		LocalMeshItemsMap.Add(ItemName, ItemMeshID);

		Client_AddItemToLocalMeshItemsMap(ItemName, ItemMeshID);
	}
}

void AOWSPlayerController::Client_AddItemToLocalMeshItemsMap_Implementation(const FString& ItemName, const int32 ItemMeshID)
{
	UOWSGameInstance* GameInstance = Cast<UOWSGameInstance>(GetWorld()->GetGameInstance());

	if (!GameInstance)
		return;

	GameInstance->LocalMeshItemsMap.Add(ItemName, ItemMeshID);
}

void AOWSPlayerController::GetLifetimeReplicatedProps(TArray< FLifetimeProperty > & OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);

	DOREPLIFETIME_CONDITION(AOWSPlayerController, MaxPredictionPing, COND_OwnerOnly);
	DOREPLIFETIME_CONDITION(AOWSPlayerController, PredictionFudgeFactor, COND_OwnerOnly);
}
