// Fill out your copyright notice in the Description page of Project Settings.

#include "OWSGameMode.h"
#include "OWSPlugin.h"
#include "OWSGameInstance.h"
#include "OWSPlayerState.h"
#include "OWSPlayerController.h"
#include "OWSAPISubsystem.h"
#include "Runtime/Engine/Classes/Kismet/GameplayStatics.h"
#include "Runtime/Core/Public/Misc/ConfigCacheIni.h"

AOWSGameMode::AOWSGameMode()
{
	InactivePlayerStateLifeSpan = 1;
	ZoneInstanceID = 0;

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

	//Create UOWSPlayerControllerComponent and bind delegates
	//OWSGameModeComponent = CreateDefaultSubobject<UOWSGameModeComponent>(TEXT("OWS Game Mode Component"));


	UGameInstance* GameInstance = UGameplayStatics::GetGameInstance(this);
	//GameInstance will be null on Editor startup, but will have a valid refernce when playing the game
	if (GameInstance)
	{
		InitializeOWSAPISubsystemOnGameMode();
	}
}

void AOWSGameMode::InitializeOWSAPISubsystemOnGameMode()
{
	UGameInstance* GameInstance = UGameplayStatics::GetGameInstance(this);
	GameInstance->GetSubsystem<UOWSAPISubsystem>()->OnNotifyGetGlobalDataItemDelegate.BindUObject(this, &AOWSGameMode::GetGlobalDataItemSuccess);
	GameInstance->GetSubsystem<UOWSAPISubsystem>()->OnErrorGetGlobalDataItemDelegate.BindUObject(this, &AOWSGameMode::GetGlobalDataItemError);
	GameInstance->GetSubsystem<UOWSAPISubsystem>()->OnNotifyAddOrUpdateGlobalDataItemDelegate.BindUObject(this, &AOWSGameMode::AddOrUpdateGlobalDataItemSuccess);
	GameInstance->GetSubsystem<UOWSAPISubsystem>()->OnErrorAddOrUpdateGlobalDataItemDelegate.BindUObject(this, &AOWSGameMode::AddOrUpdateGlobalDataItemError);
}

void AOWSGameMode::GetGlobalDataItemSuccess(TSharedPtr<FGlobalDataItem> GlobalDataItem)
{
	NotifyGetGlobalDataItem(GlobalDataItem->GlobalDataKey, GlobalDataItem->GlobalDataValue);
}
void AOWSGameMode::GetGlobalDataItemError(const FString& ErrorMsg)
{
	ErrorGetGlobalDataItem(ErrorMsg);
}

void AOWSGameMode::AddOrUpdateGlobalDataItemSuccess()
{
	NotifyAddOrUpdateGlobalDataItem();
}
void AOWSGameMode::AddOrUpdateGlobalDataItemError(const FString& ErrorMsg)
{
	ErrorAddOrUpdateGlobalDataItem(ErrorMsg);
}

void AOWSGameMode::ProcessOWS2POSTRequest(FString ApiModuleToCall, FString ApiToCall, FString PostParameters, void (AOWSGameMode::* InMethodPtr)(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful))
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

void AOWSGameMode::GetJsonObjectFromResponse(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful, FString CallingMethodName, FString& ErrorMsg, TSharedPtr<FJsonObject>& JsonObject)
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
			ErrorMsg = CallingMethodName + " - Error Deserializing JsonObject!";
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("%s - Response was unsuccessful or invalid!"), *CallingMethodName);
		ErrorMsg = CallingMethodName + " - Response was unsuccessful or invalid!";
	}
}

void AOWSGameMode::StartPlay()
{
	Super::StartPlay();

	if (GetLocalRole() == ROLE_Authority)
	{
		//Get a list of all item definitions
		//GetAllInventoryItems();

		//Get the ZoneInstanceID
		FString CommandLineZoneInstanceID;
		FParse::Value(FCommandLine::Get(), TEXT("zoneinstanceid="), CommandLineZoneInstanceID);
		ZoneInstanceID = FCString::Atoi(*CommandLineZoneInstanceID);

		UE_LOG(OWS, Warning, TEXT("OWSGameMode::StartPlay - ZoneInstanceID: %d"), ZoneInstanceID)

		//Lookup which Zone this server is running for and get the ZoneName into IAmZoneName var
		GetZoneInstanceFromZoneInstanceID(ZoneInstanceID);

		//Change Status of the Zone Instance to 2 (ready for players to connect)
		UpdateNumberOfPlayers();

		if (GetCharactersOnlineIntervalInSeconds > 0.f)
		{
			GetWorld()->GetTimerManager().SetTimer(OnGetAllCharactersOnlineTimerHandle, this, &AOWSGameMode::GetAllCharactersOnline, GetCharactersOnlineIntervalInSeconds, true);
		}

		if (UpdateServerStatusEveryXSeconds > 0.f)
		{
			GetWorld()->GetTimerManager().SetTimer(UpdateServerStatusEveryXSecondsTimerHandle, this, &AOWSGameMode::UpdateNumberOfPlayers, UpdateServerStatusEveryXSeconds, true);
		}

		if (SaveIntervalInSeconds > 0.f)
		{
			GetWorld()->GetTimerManager().SetTimer(SaveAllPlayerLocationsTimerHandle, this, &AOWSGameMode::SaveAllPlayerLocations, SaveIntervalInSeconds, true);
		}
	}
}

FString AOWSGameMode::InitNewPlayer(APlayerController* NewPlayerController, const FUniqueNetIdRepl& UniqueId, const FString& Options, const FString& Portal)
{
	FString retString = Super::InitNewPlayer(NewPlayerController, UniqueId, Options, Portal);

	UE_LOG(OWS, Verbose, TEXT("InitNewPlayer Started"));

	if (!GetWorld())
	{
		UE_LOG(OWS, Error, TEXT("OWSGameMode::InitNewPlayer - No UWorld Found!"));
		return retString;
	}

	UOWSGameInstance* GameInstance = Cast<UOWSGameInstance>(GetWorld()->GetGameInstance());

	if (!GameInstance)
	{
		UE_LOG(OWS, Error, TEXT("OWSGameMode::InitNewPlayer - No OWS Game Instance Found!  Make sure you are using an OWSGameInstance inherited Game Instance Class."));
		return retString;
	}

	FString PLX = "";
	FString PLY = "";
	FString PLZ = "";
	FString PRX = "";
	FString PRY = "";
	FString PRZ = "";
	FString PlayerName1 = "";
	FString UserSessionGUID = "";

	FString EncryptedIDData = UGameplayStatics::ParseOption(Options, TEXT("ID"));

	if (!EncryptedIDData.IsEmpty())
	{
		FString IDData = GameInstance->DecryptWithAES(EncryptedIDData, OWSEncryptionKey);

		UE_LOG(OWS, Verbose, TEXT("Raw options: %s"), *IDData);

		FString DecodedIDData = FGenericPlatformHttp::UrlDecode(IDData);

		UE_LOG(OWS, Verbose, TEXT("Decoded options: %s"), *DecodedIDData);

		TArray<FString> SplitArray;
		DecodedIDData.ParseIntoArray(SplitArray, TEXT("|"), false);

		if (SplitArray.Num() < 8)
		{
			UE_LOG(OWS, Error, TEXT("OWSGameMode::InitNewPlayer - Not enough parameters in IDData! - %s"), *DecodedIDData);
			return retString;
		}

		PLX = SplitArray[0];
		PLY = SplitArray[1];
		PLZ = SplitArray[2];
		PRX = SplitArray[3];
		PRY = SplitArray[4];
		PRZ = SplitArray[5];
		PlayerName1 = SplitArray[6];
		UserSessionGUID = SplitArray[7];

		UE_LOG(OWS, Verbose, TEXT("PlayerName: %s"), *PlayerName1);
		UE_LOG(OWS, Verbose, TEXT("UserSessionGUID: %s"), *UserSessionGUID);

		//FString OWSDefaultPawnClass = UGameplayStatics::ParseOption(DecodedOptions, TEXT("DPC"));
	}
	else
	{
		PlayerName1 = DebugCharacterName;
	}

	AOWSPlayerState* NewPlayerState = CastChecked<AOWSPlayerState>(NewPlayerController->PlayerState);

	if (!PLX.IsEmpty() && !PLY.IsEmpty() && !PLZ.IsEmpty())
	{
		float fPLX = FCString::Atof(*PLX);
		float fPLY = FCString::Atof(*PLY);
		float fPLZ = FCString::Atof(*PLZ);
		float fPRX = FCString::Atof(*PRX);
		float fPRY = FCString::Atof(*PRY);
		float fPRZ = FCString::Atof(*PRZ);

		UE_LOG(OWS, Warning, TEXT("Incoming start location is %f, %f, %f"), fPLX, fPLY, fPLZ);

		NewPlayerState->PlayerStartLocation.X = fPLX;
		NewPlayerState->PlayerStartLocation.Y = fPLY;
		NewPlayerState->PlayerStartLocation.Z = fPLZ;
		NewPlayerState->PlayerStartRotation.Roll = fPRX;
		NewPlayerState->PlayerStartRotation.Pitch = fPRY;
		NewPlayerState->PlayerStartRotation.Yaw = fPRZ;
	}
	else
	{
		NewPlayerState->PlayerStartLocation.X = DebugStartLocation.X;
		NewPlayerState->PlayerStartLocation.Y = DebugStartLocation.Y;
		NewPlayerState->PlayerStartLocation.Z = DebugStartLocation.Z;
		NewPlayerState->PlayerStartRotation.Roll = 0;
		NewPlayerState->PlayerStartRotation.Pitch = 0;
		NewPlayerState->PlayerStartRotation.Yaw = 0;

		UE_LOG(OWS, Warning, TEXT("Using Debug Start Location: %f, %f, %f"), DebugStartLocation.X, DebugStartLocation.Y, DebugStartLocation.Z);
	}

	NewPlayerState->SetPlayerName(PlayerName1);
	NewPlayerState->UserSessionGUID = UserSessionGUID;
	//NewPlayerState->DefaultPawnClass = OWSDefaultPawnClass;

	return retString;
}

APawn * AOWSGameMode::SpawnDefaultPawnFor_Implementation(AController * NewPlayer, class AActor * StartSpot)
{
	UE_LOG(OWS, Verbose, TEXT("AOWSGameMode = Start SpawnDefaultPawnFor"));

	AOWSPlayerState* NewPlayerState = CastChecked<AOWSPlayerState>(NewPlayer->PlayerState);
	UE_LOG(OWS, Warning, TEXT("Spawn Location is %f, %f, %f"), NewPlayerState->PlayerStartLocation.X, NewPlayerState->PlayerStartLocation.Y, NewPlayerState->PlayerStartLocation.Z);
	UE_LOG(OWS, Warning, TEXT("Spawn Rotation is %f, %f, %f"), NewPlayerState->PlayerStartRotation.Roll, NewPlayerState->PlayerStartRotation.Pitch, NewPlayerState->PlayerStartRotation.Yaw);

	FActorSpawnParameters SpawnInfo;
	SpawnInfo.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
	SpawnInfo.Owner = this;
	SpawnInfo.Instigator = GetInstigator();
	SpawnInfo.ObjectFlags |= RF_Transient;

	SpawnInfo.bDeferConstruction = false;
	APawn *  retPawn;

	/*if (!NewPlayerState->DefaultPawnClass.IsEmpty())
	{
		//Example: /Game/ThirdPersonBP/Blueprints/BlueprintName.BlueprintName
		UE_LOG(LogTemp, Warning, TEXT("Attempting to spawn custom pawn in SpawnDefaultPawnFor_Implementation: %s"), *NewPlayerState->DefaultPawnClass);
		retPawn = GetWorld()->SpawnActor<APawn>(LoadClass<APawn>(NULL, *NewPlayerState->DefaultPawnClass, NULL, LOAD_None, NULL), NewPlayerState->PlayerStartLocation, FRotator::ZeroRotator, SpawnInfo);
	}
	else
	{*/
		retPawn = GetWorld()->SpawnActor<APawn>(GetDefaultPawnClassForController(NewPlayer), NewPlayerState->PlayerStartLocation, NewPlayerState->PlayerStartRotation, SpawnInfo);
	//}

	if (retPawn == NULL)
	{
		UE_LOG(OWS, Error, TEXT("Couldn't spawn Pawn in SpawnDefaultPawnFor_Implementation"));
	}

	return retPawn;
}



void AOWSGameMode::GetAllInventoryItems()
{
	//Not implemented
	/*
	Http = &FHttpModule::Get();

	if (!RPGAPICustomerKey.IsEmpty())
	{
		TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
		Request->OnProcessRequestComplete().BindUObject(this, &AOWSGameMode::OnGetAllInventoryItemsResponseReceived);
		//This is the url on which to process the request
		FString url = FString(TEXT("http://" +  + "/RPGInventory/GetAllInventoryItems/"));

		FString PostParameters = FString(TEXT("CustomerGUID=")) + RPGAPICustomerKey;

		Request->SetURL(url);
		Request->SetVerb("POST");
		Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
		Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
		Request->SetContentAsString(PostParameters);
		Request->ProcessRequest();
	}*/
}

void AOWSGameMode::OnGetAllInventoryItemsResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	/*
	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			if (JsonObject->HasField("rows"))
			{
				TArray<TSharedPtr<FJsonValue>> Rows = JsonObject->GetArrayField("rows");

				for (int RowNum = 0; RowNum != Rows.Num(); RowNum++) {
					FInventoryItemStruct tempInventoryItem;
					TSharedPtr<FJsonObject> tempRow = Rows[RowNum]->AsObject();
					tempInventoryItem.ItemName = tempRow->GetStringField("ItemName");
					tempInventoryItem.ItemDescription = tempRow->GetStringField("ItemDescription");
					tempInventoryItem.ItemValue = tempRow->GetIntegerField("ItemValue");
					tempInventoryItem.ItemCanStack = tempRow->GetBoolField("ItemCanStack");
					tempInventoryItem.ItemStackSize = tempRow->GetIntegerField("ItemStackSize");
					tempInventoryItem.IsUsable = tempRow->GetBoolField("ItemIsUsable");
					tempInventoryItem.IsConsumedOnUse = tempRow->GetBoolField("ItemIsConsumedOnUse");
					//tempInventoryItem.DefaultNumberOfUses = tempRow->GetIntegerField("DefaultNumberOfUses");
					tempInventoryItem.ItemWeight = (float)tempRow->GetNumberField("ItemWeight");

					tempInventoryItem.ItemTypeID = tempRow->GetIntegerField("ItemTypeID");
					tempInventoryItem.ItemTypeDescription = tempRow->GetStringField("ItemTypeDesc");
					tempInventoryItem.ItemTypeQuality = tempRow->GetIntegerField("ItemTypeQuality");

					tempInventoryItem.UserItemType = tempRow->GetIntegerField("UserItemType");
					tempInventoryItem.EquipmentType = tempRow->GetIntegerField("EquipmentType");
					tempInventoryItem.EquipmentSlotType = tempRow->GetIntegerField("EquipmentSlotType");
					tempInventoryItem.ItemTier = tempRow->GetIntegerField("ItemTier");
					tempInventoryItem.ItemQuality = tempRow->GetIntegerField("ItemQuality");
					tempInventoryItem.ItemDuration = tempRow->GetIntegerField("ItemDuration");
					tempInventoryItem.CanBeDropped = tempRow->GetBoolField("CanBeDropped");
					tempInventoryItem.CanBeDestroyed = tempRow->GetBoolField("CanBeDestroyed");

					tempInventoryItem.CustomData = tempRow->GetStringField("CustomData");
					//tempInventoryItem.ItemMesh = tempRow->GetStringField("ItemMesh");
					//tempInventoryItem.MeshToUseForPickup = tempRow->GetStringField("MeshToUseForPickup");					
					tempInventoryItem.PremiumCurrencyPrice = tempRow->GetIntegerField("PremiumCurrencyPrice");
					tempInventoryItem.FreeCurrencyPrice = tempRow->GetIntegerField("FreeCurrencyPrice");

					tempInventoryItem.ItemMeshID = tempRow->GetIntegerField("ItemMeshID");
					tempInventoryItem.WeaponActorClassPath = tempRow->GetStringField("WeaponActorClass");
					tempInventoryItem.StaticMeshPath = tempRow->GetStringField("StaticMesh");
					tempInventoryItem.SkeletalMeshPath = tempRow->GetStringField("SkeletalMesh");
					tempInventoryItem.TextureToUseForIcon = tempRow->GetStringField("TextureToUseForIcon");

					tempInventoryItem.IconSlotWidth = tempRow->GetIntegerField("IconSlotWidth");
					tempInventoryItem.IconSlotHeight = tempRow->GetIntegerField("IconSlotHeight");

					if (tempInventoryItem.IconSlotWidth < 1)
						tempInventoryItem.IconSlotWidth = 1;

					if (tempInventoryItem.IconSlotHeight < 1)
						tempInventoryItem.IconSlotHeight = 1;

					AllInventoryItems.Add(tempInventoryItem);
					NotifyGetAllInventoryItems();
					BroadcastItemLibraryLoaded();
				}
			}
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnGetAllInventoryItemsResponseReceived Server returned no data!"));
			ErrorGetAllInventoryItems(TEXT("OnGetAllInventoryItemsResponseReceived Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnGetAllInventoryItemsResponseReceived Error accessing server!"));
		ErrorGetAllInventoryItems(TEXT("OnGetAllInventoryItemsResponseReceived Error accessing server!"));
	}*/
}


void AOWSGameMode::GetGlobalDataItem(FString GlobalDataKey)
{
	UGameInstance* GameInstance = UGameplayStatics::GetGameInstance(this);
	GameInstance->GetSubsystem<UOWSAPISubsystem>()->GetGlobalDataItem(GlobalDataKey);
}

void AOWSGameMode::AddOrUpdateGlobalDataItem(FString GlobalDataKey, FString GlobalDataValue)
{
	UGameInstance* GameInstance = UGameplayStatics::GetGameInstance(this);
	GameInstance->GetSubsystem<UOWSAPISubsystem>()->AddOrUpdateGlobalDataItem(GlobalDataKey, GlobalDataValue);
}


void AOWSGameMode::SaveAllPlayerLocations()
{
	UE_LOG(OWS, Verbose, TEXT("SaveAllPlayerLocations Started"));

	FString DataToSave;
	int PlayerIndex = 0;

	if (NextSaveGroupIndex < SplitSaveIntoHowManyGroups)
	{
		NextSaveGroupIndex++;
	}
	else
	{
		NextSaveGroupIndex = 0;
	}

	for (FConstPlayerControllerIterator Iterator = GetWorld()->GetPlayerControllerIterator(); Iterator; ++Iterator)
	{
		if (NextSaveGroupIndex == PlayerIndex % SplitSaveIntoHowManyGroups)
		{
			AOWSPlayerController* PlayerControllerToSave = Cast<AOWSPlayerController>(Iterator->Get());

			if (PlayerControllerToSave)
			{
				APawn* MyPawn = Iterator->Get()->GetPawn();

				if (MyPawn)
				{
					PlayerControllerToSave->LastCharacterLocation = MyPawn->GetActorLocation();
					PlayerControllerToSave->LastCharacterRotation = MyPawn->GetActorRotation();
				}

				FVector PawnLocation = PlayerControllerToSave->LastCharacterLocation;
				FRotator PawnRotation = PlayerControllerToSave->LastCharacterRotation;

				DataToSave.Append(PlayerControllerToSave->PlayerState->GetPlayerName());
				DataToSave.Append(":");
				DataToSave.Append(FString::SanitizeFloat(PawnLocation.X));
				DataToSave.Append(":");
				DataToSave.Append(FString::SanitizeFloat(PawnLocation.Y));
				DataToSave.Append(":");
				DataToSave.Append(FString::SanitizeFloat(PawnLocation.Z));
				DataToSave.Append(":");
				DataToSave.Append(FString::SanitizeFloat(PawnRotation.Roll));
				DataToSave.Append(":");
				DataToSave.Append(FString::SanitizeFloat(PawnRotation.Pitch));
				DataToSave.Append(":");
				DataToSave.Append(FString::SanitizeFloat(PawnRotation.Yaw));
				DataToSave.Append("|");
			}
		}

		PlayerIndex++;
	}

	if (DataToSave.Len() < 1)
	{
		UE_LOG(OWS, Verbose, TEXT("SaveAllPlayerLocations - No players to save in batch #: %i"), NextSaveGroupIndex);
		return;
	}

	DataToSave = DataToSave.Left(DataToSave.Len() - 1);
	UE_LOG(OWS, Verbose, TEXT("SaveAllPlayerLocations - Data to save: %s"), *DataToSave);

	FUpdateAllPlayerPositionsJSONPost UpdateAllPlayerPositionsJSONPost;
	UpdateAllPlayerPositionsJSONPost.SerializedPlayerLocationData = DataToSave;
	UpdateAllPlayerPositionsJSONPost.MapName = "";
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(UpdateAllPlayerPositionsJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("CharacterPersistenceAPI", "api/Characters/UpdateAllPlayerPositions", PostParameters, &AOWSGameMode::OnSaveAllPlayerLocationsResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("SaveAllPlayerLocations Error serializing GetAllCharactersJSONPost!"));
	}
}

void AOWSGameMode::OnSaveAllPlayerLocationsResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		UE_LOG(OWS, Verbose, TEXT("OnSaveAllPlayerLocationsResponseReceived Success!"));
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnSaveAllPlayerLocationsResponseReceived Error accessing server!"));
	}
}


void AOWSGameMode::GetAllCharactersOnline()
{
	/*
	TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
	Request->OnProcessRequestComplete().BindUObject(this, &AOWSGameMode::OnGetAllCharactersOnlineResponseReceived);
	//This is the url on which to process the request
	FString url = FString(TEXT("http://" +  + "/RPGServer/GetAllCharactersOnline/"));

	FString PostParameters = FString(TEXT("CustomerGUID=")) + OWSAPICustomerKey;

	Request->SetURL(url);
	Request->SetVerb("POST");
	Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
	Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
	Request->SetContentAsString(PostParameters);
	Request->ProcessRequest();
	*/
}

void AOWSGameMode::OnGetAllCharactersOnlineResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	/*
	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			//TArray<FCharactersOnlineStruct> tempCharactersOnline;

			CharactersOnline.Empty(CharactersOnline.Num());

			if (JsonObject->HasField("rows"))
			{
				TArray<TSharedPtr<FJsonValue>> Rows = JsonObject->GetArrayField("rows");

				for (int RowNum = 0; RowNum != Rows.Num(); RowNum++) {
					FCharactersOnlineStruct tempCharacterOnline;
					TSharedPtr<FJsonObject> tempRow = Rows[RowNum]->AsObject();

					tempCharacterOnline.CharacterID = tempRow->GetIntegerField("CharacterID");
					tempCharacterOnline.CharName = tempRow->GetStringField("CharName");
					tempCharacterOnline.CharacterLevel = tempRow->GetIntegerField("CharacterLevel");
					tempCharacterOnline.Gender = tempRow->GetIntegerField("Gender");
					tempCharacterOnline.Alignment = tempRow->GetIntegerField("Alignment");
					tempCharacterOnline.Fame = tempRow->GetIntegerField("Fame");
					tempCharacterOnline.TeamNumber = tempRow->GetIntegerField("TeamNumber");
				
					CharactersOnline.Add(tempCharacterOnline);
				}
			}
			//NotifyGetAllCharactersOnline(CharactersOnline);

			//UE_LOG(OWS, Error, TEXT("Total number of players online: %d"), CharactersOnline.Num());
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnGetAllCharactersOnlineResponseReceived Server returned no data!"));
			ErrorGetAllCharactersOnline(TEXT("OnGetAllCharactersOnlineResponseReceived Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnGetAllCharactersOnlineResponseReceived Error accessing server!"));
		ErrorGetAllCharactersOnline(TEXT("OnGetAllCharactersOnlineResponseReceived Error accessing server!"));
	}
	*/
}

bool AOWSGameMode::IsPlayerOnline(FString CharacterName)
{
	return CharactersOnline.ContainsByPredicate([&](FCharactersOnlineStruct Result) {return CharacterName == Result.CharName; });
}


void AOWSGameMode::GetZoneInstancesForZone(FString ZoneName)
{
	FGetZoneInstancesForZoneJSONPost GetZoneInstancesForZoneJSONPost;
	GetZoneInstancesForZoneJSONPost.Request.ZoneName = ZoneName;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(GetZoneInstancesForZoneJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("InstanceManagementAPI", "api/Instance/GetZoneInstancesForZone", PostParameters, &AOWSGameMode::OnGetZoneInstancesForZoneResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("GetZoneInstancesForZone Error serializing GetCharacterStatsJSONPost!"));
	}
}

void AOWSGameMode::OnGetZoneInstancesForZoneResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		TArray<TSharedPtr<FJsonValue>> JsonArray;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonArray))
		{
			TArray<FZoneInstance> ZoneInstances;

			FJsonObjectConverter::JsonArrayToUStruct(JsonArray, &ZoneInstances, 0, 0);

			NotifyGetZoneInstancesForZone(ZoneInstances);
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnGetZoneInstancesOfZoneResponseReceived Server returned no data!"));
			ErrorGetZoneInstancesForZone(TEXT("OnGetZoneInstancesForZoneResponseReceived Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnGetZoneInstancesOfZoneResponseReceived Error accessing server!"));
		ErrorGetZoneInstancesForZone(TEXT("OnGetZoneInstancesForZoneResponseReceived Server returned no data!"));
	}
}


void AOWSGameMode::GetZoneInstanceFromZoneInstanceID(int32 LookupZoneInstanceID)
{
	//int32 Port = GetWorld()->URL.Port;

	UE_LOG(OWS, Verbose, TEXT("GetZoneInstanceFromZoneInstanceID - Checking for ZoneInstanceID: %d"), LookupZoneInstanceID);

	TArray<FStringFormatArg> FormatParams;
	FormatParams.Add(LookupZoneInstanceID);
	FString PostParameters = FString::Format(TEXT("{ \"ZoneInstanceId\": {0} }"), FormatParams);

	ProcessOWS2POSTRequest("InstanceManagementAPI", "api/Instance/GetZoneInstance", PostParameters, &AOWSGameMode::OnGetZoneInstanceFromZoneInstanceIDResponseReceived);
}

void AOWSGameMode::OnGetZoneInstanceFromZoneInstanceIDResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		FGetServerInstanceFromPort ServerInstanceFromPort;
		FString JsonString = Response->GetContentAsString();
		if (FJsonObjectConverter::JsonObjectStringToUStruct(JsonString, &ServerInstanceFromPort))
		{
			if (ServerInstanceFromPort.ZoneName != "")
			{
				IAmZoneName = ServerInstanceFromPort.ZoneName;
				UE_LOG(OWS, Verbose, TEXT("I am ZoneName: %s"), *IAmZoneName);
				NotifyGetZoneInstanceFromZoneInstanceID(IAmZoneName);
			}
			else
			{
				UE_LOG(OWS, Warning, TEXT("OnGetZoneInstanceFromZoneInstanceIDResponseReceived No Rows!  Ignore this error if you are running from the editor in Play as Client mode!"));
				ErrorGetZoneInstanceFromZoneInstanceID(TEXT("OnGetZoneInstanceFromZoneInstanceIDResponseReceived No Rows!  Ignore this error if you are running from the editor in Play as Client mode!"));
			}
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnGetZoneInstanceFromZoneInstanceIDResponseReceived Server returned no data!"));
			ErrorGetZoneInstanceFromZoneInstanceID(TEXT("OnGetZoneInstanceFromZoneInstanceIDResponseReceived Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnGetZoneInstanceFromZoneInstanceIDResponseReceived Error accessing server!"));
		ErrorGetZoneInstanceFromZoneInstanceID(TEXT("OnGetZoneInstanceFromZoneInstanceIDResponseReceived Error accessing server!"));
	}
}

void AOWSGameMode::UpdateNumberOfPlayers()
{
	UE_LOG(OWS, Verbose, TEXT("UpdateNumberOfPlayers Started..."));

	FString NumberOfConnectedPlayers = FString::FromInt(NumPlayers);

	if (ZoneInstanceID < 1)
	{
		UE_LOG(OWS, Error, TEXT("UpdateNumberOfPlayers: ZoneInstanceId is empty!"));
		return;
	}

	FUpdateNumberOfPlayersJSONPost UpdateNumberOfPlayersJSONPost;
	UpdateNumberOfPlayersJSONPost.ZoneInstanceId = ZoneInstanceID;
	UpdateNumberOfPlayersJSONPost.NumberOfConnectedPlayers = NumberOfConnectedPlayers;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(UpdateNumberOfPlayersJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("InstanceManagementAPI", "api/Instance/UpdateNumberOfPlayers", PostParameters, &AOWSGameMode::OnUpdateNumberOfPlayersResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("UpdateNumberOfPlayers Error serializing UpdateNumberOfPlayersJSONPost!"));
	}
}

void AOWSGameMode::OnUpdateNumberOfPlayersResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		UE_LOG(OWS, Verbose, TEXT("OnUpdateNumberOfPlayersResponseReceived success!"));
	}
	else
	{
		UE_LOG(OWS, Verbose, TEXT("OnUpdateNumberOfPlayersResponseReceived Error accessing server!"));
	}
}


void AOWSGameMode::GetCurrentWorldTime()
{
	FString PostParameters = "{}";
	ProcessOWS2POSTRequest("InstanceManagementAPI", "api/Instance/GetCurrentWorldTime", PostParameters, &AOWSGameMode::OnGetCurrentWorldTimeResponseReceived);
}

void AOWSGameMode::OnGetCurrentWorldTimeResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			float fCurrentWorldTime;

			fCurrentWorldTime = JsonObject->GetNumberField(TEXT("CurrentWorldTime"));

			NotifyGetCurrentWorldTime(fCurrentWorldTime);
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnGetCurrentWorldTimeResponseReceived Server returned no data!"));
			ErrorGetCurrentWorldTime(TEXT("OnGetCurrentWorldTimeResponseReceived Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnGetCurrentWorldTimeResponseReceived Error accessing server!"));
		ErrorGetCurrentWorldTime(TEXT("OnGetCurrentWorldTimeResponseReceived Error accessing server!"));
	}
}

//Add Zone
void AOWSGameMode::AddZone(FString ZoneName, FString MapName, int SoftPlayerCap, int HardPlayerCap, int MapMode)
{
	FAddZoneJSONPost AddZoneJSONPost;
	AddZoneJSONPost.AddOrUpdateZone.ZoneName = ZoneName;
	AddZoneJSONPost.AddOrUpdateZone.MapName = MapName;
	AddZoneJSONPost.AddOrUpdateZone.SoftPlayerCap = SoftPlayerCap;
	AddZoneJSONPost.AddOrUpdateZone.HardPlayerCap = HardPlayerCap;
	AddZoneJSONPost.AddOrUpdateZone.MapMode = MapMode;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(AddZoneJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("InstanceManagementAPI", "api/Zones/AddZone", PostParameters, &AOWSGameMode::OnAddZoneResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("AddZone Error serializing AddZoneJSONPost!"));
	}
}

void AOWSGameMode::OnAddZoneResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	FString ErrorMsg;
	TSharedPtr<FJsonObject> JsonObject;
	GetJsonObjectFromResponse(Request, Response, bWasSuccessful, "OnAddZoneResponseReceived", ErrorMsg, JsonObject);
	if (!ErrorMsg.IsEmpty())
	{
		ErrorAddZone(ErrorMsg);
		return;
	}

	TSharedPtr<FSuccessAndErrorMessage> SuccessAndErrorMessage = GetStructFromJsonObject<FSuccessAndErrorMessage>(JsonObject);

	if (!SuccessAndErrorMessage->ErrorMessage.IsEmpty())
	{
		ErrorAddZone(*SuccessAndErrorMessage->ErrorMessage);
		return;
	}

	NotifyAddZone();
}


//Update Zone
/*
void AOWSGameMode::UpdateZone(int32 MapID, FString ZoneName, FString MapName, int SoftPlayerCap, int HardPlayerCap, int MapMode)
{
	FUpdateZoneJSONPost UpdateZoneJSONPost;
	UpdateZoneJSONPost.AddOrUpdateZone.MapID = MapID;
	UpdateZoneJSONPost.AddOrUpdateZone.ZoneName = ZoneName;
	UpdateZoneJSONPost.AddOrUpdateZone.MapName = MapName;
	UpdateZoneJSONPost.AddOrUpdateZone.SoftPlayerCap = SoftPlayerCap;
	UpdateZoneJSONPost.AddOrUpdateZone.HardPlayerCap = HardPlayerCap;
	UpdateZoneJSONPost.AddOrUpdateZone.MapMode = MapMode;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(UpdateZoneJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("InstanceManagementAPI", "api/Zones/UpdateZone", PostParameters, &AOWSGameMode::OnUpdateZoneResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("UpdateZone Error serializing UpdateZoneJSONPost!"));
	}
}

void AOWSGameMode::OnUpdateZoneResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	FString ErrorMsg;
	TSharedPtr<FJsonObject> JsonObject;
	GetJsonObjectFromResponse(Request, Response, bWasSuccessful, "OnUpdateZoneResponseReceived", ErrorMsg, JsonObject);
	if (!ErrorMsg.IsEmpty())
	{
		ErrorUpdateZone(ErrorMsg);
		return;
	}

	TSharedPtr<FSuccessAndErrorMessage> SuccessAndErrorMessage = GetStructFromJsonObject<FSuccessAndErrorMessage>(JsonObject);

	if (!SuccessAndErrorMessage->ErrorMessage.IsEmpty())
	{
		ErrorUpdateZone(*SuccessAndErrorMessage->ErrorMessage);
		return;
	}

	NotifyUpdateZone();
}
*/

FString AOWSGameMode::GetAddressURLAndPort()
{
	return GetWorld()->GetAddressURL();
}

void AOWSGameMode::AddItemMeshToAllPlayers(const FString& ItemName, const int32 ItemMeshID)
{
	for (FConstPlayerControllerIterator Iterator = GetWorld()->GetPlayerControllerIterator(); Iterator; ++Iterator)
	{
		AOWSPlayerController* MyPlayerController = Cast<AOWSPlayerController>(Iterator->Get());

		if (MyPlayerController)
		{
			MyPlayerController->AddItemToLocalMeshItemsMap(ItemName, ItemMeshID);
		}
	}
}

FInventoryItemStruct& AOWSGameMode::FindItemDefinition(FString ItemName)
{
	auto FoundEntry = AllInventoryItems.FindByPredicate([&](FInventoryItemStruct& InItem)
	{
		return InItem.ItemName == ItemName;
	});

	return (*FoundEntry);
}

AOWSPlayerController* AOWSGameMode::GetPlayerControllerFromCharacterName(const FString CharacterName)
{
	for (FConstPlayerControllerIterator Iterator = GetWorld()->GetPlayerControllerIterator(); Iterator; ++Iterator)
	{
		AOWSPlayerController* MyPlayerController = Cast<AOWSPlayerController>(Iterator->Get());

		if (MyPlayerController)
		{
			if (MyPlayerController->GetOWSPlayerState()->GetPlayerName() == CharacterName)
				return MyPlayerController;
		}
	}

	return nullptr;
}