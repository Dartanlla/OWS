// Copyright 2020 Sabre Dart Studios

#include "OWSPlayerControllerComponent.h"
#include "OWSGameInstance.h"
#include "OWS2API.h"
#include "Character/Player/PlayerCharacterBase.h"
#include "OWSPlugin.h"
#include "Interfaces/IHttpResponse.h"
#include "Kismet/GameplayStatics.h"
#include "GenericPlatform/GenericPlatformHttp.h"


// Sets default values for this component's properties
UOWSPlayerControllerComponent::UOWSPlayerControllerComponent()
{
	// Set this component to be initialized when the game starts, and to be ticked every frame.  You can turn these features
	// off to improve performance if you don't need them.
	PrimaryComponentTick.bCanEverTick = false;

	// ...
	GConfig->GetString(
		TEXT("/Script/EngineSettings.GeneralProjectSettings"),
		TEXT("OWSAPICustomerKey"),
		OWSAPICustomerKey,
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
		TEXT("OWS2InstanceManagementAPIPath"),
		OWS2InstanceManagementAPIPath,
		GGameIni
	);

	GConfig->GetString(
		TEXT("/Script/EngineSettings.GeneralProjectSettings"),
		TEXT("OWS2CharacterPersistenceAPIPath"),
		OWS2CharacterPersistenceAPIPath,
		GGameIni
	);

	GConfig->GetString(
		TEXT("/Script/EngineSettings.GeneralProjectSettings"),
		TEXT("OWSEncryptionKey"),
		OWSEncryptionKey,
		GGameIni
	);

	void UOWSPlayerControllerComponent::CreateCharacterUsingDefaultCharacterValuesSuccess()
{
	//OnNotifyCreateCharacterUsingDefaultCharacterValuesDelegate.ExecuteIfBound();
}

void UOWSPlayerControllerComponent::CreateCharacterUsingDefaultCharacterValuesError(const FString& ErrorMsg)
{
	//OnErrorCreateCharacterUsingDefaultCharacterValuesDelegate.ExecuteIfBound(ErrorMsg);
}

void UOWSPlayerControllerComponent::LogoutSuccess()
{
	//OnNotifyLogoutDelegate.ExecuteIfBound();
}

void UOWSPlayerControllerComponent::LogoutError(const FString& ErrorMsg)
{
	//OnErrorLogoutDelegate.ExecuteIfBound(ErrorMsg);
}
}

// Called when the game starts
void UOWSPlayerControllerComponent::BeginPlay()
{
	Super::BeginPlay();

	// ...
	
}

void UOWSPlayerControllerComponent::GetJsonObjectFromResponse(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful, FString CallingMethodName, FString& ErrorMsg, TSharedPtr<FJsonObject>& JsonObject)
{
	if (bWasSuccessful && Response.IsValid())
	{
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			ErrorMsg = "";
			return;
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("%s - Error Deserializing JsonObject!"), *CallingMethodName);
			ErrorMsg = CallingMethodName + " - Error De serializing JsonObject!";
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("%s - Response was unsuccessful or invalid!"), *CallingMethodName);
		ErrorMsg = CallingMethodName + " - Response was unsuccessful or invalid!";
	}
}

void UOWSPlayerControllerComponent::GetPlayerNameAndOWSCharacter(APlayerCharacterBase* PlayerChar, FString& PlayerName)
{
	APlayerController* PlayerController = Cast<APlayerController>(GetOwner());

	//Get the player name from the Player State
	PlayerName = PlayerController->PlayerState->GetPlayerName();
	//Trim whitespace from the start and end
	PlayerName = PlayerName.TrimStartAndEnd();

	PlayerChar = Cast<APlayerCharacterBase>(PlayerController->GetPawn());
}

AOWSPlayerState* UOWSPlayerControllerComponent::GetOWSPlayerState() const
{
	APlayerController* PlayerController = Cast<APlayerController>(GetOwner());

	return PlayerController->GetPlayerState<AOWSPlayerState>();
}

void UOWSPlayerControllerComponent::TravelToMap(const FString& URL, const bool SeamlessTravel)
{
	APlayerController* PlayerController = Cast<APlayerController>(GetOwner());

	UE_LOG(OWS, Warning, TEXT("TravelToMap: %s"), *URL);
	PlayerController->ClientTravel(URL, TRAVEL_Absolute, false, FGuid());
}

void UOWSPlayerControllerComponent::TravelToMap2(const FString& ServerAndPort, const float X, const float Y, const float Z, const float RX, const float RY,
	const float RZ, const FString& PlayerName, const bool SeamlessTravel)
{
	APlayerController* PlayerController = Cast<APlayerController>(GetOwner());

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

	//Build our connection string
	FString IDData = FString::SanitizeFloat(X)
		+ "|" + FString::SanitizeFloat(Y)
		+ "|" + FString::SanitizeFloat(Z)
		+ "|" + FString::SanitizeFloat(RX)
		+ "|" + FString::SanitizeFloat(RY)
		+ "|" + FString::SanitizeFloat(RZ)
		+ "|" + FGenericPlatformHttp::UrlEncode(GetOWSPlayerState()->GetPlayerName())
		+ "|" + GetOWSPlayerState()->UserSessionGUID;

	//Encrypt the connection string using the key found in DefaultGame.ini called OWSEncryptionKey
	FString EncryptedIDData = GameInstance->EncryptWithAES(IDData, OWSEncryptionKey);

	//The encrypted connection string is sent to the UE server as the ID parameter
	FString URL = ServerAndPort
		+ FString(TEXT("?ID=")) + EncryptedIDData;

	//This is not an actual warning.  Yellow text is just easier to read.
	UE_LOG(OWS, Warning, TEXT("TravelToMap: %s"), *URL);
	PlayerController->ClientTravel(URL, TRAVEL_Absolute, false, FGuid());
}

/*
Valid values for ApiModuleToCall:
	PublicAPI
	InstanceManagementAPI
	CharacterPersistanceAPI
*/
void UOWSPlayerControllerComponent::ProcessOWS2POSTRequest(FString ApiModuleToCall, FString ApiToCall, FString PostParameters, void (UOWSPlayerControllerComponent::* InMethodPtr)(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful))
{
	Http = &FHttpModule::Get();

	TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
	Request->OnProcessRequestComplete().BindUObject(this, InMethodPtr);

	FString OWS2APIPathToUse = "";

	if (ApiModuleToCall == "PublicAPI")
	{
		OWS2APIPathToUse = OWS2APIPath;
	}
	else if (ApiModuleToCall == "InstanceManagementAPI")
	{
		OWS2APIPathToUse = OWS2InstanceManagementAPIPath;
	}
	else if (ApiModuleToCall == "CharacterPersistenceAPI")
	{
		OWS2APIPathToUse = OWS2CharacterPersistenceAPIPath;
	}
	else //When an ApiModuleToCall is not specified, use the PublicAPI
	{
		OWS2APIPathToUse = OWS2APIPath;
	}

	Request->SetURL(FString(OWS2APIPathToUse + ApiToCall));
	Request->SetVerb("POST");
	Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
	Request->SetHeader("Content-Type", TEXT("application/json"));
	Request->SetHeader(TEXT("X-CustomerGUID"), OWSAPICustomerKey);
	Request->SetContentAsString(PostParameters);
	Request->ProcessRequest();
}

//SetSelectedCharacterAndConnectToLastZone - Set character name and get user session
void UOWSPlayerControllerComponent::SetSelectedCharacterAndConnectToLastZone(FString UserSessionGUID, FString SelectedCharacterName)
{
	//Trim whitespace
	SelectedCharacterName.TrimStartAndEndInline();

	FSetSelectedCharacterAndConnectToLastZoneJSONPost SetSelectedCharacterAndConnectToLastZoneJSONPost;
	SetSelectedCharacterAndConnectToLastZoneJSONPost.UserSessionGUID = UserSessionGUID;
	SetSelectedCharacterAndConnectToLastZoneJSONPost.SelectedCharacterName = SelectedCharacterName;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(SetSelectedCharacterAndConnectToLastZoneJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("PublicAPI", "api/Users/SetSelectedCharacterAndGetUserSession", PostParameters, &UOWSPlayerControllerComponent::OnSetSelectedCharacterAndConnectToLastZoneResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("SetSelectedCharacterAndConnectToLastZone Error serializing SetSelectedCharacterAndConnectToLastZoneJSONPost!"));
	}
}

void UOWSPlayerControllerComponent::OnSetSelectedCharacterAndConnectToLastZoneResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		UE_LOG(OWS, Verbose, TEXT("OnSetSelectedCharacterAndConnectToLastZone Success!"));

		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			ServerTravelUserSessionGUID = JsonObject->GetStringField(TEXT("UserSessionGUID"));
			ServerTravelCharacterName = JsonObject->GetStringField(TEXT("CharName"));
			ServerTravelX = JsonObject->GetNumberField(TEXT("X"));
			ServerTravelY = JsonObject->GetNumberField(TEXT("Y"));
			ServerTravelZ = JsonObject->GetNumberField(TEXT("Z"));
			ServerTravelRX = JsonObject->GetNumberField(TEXT("RX"));
			ServerTravelRY = JsonObject->GetNumberField(TEXT("RY"));
			ServerTravelRZ = JsonObject->GetNumberField(TEXT("RZ"));

			UE_LOG(OWS, Log, TEXT("OnSetSelectedCharacterAndConnectToLastZone location is %f, %f, %f"), ServerTravelX, ServerTravelY, ServerTravelZ);

			if (ServerTravelCharacterName.IsEmpty())
			{
				//Call Error BP Event

				return;
			}

			TravelToLastZoneServer(ServerTravelCharacterName);
		}
		else
		{
			UE_LOG(LogTemp, Error, TEXT("OnSetSelectedCharacterAndConnectToLastZone Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnSetSelectedCharacterAndConnectToLastZone Error accessing server!"));
	}
}

//TravelToLastZoneServer
void UOWSPlayerControllerComponent::TravelToLastZoneServer(FString CharacterName)
{
	FTravelToLastZoneServerJSONPost TravelToLastZoneServerJSONPost;
	TravelToLastZoneServerJSONPost.CharacterName = CharacterName;
	TravelToLastZoneServerJSONPost.ZoneName = "GETLASTZONENAME";
	TravelToLastZoneServerJSONPost.PlayerGroupType = 0;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(TravelToLastZoneServerJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("PublicAPI", "api/Users/GetServerToConnectTo", PostParameters, &UOWSPlayerControllerComponent::OnTravelToLastZoneServerResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("TravelToLastZoneServer Error serializing TravelToLastZoneServerJSONPost!"));
	}
}

void UOWSPlayerControllerComponent::OnTravelToLastZoneServerResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	FString ServerAndPort;

	if (!GetWorld())
	{
		return;
	}

	UOWSGameInstance* GameInstance = Cast<UOWSGameInstance>(GetWorld()->GetGameInstance());

	if (!GameInstance)
	{
		return;
	}

	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			FString ServerIP = JsonObject->GetStringField(TEXT("serverip"));
			FString Port = JsonObject->GetStringField(TEXT("port"));

			if (ServerIP.IsEmpty() || Port.IsEmpty())
			{
				UE_LOG(OWS, Error, TEXT("OnTravelToLastZoneServerResponseReceived Cannot Get Server IP and Port!"));
				return;
			}

			ServerAndPort = ServerIP + FString(TEXT(":")) + Port.Left(4);

			UE_LOG(OWS, Warning, TEXT("OnTravelToLastZoneServerResponseReceived ServerAndPort: %s"), *ServerAndPort);

			//Encrypt data to send
			FString IDData = FString::SanitizeFloat(ServerTravelX)
				+ "|" + FString::SanitizeFloat(ServerTravelY)
				+ "|" + FString::SanitizeFloat(ServerTravelZ)
				+ "|" + FString::SanitizeFloat(ServerTravelRX)
				+ "|" + FString::SanitizeFloat(ServerTravelRY)
				+ "|" + FString::SanitizeFloat(ServerTravelRZ)
				+ "|" + FGenericPlatformHttp::UrlEncode(ServerTravelCharacterName)
				+ "|" + ServerTravelUserSessionGUID;
			FString EncryptedIDData = GameInstance->EncryptWithAES(IDData, OWSEncryptionKey);

			FString URL = ServerAndPort
				+ FString(TEXT("?ID=")) + EncryptedIDData;

			TravelToMap(URL, false);
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnTravelToLastZoneServerResponseReceived Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnTravelToLastZoneServerResponseReceived Error accessing server!"));
	}
}

//GetZoneServerToTravelTo
void UOWSPlayerControllerComponent::GetZoneServerToTravelTo(FString CharacterName, TEnumAsByte<ERPGSchemeToChooseMap::SchemeToChooseMap> SelectedSchemeToChooseMap, int32 WorldServerID, FString ZoneName)
{
	FTravelToLastZoneServerJSONPost TravelToLastZoneServerJSONPost;
	TravelToLastZoneServerJSONPost.CharacterName = CharacterName;
	TravelToLastZoneServerJSONPost.ZoneName = ZoneName;
	TravelToLastZoneServerJSONPost.PlayerGroupType = 0;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(TravelToLastZoneServerJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("PublicAPI", "api/Users/GetServerToConnectTo", PostParameters, &UOWSPlayerControllerComponent::OnGetZoneServerToTravelToResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("GetMapServerToTravelTo Error serializing TravelToLastZoneServerJSONPost!"));
	}
}

void UOWSPlayerControllerComponent::OnGetZoneServerToTravelToResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	FString ServerAndPort;

	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			FString ServerIP = JsonObject->GetStringField(TEXT("serverip"));
			FString Port = JsonObject->GetStringField(TEXT("port"));

			if (ServerIP.IsEmpty() || Port.IsEmpty())
			{
				OnErrorGetZoneServerToTravelToDelegate.ExecuteIfBound(TEXT("Cannot connect to server!"));
				return;
			}

			ServerAndPort = ServerIP + FString(TEXT(":")) + Port.Left(4);

			UE_LOG(LogTemp, Warning, TEXT("ServerAndPort: %s"), *ServerAndPort);

			OnNotifyGetZoneServerToTravelToDelegate.ExecuteIfBound(ServerAndPort);
		}
		else
		{
			UE_LOG(LogTemp, Error, TEXT("OnGetZoneServerToTravelToResponseReceived Server returned no data!"));
			OnErrorGetZoneServerToTravelToDelegate.ExecuteIfBound(TEXT("There was a problem connecting to the server.  Please try again."));
		}
	}
	else
	{
		UE_LOG(LogTemp, Error, TEXT("OnGetZoneServerToTravelToResponseReceived Error accessing server!"));
		OnErrorGetZoneServerToTravelToDelegate.ExecuteIfBound(TEXT("Unknown error connecting to server!"));
	}
}

void UOWSPlayerControllerComponent::SavePlayerLocation()
{
	//Not implemented
}

void UOWSPlayerControllerComponent::OnSavePlayerLocationResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
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

//GetAllCharacters
void UOWSPlayerControllerComponent::GetAllCharacters(FString UserSessionGUID)
{
	FGetAllCharactersJSONPost GetAllCharactersJSONPost;
	GetAllCharactersJSONPost.UserSessionGUID = UserSessionGUID;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(GetAllCharactersJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("PublicAPI", "api/Users/GetAllCharacters", PostParameters, &UOWSPlayerControllerComponent::OnGetAllCharactersResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("GetAllCharacters Error serializing GetAllCharactersJSONPost!"));
	}
}

void UOWSPlayerControllerComponent::OnGetAllCharactersResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		TArray<FUserCharacter> UsersCharactersData;
		if (FJsonObjectConverter::JsonArrayStringToUStruct(Response->GetContentAsString(), &UsersCharactersData, 0, 0))
		{
			OnNotifyGetAllCharactersDelegate.ExecuteIfBound(UsersCharactersData);
		}
		else
		{
			OnErrorGetAllCharactersDelegate.ExecuteIfBound(TEXT("OnGetAllCharactersResponseReceived Error Parsing JSON!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnGetAllCharactersResponseReceived Error accessing login server!"));
		OnErrorGetAllCharactersDelegate.ExecuteIfBound(TEXT("Unknown error connecting to server!"));
	}
}

//GetCharacterStats
void UOWSPlayerControllerComponent::GetCharacterStats(FString CharName)
{
	FGetCharacterStatsJSONPost GetCharacterStatsJSONPost;
	GetCharacterStatsJSONPost.CharacterName = CharName;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(GetCharacterStatsJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("CharacterPersistenceAPI", "api/Characters/GetStatsByName", PostParameters, &UOWSPlayerControllerComponent::OnGetCharacterStatsResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("GetCharacterStats Error serializing GetCharacterStatsJSONPost!"));
	}
}

void UOWSPlayerControllerComponent::OnGetCharacterStatsResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		TArray<FUserCharacterStat> UsersCharacterStatsData;
		if (FJsonObjectConverter::JsonArrayStringToUStruct(Response->GetContentAsString(), &UsersCharacterStatsData, 0, 0))
		{
			OnNotifyGetCharacterStatsDelegate.ExecuteIfBound(UsersCharacterStatsData);
		}
		else
		{
			OnErrorGetCharacterStatsDelegate.ExecuteIfBound(TEXT("OnGetAllCharactersResponseReceived Error Parsing JSON!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnGetAllCharactersResponseReceived Error accessing login server!"));
		OnErrorGetCharacterStatsDelegate.ExecuteIfBound(TEXT("Unknown error connecting to server!"));
	}
}

void UOWSPlayerControllerComponent::GetCharacterQuests(FString CharName)
{
	FGetCharacterStatsJSONPost GetCharacterStatsJSONPost;
	GetCharacterStatsJSONPost.CharacterName = CharName;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(GetCharacterStatsJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("CharacterPersistenceAPI", "api/Characters/GetQuestsByName", PostParameters, &UOWSPlayerControllerComponent::OnGetCharacterQuestsResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("GetCharacterStats Error serializing GetCharacterStatsJSONPost!"));
	}
}
	
void UOWSPlayerControllerComponent::OnGetCharacterQuestsResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		TArray<FUserCharacterQuest> UsersCharacterQuestData;
		if (FJsonObjectConverter::JsonArrayStringToUStruct(Response->GetContentAsString(), &UsersCharacterQuestData, 0, 0))
		{
			OnNotifyGetCharacterQuestsDelegate.ExecuteIfBound(UsersCharacterQuestData);
		}
		else
		{
			OnErrorGetCharacterQuestsDelegate.ExecuteIfBound(TEXT("OnGetAllCharactersResponseReceived Error Parsing JSON!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnGetAllCharactersResponseReceived Error accessing login server!"));
		OnErrorGetCharacterQuestsDelegate.ExecuteIfBound(TEXT("Unknown error connecting to server!"));
	}
}

//GetCharacterInventory
void UOWSPlayerControllerComponent::GetCharacterInventory(FString CharName)
{
	FGetCharacterStatsJSONPost GetCharacterStatsJSONPost;
	GetCharacterStatsJSONPost.CharacterName = CharName;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(GetCharacterStatsJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("CharacterPersistenceAPI", "api/Characters/GetInventoryByName", PostParameters, &UOWSPlayerControllerComponent::OnGetCharacterInventoryResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("GetCharacterStats Error serializing GetCharacterStatsJSONPost!"));
	}
}

void UOWSPlayerControllerComponent::OnGetCharacterInventoryResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		TArray<FUserCharacterInventory> UsersCharacterInventoryData;
		if (FJsonObjectConverter::JsonArrayStringToUStruct(Response->GetContentAsString(), &UsersCharacterInventoryData, 0, 0))
		{
			OnNotifyGetCharacterInventoryDelegate.ExecuteIfBound(UsersCharacterInventoryData);
		}
		else
		{
			OnErrorGetAllCharactersDelegate.ExecuteIfBound(TEXT("OnGetAllCharactersResponseReceived Error Parsing JSON!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnGetAllCharactersResponseReceived Error accessing login server!"));
		OnErrorGetAllCharactersDelegate.ExecuteIfBound(TEXT("Unknown error connecting to server!"));
	}
}

//GetCharacterAbilities
void UOWSPlayerControllerComponent::GetCharacterAbilities(FString CharName)
{
	FCharacterNameJSONPost CharacterNameJSONPost;
	CharacterNameJSONPost.CharacterName = CharName;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(CharacterNameJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("CharacterPersistenceAPI", "api/Abilities/GetCharacterAbilities", PostParameters, &UOWSPlayerControllerComponent::OnGetCharacterAbilitiesResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("GetCharacterAbilities Error serializing GetCharacterStatsJSONPost!"));
	}
}

void UOWSPlayerControllerComponent::OnGetCharacterAbilitiesResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		TArray<TSharedPtr<FJsonValue>> JsonArray;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonArray))
		{
			TArray<FUserCharacterAbility> Abilities;

			FJsonObjectConverter::JsonArrayToUStruct(JsonArray, &Abilities, 0, 0);

			OnNotifyGetCharacterAbilitiesDelegate.ExecuteIfBound(Abilities);
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnGetCharacterAbilitiesResponseReceived Server returned no data!"));
			OnErrorGetCharacterAbilitiesDelegate.ExecuteIfBound(TEXT("OnGetCharacterAbilitiesResponseReceived Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnGetCharacterAbilitiesResponseReceived Error accessing server!"));
		OnErrorGetCharacterAbilitiesDelegate.ExecuteIfBound(TEXT("OnGetCharacterAbilitiesResponseReceived Error accessing server!"));
	}
}

//GetCharacterDataAndCustomData - This makes a call to the OWS Public API and is usable from the Character Selection screen.
void UOWSPlayerControllerComponent::GetCharacterDataAndCustomData(FString UserSessionGUID, FString CharName)
{
	FGetCharacterDataAndCustomData GetCharacterDataAndCustomDataJSONPost;
	GetCharacterDataAndCustomDataJSONPost.UserSessionGUID = UserSessionGUID;
	GetCharacterDataAndCustomDataJSONPost.CharacterName = CharName;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(GetCharacterDataAndCustomDataJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("OWSPublicAPI", "api/Characters/ByName", PostParameters, &UOWSPlayerControllerComponent::OnGetCharacterDataAndCustomDataResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("GetCharacterDataAndCustomData Error serializing GetCharacterStatsJSONPost!"));
	}
}

void UOWSPlayerControllerComponent::OnGetCharacterDataAndCustomDataResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			OnNotifyGetCharacterDataAndCustomDataDelegate.ExecuteIfBound(JsonObject);
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnGetCharacterDataAndCustomDataResponseReceived Error accessing server!"));
		OnErrorGetCharacterDataAndCustomDataDelegate.ExecuteIfBound(TEXT("Unknown error connecting to server!"));
	}
}

//Update Character Stats
void UOWSPlayerControllerComponent::UpdateCharacterStats(FString JSONString)
{
	ProcessOWS2POSTRequest("CharacterPersistenceAPI", "api/Characters/UpdateCharacterStats", JSONString, &UOWSPlayerControllerComponent::OnUpdateCharacterStatsResponseReceived);
}

void UOWSPlayerControllerComponent::OnUpdateCharacterStatsResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	FString ErrorMsg;
	TSharedPtr<FJsonObject> JsonObject;
	GetJsonObjectFromResponse(Request, Response, bWasSuccessful, "OnUpdateCharacterStatsResponseReceived", ErrorMsg, JsonObject);
	if (!ErrorMsg.IsEmpty())
	{
		OnErrorUpdateCharacterStatsDelegate.ExecuteIfBound(ErrorMsg);
		return;
	}

	TSharedPtr<FSuccessAndErrorMessage> SuccessAndErrorMessage = GetStructFromJsonObject<FSuccessAndErrorMessage>(JsonObject);

	if (!SuccessAndErrorMessage->ErrorMessage.IsEmpty())
	{
		OnErrorUpdateCharacterStatsDelegate.ExecuteIfBound(*SuccessAndErrorMessage->ErrorMessage);
		return;
	}

	OnNotifyUpdateCharacterStatsDelegate.ExecuteIfBound();
}

//UpdateAbilityOnCharacter
void UOWSPlayerControllerComponent::UpdateCharacterAbilities(FString JSONString)
{
	ProcessOWS2POSTRequest("CharacterPersistenceAPI", "api/Abilities/UpdateCharacterAbilities", JSONString, &UOWSPlayerControllerComponent::OnUpdateCharacterAbilitiesOnCharacterResponseReceived);
}

void UOWSPlayerControllerComponent::OnUpdateCharacterAbilitiesOnCharacterResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		OnNotifyAddAbilityToCharacterDelegate.ExecuteIfBound();
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnUpdateAbilityOnCharacterResponseReceived Error accessing server!"));
		OnErrorAddAbilityToCharacterDelegate.ExecuteIfBound(TEXT("Unknown error accessing API server!"));
	}
}

void UOWSPlayerControllerComponent::UpdateCharacterQuests(FString JSONString)
{
	ProcessOWS2POSTRequest("CharacterPersistenceAPI", "api/Characters/UpdateCharacterQuests", JSONString, &UOWSPlayerControllerComponent::OnUpdateCharacterAbilitiesOnCharacterResponseReceived);
}

void UOWSPlayerControllerComponent::OnUpdateCharacterQuestsResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	FString ErrorMsg;
	TSharedPtr<FJsonObject> JsonObject;
	GetJsonObjectFromResponse(Request, Response, bWasSuccessful, "OnUpdateCharacterQuestResponseReceived", ErrorMsg, JsonObject);
	if (!ErrorMsg.IsEmpty())
	{
		OnErrorUpdateCharacterQuestDelegate.ExecuteIfBound(ErrorMsg);
		return;
	}

	TSharedPtr<FSuccessAndErrorMessage> SuccessAndErrorMessage = GetStructFromJsonObject<FSuccessAndErrorMessage>(JsonObject);

	if (!SuccessAndErrorMessage->ErrorMessage.IsEmpty())
	{
		OnErrorUpdateCharacterQuestDelegate.ExecuteIfBound(*SuccessAndErrorMessage->ErrorMessage);
		return;
	}

	OnNotifyUpdateCharacterQuestDelegate.ExecuteIfBound();
}

void UOWSPlayerControllerComponent::UpdateCharacterInventory(FString JSONString)
{
	ProcessOWS2POSTRequest("CharacterPersistenceAPI", "api/Characters/UpdateCharacterInventory", JSONString, &UOWSPlayerControllerComponent::OnUpdateCharacterInventoryResponseReceived);
}

void UOWSPlayerControllerComponent::OnUpdateCharacterInventoryResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	FString ErrorMsg;
	TSharedPtr<FJsonObject> JsonObject;
	GetJsonObjectFromResponse(Request, Response, bWasSuccessful, "OnUpdateCharacterInventoryResponseReceived", ErrorMsg, JsonObject);
	if (!ErrorMsg.IsEmpty())
	{
		OnErrorUpdateCharacterStatsDelegate.ExecuteIfBound(ErrorMsg);
		return;
	}

	TSharedPtr<FSuccessAndErrorMessage> SuccessAndErrorMessage = GetStructFromJsonObject<FSuccessAndErrorMessage>(JsonObject);

	if (!SuccessAndErrorMessage->ErrorMessage.IsEmpty())
	{
		OnErrorUpdateCharacterStatsDelegate.ExecuteIfBound(*SuccessAndErrorMessage->ErrorMessage);
		return;
	}

	OnNotifyUpdateCharacterStatsDelegate.ExecuteIfBound();
}

//GetCustomCharacterData
void UOWSPlayerControllerComponent::GetCustomCharacterData(FString CharName)
{
	FGetCustomCharacterDataJSONPost GetCustomCharacterDataJSONPost;
	GetCustomCharacterDataJSONPost.CharacterName = CharName;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(GetCustomCharacterDataJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("CharacterPersistenceAPI", "api/Characters/GetCustomData", PostParameters, &UOWSPlayerControllerComponent::OnGetCustomCharacterDataResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("GetCustomCharacterData Error serializing GetCharacterStatsJSONPost!"));
	}
}

void UOWSPlayerControllerComponent::OnGetCustomCharacterDataResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			OnNotifyGetCustomCharacterDataDelegate.ExecuteIfBound(JsonObject);
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnGetCustomCharacterDataResponseReceived Error accessing server!"));
		OnErrorGetCustomCharacterDataDelegate.ExecuteIfBound(TEXT("Unknown error connecting to server!"));
	}
}

//AddOrUpdateCustomCharacterData
void UOWSPlayerControllerComponent::AddOrUpdateCustomCharacterData(FString CharName, FString CustomFieldName, FString CustomValue)
{
	FAddOrUpdateCustomCharacterDataJSONPost AddOrUpdateCustomCharacterDataJSONPost;
	AddOrUpdateCustomCharacterDataJSONPost.AddOrUpdateCustomCharacterData.CharacterName = CharName;
	AddOrUpdateCustomCharacterDataJSONPost.AddOrUpdateCustomCharacterData.CustomFieldName = CustomFieldName;
	AddOrUpdateCustomCharacterDataJSONPost.AddOrUpdateCustomCharacterData.FieldValue = CustomValue;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(AddOrUpdateCustomCharacterDataJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("CharacterPersistenceAPI", "api/Characters/AddOrUpdateCustomData", PostParameters, &UOWSPlayerControllerComponent::OnAddOrUpdateCustomCharacterDataResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("AddOrUpdateCustomCharacterData Error serializing GetCharacterStatsJSONPost!"));
	}
}

void UOWSPlayerControllerComponent::OnAddOrUpdateCustomCharacterDataResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		OnNotifyAddOrUpdateCustomCharacterDataDelegate.ExecuteIfBound();
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnAddOrUpdateCustomCharacterDataResponseReceived Error accessing login server!"));
		OnErrorAddOrUpdateCustomCharacterDataDelegate.ExecuteIfBound(TEXT("Unknown error connecting to server!"));
	}
}

void UOWSPlayerControllerComponent::SaveAllPlayerData()
{
	//Not implemented
}

void UOWSPlayerControllerComponent::OnSaveAllPlayerDataResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
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

/***** Abilities *****/

//AddAbilityToCharacter
void UOWSPlayerControllerComponent::AddAbilityToCharacter(FString CharName, FString AbilityName, int32 AbilityLevel, FString CustomJSON)
{
	FAddAbilityToCharacterJSONPost AddAbilityToCharacterJSONPost;
	AddAbilityToCharacterJSONPost.CharacterName = CharName;
	AddAbilityToCharacterJSONPost.AbilityName = AbilityName;
	AddAbilityToCharacterJSONPost.AbilityLevel = AbilityLevel;
	AddAbilityToCharacterJSONPost.CharHasAbilitiesCustomJSON = CustomJSON;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(AddAbilityToCharacterJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("CharacterPersistenceAPI", "api/Abilities/AddAbilityToCharacter", PostParameters, &UOWSPlayerControllerComponent::OnAddAbilityToCharacterResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("AddAbilityToCharacter Error serializing AddAbilityToCharacterJSONPost!"));
	}
}

void UOWSPlayerControllerComponent::OnAddAbilityToCharacterResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		OnNotifyAddAbilityToCharacterDelegate.ExecuteIfBound();
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnAddAbilityToCharacterResponseReceived Error accessing server!"));
		OnErrorAddAbilityToCharacterDelegate.ExecuteIfBound(TEXT("Unknown error connecting to server!"));
	}
}

//GetAbilityBars
void UOWSPlayerControllerComponent::GetAbilityBars(FString CharName)
{
	FCharacterNameJSONPost CharacterNameJSONPost;
	CharacterNameJSONPost.CharacterName = CharName;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(CharacterNameJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("CharacterPersistenceAPI", "api/Abilities/GetAbilityBars", PostParameters, &UOWSPlayerControllerComponent::OnGetAbilityBarsResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("GetAbilityBars Error serializing GetCharacterStatsJSONPost!"));
	}
}

void UOWSPlayerControllerComponent::OnGetAbilityBarsResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		TArray<TSharedPtr<FJsonValue>> JsonArray;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonArray))
		{
			TArray<FAbilityBar> AbilityBars;

			FJsonObjectConverter::JsonArrayToUStruct(JsonArray, &AbilityBars, 0, 0);

			OnNotifyGetAbilityBarsDelegate.ExecuteIfBound(AbilityBars);
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnGetAbilityBarsResponseReceived Server returned no data!"));
			OnErrorGetAbilityBarsDelegate.ExecuteIfBound(TEXT("OnGetAbilityBarsResponseReceived Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnGetAbilityBarsResponseReceived Error accessing server!"));
		OnErrorGetAbilityBarsDelegate.ExecuteIfBound(TEXT("OnGetAbilityBarsResponseReceived Error accessing API server!"));
	}
}

//RemoveAbilityFromCharacter
void UOWSPlayerControllerComponent::RemoveAbilityFromCharacter(FString CharName, FString AbilityName)
{
	FRemoveAbilityFromCharacterJSONPost RemoveAbilityFromCharacterJSONPost;
	RemoveAbilityFromCharacterJSONPost.CharacterName = CharName;
	RemoveAbilityFromCharacterJSONPost.AbilityName = AbilityName;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(RemoveAbilityFromCharacterJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("CharacterPersistenceAPI", "api/Abilities/RemoveAbilityFromCharacter", PostParameters, &UOWSPlayerControllerComponent::OnRemoveAbilityFromCharacterResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("RemoveAbilityFromCharacter Error serializing RemoveAbilityFromCharacterJSONPost!"));
	}
}

void UOWSPlayerControllerComponent::OnRemoveAbilityFromCharacterResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		OnNotifyRemoveAbilityFromCharacterDelegate.ExecuteIfBound();
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnRemoveAbilityFromCharacterResponseReceived Error accessing server!"));
		OnErrorRemoveAbilityFromCharacterDelegate.ExecuteIfBound(TEXT("Unknown error accessing API server!"));
	}
}

//PlayerLogout
void UOWSPlayerControllerComponent::PlayerLogout(FString CharName)
{
	//CharName will be empty if the user has not connected to a server yet.  This logout method is not intended to be used during the character selection screen.
	if (CharName.IsEmpty())
	{
		return;
	}

	FCharacterNameJSONPost CharacterNameJSONPost;
	CharacterNameJSONPost.CharacterName = CharName;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(CharacterNameJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("CharacterPersistenceAPI", "api/Characters/PlayerLogout", PostParameters, &UOWSPlayerControllerComponent::OnPlayerLogoutResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("PlayerLogout Error serializing GetCharacterStatsJSONPost!"));
	}
}

void UOWSPlayerControllerComponent::OnPlayerLogoutResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			UE_LOG(OWS, Verbose, TEXT("Logout Successful!"));
			OnNotifyPlayerLogoutDelegate.ExecuteIfBound();
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnPlayerLogoutResponseReceived Server returned no data!"));
			OnErrorPlayerLogoutDelegate.ExecuteIfBound(TEXT("Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnPlayerLogoutResponseReceived Error accessing server!"));
		OnErrorPlayerLogoutDelegate.ExecuteIfBound(TEXT("Error accessing server!"));
	}
}

//CreateCharacter
void UOWSPlayerControllerComponent::CreateCharacter(FString UserSessionGUID, FString CharacterName, FString ClassName)
{
	FCreateCharacterJSONPost CreateCharacterJSONPost;
	CreateCharacterJSONPost.UserSessionGUID = UserSessionGUID;
	CreateCharacterJSONPost.CharacterName = CharacterName;
	CreateCharacterJSONPost.ClassName = ClassName;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(CreateCharacterJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("PublicAPI", "api/Users/CreateCharacter", PostParameters, &UOWSPlayerControllerComponent::OnCreateCharacterResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("CreateCharacter Error serializing CreateCharacterJSONPost!"));
	}
}

void UOWSPlayerControllerComponent::OnCreateCharacterResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		FString ResponseString = Response->GetContentAsString();

		FCreateCharacter CreateCharacter;

		if (!FJsonObjectConverter::JsonObjectStringToUStruct(ResponseString, &CreateCharacter, 0, 0))
		{
			OnErrorCreateCharacterDelegate.ExecuteIfBound(TEXT("Could not deserialize CreateCharacter JSON to CreateCharacter struct!"));
			return;
		}

		if (!CreateCharacter.ErrorMessage.IsEmpty())
		{
			OnErrorCreateCharacterDelegate.ExecuteIfBound(*CreateCharacter.ErrorMessage);
			return;
		}

		UE_LOG(OWS, Verbose, TEXT("OnCreateCharacterResponseReceived Success!"));
		OnNotifyCreateCharacterDelegate.ExecuteIfBound(CreateCharacter);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnCreateCharacterResponseReceived Error accessing login server!"));
		OnErrorCreateCharacterDelegate.ExecuteIfBound(TEXT("Unknown error connecting to server!"));
	}
}

//Create Character Using Default Character Values
void UOWSPlayerControllerComponent::CreateCharacterUsingDefaultCharacterValues(FString UserSessionGUID, FString CharacterName, FString DefaultSetName)
{
	// UGameInstance* GameInstance = UGameplayStatics::GetGameInstance(this);
	// GameInstance->GetSubsystem<UOWSAPISubsystem>()->CreateCharacterUsingDefaultCharacterValues(UserSessionGUID, CharacterName, DefaultSetName);
}

//Logout
void UOWSPlayerControllerComponent::Logout(FString UserSessionGUID)
{
	// UGameInstance* GameInstance = UGameplayStatics::GetGameInstance(this);
	// GameInstance->GetSubsystem<UOWSAPISubsystem>()->Logout(UserSessionGUID);
}

//Remove Character
void UOWSPlayerControllerComponent::RemoveCharacter(FString UserSessionGUID, FString CharacterName)
{
	FRemoveCharacterJSONPost RemoveCharacterJSONPost;
	RemoveCharacterJSONPost.UserSessionGUID = UserSessionGUID;
	RemoveCharacterJSONPost.CharacterName = CharacterName;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(RemoveCharacterJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("PublicAPI", "api/Users/RemoveCharacter", PostParameters, &UOWSPlayerControllerComponent::OnRemoveCharacterResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("RemoveCharacter Error serializing RemoveCharacterJSONPost!"));
	}
}

void UOWSPlayerControllerComponent::OnRemoveCharacterResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		FString ResponseString = Response->GetContentAsString();
		FSuccessAndErrorMessage SuccessAndErrorMessage;
		if (!FJsonObjectConverter::JsonObjectStringToUStruct(ResponseString, &SuccessAndErrorMessage, 0, 0))
		{
			UE_LOG(OWS, Error, TEXT("OnRemoveCharacterResponseReceived Error deserializing SuccessAndErrorMessage!"));
			OnErrorRemoveCharacterDelegate.ExecuteIfBound(TEXT("OnRemoveCharacterResponseReceived Error deserializing SuccessAndErrorMessage!"));
		}

		if (!SuccessAndErrorMessage.ErrorMessage.IsEmpty())
		{
			OnErrorRemoveCharacterDelegate.ExecuteIfBound(*SuccessAndErrorMessage.ErrorMessage);
			return;
		}

		UE_LOG(OWS, Verbose, TEXT("OnRemoveCharacterResponseReceived Success!"));
		OnNotifyRemoveCharacterDelegate.ExecuteIfBound();
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnRemoveCharacterResponseReceived Error accessing server!"));
		OnErrorRemoveCharacterDelegate.ExecuteIfBound(TEXT("OnRemoveCharacterResponseReceived Error accessing server!"));
	}
}

//LaunchZoneInstance
void UOWSPlayerControllerComponent::LaunchZoneInstance(FString CharacterName, FString ZoneName, ERPGPlayerGroupType::PlayerGroupType GroupType)
{
	FLaunchZoneInstance LaunchZoneInstance;
	LaunchZoneInstance.CharacterName = CharacterName;
	LaunchZoneInstance.ZoneName = ZoneName;
	LaunchZoneInstance.PlayerGroupTypeID = GroupType;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(LaunchZoneInstance, PostParameters))
	{
		ProcessOWS2POSTRequest("PublicAPI", "api/Users/GetServerToConnectTo", PostParameters, &UOWSPlayerControllerComponent::OnLaunchZoneInstanceResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("LaunchZoneInstance Error serializing LaunchZoneInstance!"));
	}
}

void UOWSPlayerControllerComponent::OnLaunchZoneInstanceResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	FString ServerAndPort;

	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			FString ServerIP = JsonObject->GetStringField(TEXT("serverip"));
			FString Port = JsonObject->GetStringField(TEXT("port"));

			ServerAndPort = ServerIP + FString(TEXT(":")) + Port;

			UE_LOG(OWS, Verbose, TEXT("OnLaunchZoneInstanceResponseReceived: %s:%s"), *ServerIP, *Port);

			OnNotifyLaunchZoneInstanceDelegate.ExecuteIfBound(ServerAndPort);
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnLaunchZoneInstanceResponseReceived Server returned no data!  This usually means the dungeon server instance failed to spin up before the timeout was reached."));
			OnErrorLaunchZoneInstanceDelegate.ExecuteIfBound(TEXT("Server returned no data!  This usually means the dungeon server instance failed to spin up before the timeout was reached."));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnLaunchZoneInstanceResponseReceived Error accessing server!"));
		OnErrorLaunchZoneInstanceDelegate.ExecuteIfBound(TEXT("Error accessing server!"));
	}
}

void UOWSPlayerControllerComponent::AddQuestListToDatabase(FString JSONString)
{
	ProcessOWS2POSTRequest("CharacterPersistenceAPI", "api/Characters/AddQuestListToDatabase", JSONString, &UOWSPlayerControllerComponent::OnAddQuestListToDatabaseResponseReceived);
}

void UOWSPlayerControllerComponent::OnAddQuestListToDatabaseResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{

}