// Fill out your copyright notice in the Description page of Project Settings.

#include "OWSChatManager.h"
#include "OWSPlugin.h"
#include "Runtime/Online/HTTP/Public/Http.h"
#include "Runtime/Core/Public/Misc/ConfigCacheIni.h"


// Sets default values
AOWSChatManager::AOWSChatManager()
{
 	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = false;

	Http = &FHttpModule::Get();
}

// Called when the game starts or when spawned
void AOWSChatManager::BeginPlay()
{
	Super::BeginPlay();
	
}

// Called every frame
void AOWSChatManager::Tick( float DeltaTime )
{
	Super::Tick( DeltaTime );

}

void AOWSChatManager::SendGlobalChat(FString SentFromCharacterName, FString Message)
{
	/*
	FString PlayerName = SentFromCharacterName;
	PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

	FString FullMessageToSend = SentFromCharacterName + FString(TEXT(": ")) + Message;

	TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
	Request->OnProcessRequestComplete().BindUObject(this, &AOWSChatManager::OnSendGlobalChatResponseReceived);
	//This is the url on which to process the request
	FString url = FString(TEXT("http://" +  + "/Chat/AddGlobalChatMessage"));	

	FString PostParameters = FString(TEXT("id=")) + PlayerName
		+ FString(TEXT("&Message=")) + FullMessageToSend
		+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

	Request->SetURL(url);
	Request->SetVerb("POST");
	Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
	Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
	Request->SetContentAsString(PostParameters);
	Request->ProcessRequest();
	*/
}

void AOWSChatManager::OnSendGlobalChatResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	//UE_LOG(LogTemp, Warning, TEXT("Response %s"), *Response->GetContentAsString());
}

void AOWSChatManager::SendChatToChannel(FString SentFromCharacterName, FString Message, FString ChatChannelName)
{
	/*
	FString PlayerName = SentFromCharacterName;
	PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

	FString FullMessageToSend = SentFromCharacterName + FString(TEXT(": ")) + Message;

	TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
	Request->OnProcessRequestComplete().BindUObject(this, &AOWSChatManager::OnSendChatToChannelResponseReceived);
	//This is the url on which to process the request
	FString url = FString(TEXT("http://" +  + "/Chat/AddGroupChatMessage"));

	FString PostParameters = FString(TEXT("id=")) + PlayerName
		+ FString(TEXT("&Message=")) + FullMessageToSend		
		+ FString(TEXT("&ChatGroupName=")) + ChatChannelName
		+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

	Request->SetURL(url);
	Request->SetVerb("POST");
	Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
	Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
	Request->SetContentAsString(PostParameters);
	Request->ProcessRequest();
	*/
}

void AOWSChatManager::OnSendChatToChannelResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{

}

void AOWSChatManager::SendPrivateChatMessage(FString SentFromCharacterName, FString SendToCharacterName, FString Message)
{
	/*
	FString PlayerName = SentFromCharacterName;
	PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

	FString FullMessageToSend = SentFromCharacterName + FString(TEXT(": ")) + Message;

	TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
	Request->OnProcessRequestComplete().BindUObject(this, &AOWSChatManager::OnSendPrivateChatMessageResponseReceived);
	//This is the url on which to process the request
	FString url = FString(TEXT("http://" +  + "/Chat/AddPrivateChatMessage"));

	FString PostParameters = FString(TEXT("id=")) + PlayerName
		+ FString(TEXT("Message=")) + FullMessageToSend
		+ FString(TEXT("&ToCharName=")) + SendToCharacterName
		+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

	Request->SetURL(url);
	Request->SetVerb("POST");
	Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
	Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
	Request->SetContentAsString(PostParameters);
	Request->ProcessRequest();
	*/
}

void AOWSChatManager::OnSendPrivateChatMessageResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{

}


void AOWSChatManager::GetNewChatMessages(int32 LastChatMessageReceived)
{
	/*
	TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
	Request->OnProcessRequestComplete().BindUObject(this, &AOWSChatManager::OnGetNewChatMessagesResponseReceived);
	//This is the url on which to process the request
	FString url = FString(TEXT("http://" +  + "/Chat/GetNewChatMessages/"));

	FString PostParameters = FString(TEXT("LastMessageReceived=")) + FString::FromInt(LastChatMessageReceived)
		+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

	Request->SetURL(url);
	Request->SetVerb("POST");
	Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
	Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
	Request->SetContentAsString(PostParameters);
	//UE_LOG(LogTemp, Error, TEXT("Sending: %s"), *PostParameters);
	Request->ProcessRequest();
	*/
}

void AOWSChatManager::OnGetNewChatMessagesResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	/*
	//UE_LOG(LogTemp, Error, TEXT("OnGetNewChatMessagesResponseReceived"));
	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			if (JsonObject->GetStringField("success") == "true")
			{
				TArray<TSharedPtr<FJsonValue>> Rows = JsonObject->GetArrayField("rows");

				TArray<FChatMessage> ChatMessages;

				int32 MaxMessageID = 0;

				MaxMessageID = JsonObject->GetIntegerField("maxchatmessageid");
			
				for (int RowNum = 0; RowNum != Rows.Num(); RowNum++) {
					FChatMessage tempChatMessage;
					TSharedPtr<FJsonObject> tempRow = Rows[RowNum]->AsObject();
					tempChatMessage.ChatMessage = tempRow->GetStringField("ChatMessage");
					tempChatMessage.ChatMessageID = tempRow->GetIntegerField("ChatMessageID");
					tempChatMessage.SentByCharID = tempRow->GetIntegerField("SentByCharID");
					tempChatMessage.SentByCharName = tempRow->GetStringField("SentByCharName");
					tempChatMessage.SentToCharID = tempRow->GetIntegerField("SentToCharID");
					tempChatMessage.SentToCharName = tempRow->GetStringField("SentToCharName");
					tempChatMessage.ChatGroupID = tempRow->GetIntegerField("ChatGroupID");
					tempChatMessage.ChatGroupName = tempRow->GetStringField("ChatGroupName");

					ChatMessages.Add(tempChatMessage);
				}

				NotifyGetNewChatMessages(ChatMessages, MaxMessageID);
			}
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnLoginResponseReceived Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnLoginResponseReceived Error accessing login server!"));
	}
	*/
}


void AOWSChatManager::AddOrJoinChatGroup(FString CharacterNameToAdd, FString ChatGroupName)
{
	/*
	FString PlayerName = CharacterNameToAdd;
	PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

	TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
	Request->OnProcessRequestComplete().BindUObject(this, &AOWSChatManager::OnAddOrJoinChatGroupResponseReceived);
	//This is the url on which to process the request
	FString url = FString(TEXT("http://" +  + "/Chat/AddOrJoinChatGroup"));

	FString PostParameters = FString(TEXT("id=")) + PlayerName
		+ FString(TEXT("&ChatGroupName=")) + ChatGroupName
		+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

	Request->SetURL(url);
	Request->SetVerb("POST");
	Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
	Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
	Request->SetContentAsString(PostParameters);
	Request->ProcessRequest();
	*/
}

void AOWSChatManager::OnAddOrJoinChatGroupResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	/*
	if (bWasSuccessful)
	{

	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnAddOrJoinChatGroupResponseReceived Error accessing login server!"));
	}
	*/
}


void AOWSChatManager::LeaveChatGroup(FString CharacterNameToRemove, FString ChatGroupName)
{
	/*
	FString PlayerName = CharacterNameToRemove;
	PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

	TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
	Request->OnProcessRequestComplete().BindUObject(this, &AOWSChatManager::OnLeaveChatGroupResponseReceived);
	//This is the url on which to process the request
	FString url = FString(TEXT("http://" +  + "/Chat/LeaveChatGroup"));

	FString PostParameters = FString(TEXT("id=")) + PlayerName
		+ FString(TEXT("&ChatGroupName=")) + ChatGroupName
		+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

	Request->SetURL(url);
	Request->SetVerb("POST");
	Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
	Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
	Request->SetContentAsString(PostParameters);
	Request->ProcessRequest();
	*/
}

void AOWSChatManager::OnLeaveChatGroupResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	/*
	if (bWasSuccessful)
	{

	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnLeaveChatGroupResponseReceived Error accessing login server!"));
	}
	*/
}



