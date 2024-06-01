// Copyright 2022 Sabre Dart Studios

#include "OWSAPISubsystem.h"
#include "Runtime/Engine/Classes/Kismet/GameplayStatics.h"
#include "JsonObjectConverter.h"

void UOWSAPISubsystem::Initialize(FSubsystemCollectionBase& Collection)
{
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
		TEXT("OWS2GlobalDataAPIPath"),
		OWS2GlobalDataAPIPath,
		GGameIni
	);
}

void UOWSAPISubsystem::Deinitialize()
{

}

void UOWSAPISubsystem::GetJsonObjectFromResponse(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful, FString CallingMethodName, FString& ErrorMsg, TSharedPtr<FJsonObject>& JsonObject)
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

void UOWSAPISubsystem::ProcessOWS2POSTRequest(FString ApiModuleToCall, FString ApiToCall, FString PostParameters, void (UOWSAPISubsystem::* InMethodPtr)(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful))
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
	else if (ApiModuleToCall == "GlobalDataAPI")
	{
		OWS2APIPathToUse = OWS2GlobalDataAPIPath;
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

void UOWSAPISubsystem::ProcessOWS2GETRequest(FString ApiModuleToCall, FString ApiToCall, void (UOWSAPISubsystem::* InMethodPtr)(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful))
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
	else if (ApiModuleToCall == "GlobalDataAPI")
	{
		OWS2APIPathToUse = OWS2GlobalDataAPIPath;
	}
	else //When an ApiModuleToCall is not specified, use the PublicAPI
	{
		OWS2APIPathToUse = OWS2APIPath;
	}

	Request->SetURL(FString(OWS2APIPathToUse + ApiToCall));
	Request->SetVerb("GET");
	Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
	Request->SetHeader("Content-Type", TEXT("application/json"));
	Request->SetHeader(TEXT("X-CustomerGUID"), OWSAPICustomerKey);
	Request->ProcessRequest();
}


//Get Global Data Item
void UOWSAPISubsystem::GetGlobalDataItem(FString GlobalDataKey)
{
	FString Url = "api/GlobalData/GetGlobalDataItem/" + GlobalDataKey.TrimStartAndEnd();
	ProcessOWS2GETRequest("GlobalDataAPI", Url, &UOWSAPISubsystem::OnGetGlobalDataItemResponseReceived);
}

void UOWSAPISubsystem::OnGetGlobalDataItemResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	FString ErrorMsg;
	TSharedPtr<FJsonObject> JsonObject;
	GetJsonObjectFromResponse(Request, Response, bWasSuccessful, "OnGetGlobalDataItemResponseReceived", ErrorMsg, JsonObject);
	if (!ErrorMsg.IsEmpty())
	{
		OnErrorGetGlobalDataItemDelegate.ExecuteIfBound(ErrorMsg);
		return;
	}

	TSharedPtr<FGlobalDataItem> GlobalDataItem = GetStructFromJsonObject<FGlobalDataItem>(JsonObject);

	OnNotifyGetGlobalDataItemDelegate.ExecuteIfBound(GlobalDataItem);
}


//Add or Update Global Data
void UOWSAPISubsystem::AddOrUpdateGlobalDataItem(FString GlobalDataKey, FString GlobalDataValue)
{
	FGlobalDataItem GlobalDataItem;
	GlobalDataItem.GlobalDataKey = GlobalDataKey;
	GlobalDataItem.GlobalDataValue = GlobalDataValue;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(GlobalDataItem, PostParameters))
	{
		ProcessOWS2POSTRequest("GlobalDataAPI", "api/GlobalData/AddOrUpdateGlobalDataItem", PostParameters, &UOWSAPISubsystem::OnAddOrUpdateGlobalDataItemResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("AddOrUpdateGlobalDataItem Error serializing GlobalDataItem!"));
	}
}

void UOWSAPISubsystem::OnAddOrUpdateGlobalDataItemResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	FString ErrorMsg;
	TSharedPtr<FJsonObject> JsonObject;
	GetJsonObjectFromResponse(Request, Response, bWasSuccessful, "OnAddOrUpdateGlobalDataItemResponseReceived", ErrorMsg, JsonObject);
	if (!ErrorMsg.IsEmpty())
	{
		OnErrorAddOrUpdateGlobalDataItemDelegate.ExecuteIfBound(ErrorMsg);
		return;
	}

	TSharedPtr<FSuccessAndErrorMessage> SuccessAndErrorMessage = GetStructFromJsonObject<FSuccessAndErrorMessage>(JsonObject);

	if (!SuccessAndErrorMessage->ErrorMessage.IsEmpty())
	{
		OnErrorAddOrUpdateGlobalDataItemDelegate.ExecuteIfBound(*SuccessAndErrorMessage->ErrorMessage);
		return;
	}

	OnNotifyAddOrUpdateGlobalDataItemDelegate.ExecuteIfBound();
}


//Create Character Using Default Character Values
void UOWSAPISubsystem::CreateCharacterUsingDefaultCharacterValues(FString UserSessionGUID, FString CharacterName, FString DefaultSetName)
{
	FCreateCharacterUsingDefaultCharacterValues CreateCharacterUsingDefaultCharacterValues;
	CreateCharacterUsingDefaultCharacterValues.UserSessionGUID = UserSessionGUID;
	CreateCharacterUsingDefaultCharacterValues.CharacterName = CharacterName;
	CreateCharacterUsingDefaultCharacterValues.DefaultSetName = DefaultSetName;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(CreateCharacterUsingDefaultCharacterValues, PostParameters))
	{
		ProcessOWS2POSTRequest("PublicAPI", "api/Users/CreateCharacterUsingDefaultCharacterValues", PostParameters, 
			&UOWSAPISubsystem::OnCreateCharacterUsingDefaultCharacterValuesResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("CreateCharacterUsingDefaultCharacterValues Error serializing CreateCharacterUsingDefaultCharacterValues!"));
	}
}

void UOWSAPISubsystem::OnCreateCharacterUsingDefaultCharacterValuesResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	FString ErrorMsg;
	TSharedPtr<FJsonObject> JsonObject;
	GetJsonObjectFromResponse(Request, Response, bWasSuccessful, "OnCreateCharacterUsingDefaultCharacterValuesResponseReceived", ErrorMsg, JsonObject);
	if (!ErrorMsg.IsEmpty())
	{
		OnErrorCreateCharacterUsingDefaultCharacterValuesDelegate.ExecuteIfBound(ErrorMsg);
		return;
	}

	TSharedPtr<FSuccessAndErrorMessage> SuccessAndErrorMessage = GetStructFromJsonObject<FSuccessAndErrorMessage>(JsonObject);

	if (!SuccessAndErrorMessage->ErrorMessage.IsEmpty())
	{
		OnErrorCreateCharacterUsingDefaultCharacterValuesDelegate.ExecuteIfBound(*SuccessAndErrorMessage->ErrorMessage);
		return;
	}

	OnNotifyCreateCharacterUsingDefaultCharacterValuesDelegate.ExecuteIfBound();
}

//Logout
void UOWSAPISubsystem::Logout(FString UserSessionGUID)
{
	FLogout Logout;
	Logout.UserSessionGUID = UserSessionGUID;
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(Logout, PostParameters))
	{
		ProcessOWS2POSTRequest("PublicAPI", "api/Users/Logout", PostParameters,
			&UOWSAPISubsystem::OnLogoutResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("Logout Error serializing OnLogoutResponseReceived!"));
	}
}

void UOWSAPISubsystem::OnLogoutResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	FString ErrorMsg;
	TSharedPtr<FJsonObject> JsonObject;
	GetJsonObjectFromResponse(Request, Response, bWasSuccessful, "OnLogoutResponseReceived", ErrorMsg, JsonObject);
	if (!ErrorMsg.IsEmpty())
	{
		OnErrorLogoutDelegate.ExecuteIfBound(ErrorMsg);
		return;
	}

	TSharedPtr<FSuccessAndErrorMessage> SuccessAndErrorMessage = GetStructFromJsonObject<FSuccessAndErrorMessage>(JsonObject);

	if (!SuccessAndErrorMessage->ErrorMessage.IsEmpty())
	{
		OnErrorLogoutDelegate.ExecuteIfBound(*SuccessAndErrorMessage->ErrorMessage);
		return;
	}

	OnNotifyLogoutDelegate.ExecuteIfBound();
}