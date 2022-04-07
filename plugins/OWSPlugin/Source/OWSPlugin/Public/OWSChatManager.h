// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "GameFramework/Actor.h"
#include "Runtime/Online/HTTP/Public/Http.h"
#include "OWSChatManager.generated.h"

USTRUCT(BlueprintType, Blueprintable)
struct FChatMessage
{
	GENERATED_USTRUCT_BODY()

public:
	FChatMessage() {
		ChatMessage = "";
		ChatMessageID = 0;
		SentByCharID = 0;
		SentByCharName = "";
		SentToCharID = 0;
		SentToCharName = "";
		ChatGroupID = 0;
		ChatGroupName = "";
	}

	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Chat")
		FString ChatMessage;
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Chat")
		int32 ChatMessageID;
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Chat")
		int32 SentByCharID;
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Chat")
		FString SentByCharName;
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Chat")
		int32 SentToCharID;	
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Chat")
		FString SentToCharName;
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Chat")
		int32 ChatGroupID;
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Chat")
		FString ChatGroupName;
};

UCLASS()
class OWSPLUGIN_API AOWSChatManager : public AActor
{
	GENERATED_BODY()

	FHttpModule* Http;

public:	
	// Sets default values for this actor's properties
	AOWSChatManager();

	// Called when the game starts or when spawned
	virtual void BeginPlay() override;
	
	// Called every frame
	virtual void Tick( float DeltaSeconds ) override;

	//Send Global Chat
	UFUNCTION(BlueprintCallable, Category = "Chat")
		void SendGlobalChat(FString SentFromCharacterName, FString Message);

	void OnSendGlobalChatResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	//Send Chat to Channel
	UFUNCTION(BlueprintCallable, Category = "Chat")
		void SendChatToChannel(FString SentFromCharacterName, FString Message, FString ChatChannelName);

	void OnSendChatToChannelResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	//Send Private Message
	UFUNCTION(BlueprintCallable, Category = "Chat")
		void SendPrivateChatMessage(FString SentFromCharacterName, FString SendToCharacterName, FString Message);

	void OnSendPrivateChatMessageResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);	

	//Get new Chat Messages
	UFUNCTION(BlueprintCallable, Category = "Chat")
		void GetNewChatMessages(int32 LastChatMessageReceived);

	void OnGetNewChatMessagesResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	UFUNCTION(BlueprintImplementableEvent, Category = "Chat")
		void NotifyGetNewChatMessages(const TArray<FChatMessage> &NewChatMessages, const int32 MaxMessageID);
	UFUNCTION(BlueprintImplementableEvent, Category = "Chat")
		void ErrorGetNewChatMessages(const FString &ErrorMsg);

	//Add to Chat Group
	UFUNCTION(BlueprintCallable, Category = "Chat")
		void AddOrJoinChatGroup(FString CharacterNameToAdd, FString ChatGroupName);

	void OnAddOrJoinChatGroupResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	//Leave Chat Group
	UFUNCTION(BlueprintCallable, Category = "Chat")
		void LeaveChatGroup(FString CharacterNameToRemove, FString ChatGroupName);

	void OnLeaveChatGroupResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);
	
};
