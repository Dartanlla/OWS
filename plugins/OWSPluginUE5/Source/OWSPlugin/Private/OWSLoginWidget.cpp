// Fill out your copyright notice in the Description page of Project Settings.

#include "OWSLoginWidget.h"
#include "OWSPlugin.h"
#include "Runtime/Online/HTTP/Public/Http.h"
#include "OWSPlayerController.h"
#include "Runtime/Core/Public/Misc/ConfigCacheIni.h"

UOWSLoginWidget::UOWSLoginWidget(const class FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer)
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
}

//This method only calls the Public API
void UOWSLoginWidget::ProcessOWS2POSTRequest(FString ApiToCall, FString PostParameters, void (UOWSLoginWidget::* InMethodPtr)(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful))
{
	Http = &FHttpModule::Get();
	TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
	Request->OnProcessRequestComplete().BindUObject(this, InMethodPtr);

	FString OWS2APIPathToUse = OWS2APIPath;

	Request->SetURL(FString(OWS2APIPathToUse + ApiToCall));
	Request->SetVerb("POST");
	Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
	Request->SetHeader("Content-Type", TEXT("application/json"));
	Request->SetHeader(TEXT("X-CustomerGUID"), OWSAPICustomerKey);
	Request->SetContentAsString(PostParameters);
	Request->ProcessRequest();
}

void UOWSLoginWidget::GetJsonObjectFromResponse(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful, FString CallingMethodName, FString& ErrorMsg, TSharedPtr<FJsonObject>& JsonObject)
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

void UOWSLoginWidget::LoginAndCreateSession(FString Email, FString Password)
{
	FLoginAndCreateSessionJSONPost LoginAndCreateSessionJSONPost(Email, Password);
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(LoginAndCreateSessionJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("api/Users/LoginAndCreateSession", PostParameters, &UOWSLoginWidget::OnLoginAndCreateSessionResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("LoginAndCreateSession Error serializing FLoginAndCreateSessionJSONPost!"));
	}
}

void UOWSLoginWidget::OnLoginAndCreateSessionResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	FString ErrorMsg;
	TSharedPtr<FJsonObject> JsonObject;
	GetJsonObjectFromResponse(Request, Response, bWasSuccessful, "OnLoginAndCreateSessionResponseReceived", ErrorMsg, JsonObject);
	if (!ErrorMsg.IsEmpty())
	{
		ErrorLoginAndCreateSession(ErrorMsg);
		return;
	}

	TSharedPtr<FLoginAndCreateSession> LoginAndCreateSession = GetStructFromJsonObject<FLoginAndCreateSession>(JsonObject);

	if (!LoginAndCreateSession->ErrorMessage.IsEmpty())
	{
		ErrorLoginAndCreateSession(*LoginAndCreateSession->ErrorMessage);
		return;
	}

	if (!LoginAndCreateSession->Authenticated || LoginAndCreateSession->UserSessionGUID.IsEmpty())
	{
		ErrorLoginAndCreateSession("Unknown Login Error!  Make sure OWS 2 is running in debug mode in VS 2022 with docker-compose.  Then make sure your OWSAPICustomerKey in DefaultGame.ini matches your CustomerGUID in your database.");
		return;
	}

	NotifyLoginAndCreateSession(LoginAndCreateSession->UserSessionGUID);
}


void UOWSLoginWidget::ExternalLoginAndCreateSession(FString Email, FString Password)
{
	FLoginAndCreateSessionJSONPost LoginAndCreateSessionJSONPost(Email, Password);
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(LoginAndCreateSessionJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("api/Users/ExternalLoginAndCreateSession", PostParameters, &UOWSLoginWidget::OnExternalLoginAndCreateSessionResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("ExternalLoginAndCreateSession Error serializing FLoginAndCreateSessionJSONPost!"));
	}
}

void UOWSLoginWidget::OnExternalLoginAndCreateSessionResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	FString ErrorMsg;
	TSharedPtr<FJsonObject> JsonObject;
	GetJsonObjectFromResponse(Request, Response, bWasSuccessful, "OnExternalLoginAndCreateSessionResponseReceived", ErrorMsg, JsonObject);
	if (!ErrorMsg.IsEmpty())
	{
		ErrorExternalLoginAndCreateSession(ErrorMsg);
		return;
	}

	TSharedPtr<FLoginAndCreateSession> LoginAndCreateSession = GetStructFromJsonObject<FLoginAndCreateSession>(JsonObject);

	if (!LoginAndCreateSession->ErrorMessage.IsEmpty())
	{
		ErrorExternalLoginAndCreateSession(*LoginAndCreateSession->ErrorMessage);
		return;
	}

	NotifyExternalLoginAndCreateSession(LoginAndCreateSession->UserSessionGUID);
}



void UOWSLoginWidget::Register(FString Email, FString Password, FString FirstName, FString LastName)
{
	FRegisterJSONPost RegisterJSONPost(Email, Password, FirstName, LastName);
	FString PostParameters = "";
	if (FJsonObjectConverter::UStructToJsonObjectString(RegisterJSONPost, PostParameters))
	{
		ProcessOWS2POSTRequest("api/Users/RegisterUser", PostParameters, &UOWSLoginWidget::OnRegisterResponseReceived);
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("Register Error serializing FRegisterJSONPost!"));
	}
}

void UOWSLoginWidget::OnRegisterResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	FString ErrorMsg;
	TSharedPtr<FJsonObject> JsonObject;
	GetJsonObjectFromResponse(Request, Response, bWasSuccessful, "OnRegisterResponseReceived", ErrorMsg, JsonObject);
	if (!ErrorMsg.IsEmpty())
	{
		ErrorRegister(ErrorMsg);
		return;
	}

	TSharedPtr<FLoginAndCreateSession> RegisterAndCreateSession = GetStructFromJsonObject<FLoginAndCreateSession>(JsonObject);

	if (!RegisterAndCreateSession->ErrorMessage.IsEmpty())
	{
		ErrorRegister(*RegisterAndCreateSession->ErrorMessage);
		return;
	}

	NotifyRegister(RegisterAndCreateSession->UserSessionGUID);
}
