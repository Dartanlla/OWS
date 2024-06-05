// Copyright 2022 Sabre Dart Studios

#pragma once

#include "CoreMinimal.h"
#include "OWSPlugin.h"
#include "OWS2API.h"
#include "Subsystems/GameInstanceSubsystem.h"
#include "Runtime/Online/HTTP/Public/Http.h"
#include "JsonObjectConverter.h"
#include "OWSAPISubsystem.generated.h"

//Get Global Data Item
DECLARE_DELEGATE_OneParam(FNotifyGetGlobalDataItemDelegate, TSharedPtr<FGlobalDataItem> GlobalDataItem)
DECLARE_DELEGATE_OneParam(FErrorGetGlobalDataItemDelegate, const FString&)

//Add or Update Global Data Item
DECLARE_DELEGATE(FNotifyAddOrUpdateGlobalDataItemDelegate)
DECLARE_DELEGATE_OneParam(FErrorAddOrUpdateGlobalDataItemDelegate, const FString&)

//Create Character Using Default Character Values
DECLARE_DELEGATE(FNotifyCreateCharacterUsingDefaultCharacterValuesDelegate)
DECLARE_DELEGATE_OneParam(FErrorCreateCharacterUsingDefaultCharacterValuesDelegate, const FString&)

//Logout
DECLARE_DELEGATE(FNotifyLogoutDelegate)
DECLARE_DELEGATE_OneParam(FErrorLogoutDelegate, const FString&)


/**
 * 
 */
UCLASS()
class OWSPLUGIN_API UOWSAPISubsystem : public UGameInstanceSubsystem
{
	GENERATED_BODY()
	
public:

	UPROPERTY(BlueprintReadWrite)
		FString OWSAPICustomerKey;

	UPROPERTY(BlueprintReadWrite, Category = "Config")
		FString OWS2APIPath = "";

	UPROPERTY(BlueprintReadWrite, Category = "Config")
		FString OWS2InstanceManagementAPIPath = "";

	UPROPERTY(BlueprintReadWrite, Category = "Config")
		FString OWS2CharacterPersistenceAPIPath = "";

	UPROPERTY(BlueprintReadWrite, Category = "Config")
		FString OWS2GlobalDataAPIPath = "";

	UPROPERTY(BlueprintReadWrite, Category = "Config")
		FString OWSEncryptionKey = "";

protected:
	FHttpModule* Http;

public:
	//Get Global Data Item
	UFUNCTION(BlueprintCallable, Category = "GlobalData")
		void GetGlobalDataItem(FString GlobalDataKey);

	void OnGetGlobalDataItemResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyGetGlobalDataItemDelegate OnNotifyGetGlobalDataItemDelegate;
	FErrorGetGlobalDataItemDelegate OnErrorGetGlobalDataItemDelegate;

	//Add or Update Global Data Item
	UFUNCTION(BlueprintCallable, Category = "GlobalData")
		void AddOrUpdateGlobalDataItem(FString GlobalDataKey, FString GlobalDataValue);

	void OnAddOrUpdateGlobalDataItemResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyAddOrUpdateGlobalDataItemDelegate OnNotifyAddOrUpdateGlobalDataItemDelegate;
	FErrorAddOrUpdateGlobalDataItemDelegate OnErrorAddOrUpdateGlobalDataItemDelegate;

	//Create Character Using Default Character Values
	UFUNCTION(BlueprintCallable, Category = "Users")
		void CreateCharacterUsingDefaultCharacterValues(FString UserSessionGUID, FString CharacterName, FString DefaultSetName);

	void OnCreateCharacterUsingDefaultCharacterValuesResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyCreateCharacterUsingDefaultCharacterValuesDelegate OnNotifyCreateCharacterUsingDefaultCharacterValuesDelegate;
	FErrorCreateCharacterUsingDefaultCharacterValuesDelegate OnErrorCreateCharacterUsingDefaultCharacterValuesDelegate;

	//Logout
	UFUNCTION(BlueprintCallable, Category = "Users")
		void Logout(FString UserSessionGUID);

	void OnLogoutResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	FNotifyLogoutDelegate OnNotifyLogoutDelegate;
	FErrorLogoutDelegate OnErrorLogoutDelegate;

protected:
	void ProcessOWS2POSTRequest(FString ApiModuleToCall, FString ApiToCall, FString PostParameters, void (UOWSAPISubsystem::* InMethodPtr)(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful));
	void ProcessOWS2GETRequest(FString ApiModuleToCall, FString ApiToCall, void (UOWSAPISubsystem::* InMethodPtr)(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful));
	void GetJsonObjectFromResponse(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful, FString CallingMethodName, FString& ErrorMsg, TSharedPtr<FJsonObject>& JsonObject);

	template <typename T>
	TSharedPtr<T> GetStructFromJsonObject(TSharedPtr<FJsonObject> JsonObject)
	{
		TSharedPtr<T> Result = MakeShareable(new T);
		FJsonObjectConverter::JsonObjectToUStruct(JsonObject.ToSharedRef(), T::StaticStruct(), Result.Get(), 0, 0);
		return Result;
	}

	// Begin USubsystem
	virtual void Initialize(FSubsystemCollectionBase& Collection) override;
	virtual void Deinitialize() override;
	// End USubsystem
};
