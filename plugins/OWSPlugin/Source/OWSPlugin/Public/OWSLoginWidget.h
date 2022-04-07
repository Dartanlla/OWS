// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "Blueprint/UserWidget.h"
#include "Runtime/JsonUtilities/Public/JsonObjectConverter.h"
#include "Runtime/Online/HTTP/Public/Http.h"
#include "OWS2API.h"
#include "OWSLoginWidget.generated.h"



/**
 * 
 */
UCLASS()
class OWSPLUGIN_API UOWSLoginWidget : public UUserWidget
{
	GENERATED_BODY()

	FHttpModule* Http;
	
public:	

	UOWSLoginWidget(const class FObjectInitializer& ObjectInitializer);

	UPROPERTY(BlueprintReadWrite)
		FString OWSAPICustomerKey;

	UPROPERTY(BlueprintReadWrite, Category = "Config")
		FString OWS2APIPath = "";

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Login")
		float LoginTimeout = 30.f;

	//LoginAndCreateSession
	UFUNCTION(BlueprintCallable, Category = "Login")
		void LoginAndCreateSession(FString Email, FString Password);

	void OnLoginAndCreateSessionResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	UFUNCTION(BlueprintImplementableEvent, Category = "Login")
		void NotifyLoginAndCreateSession(const FString &UserSessionGUID);

	UFUNCTION(BlueprintImplementableEvent, Category = "Login")
		void ErrorLoginAndCreateSession(const FString &ErrorMsg);

	//ExternalLoginAndCreateSession
	UFUNCTION(BlueprintCallable, Category = "Login")
		void ExternalLoginAndCreateSession(FString Email, FString Password);

	void OnExternalLoginAndCreateSessionResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	UFUNCTION(BlueprintImplementableEvent, Category = "Login")
		void NotifyExternalLoginAndCreateSession(const FString &UserSessionGUID);

	UFUNCTION(BlueprintImplementableEvent, Category = "Login")
		void ErrorExternalLoginAndCreateSession(const FString &ErrorMsg);

	//Register
	UFUNCTION(BlueprintCallable, Category = "Login")
		void Register(FString Email, FString Password, FString FirstName, FString LastName);

	void OnRegisterResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	UFUNCTION(BlueprintImplementableEvent, Category = "Login")
		void NotifyRegister();

	UFUNCTION(BlueprintImplementableEvent, Category = "Login")
		void ErrorRegister(const FString &ErrorMsg);

protected:
	void ProcessOWS2POSTRequest(FString ApiToCall, FString PostParameters, void (UOWSLoginWidget::* InMethodPtr)(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful));
	void GetJsonObjectFromResponse(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful, FString CallingMethodName, FString &ErrorMsg, TSharedPtr<FJsonObject>& JsonObject);

	template <typename T>
	TSharedPtr<T> GetStructFromJsonObject(TSharedPtr<FJsonObject> JsonObject)
	{
		TSharedPtr<T> Result = MakeShareable(new T);
		FJsonObjectConverter::JsonObjectToUStruct(JsonObject.ToSharedRef(), T::StaticStruct(), Result.Get(), 0, 0);
		return Result;
	}

};
