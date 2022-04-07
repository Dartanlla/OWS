// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "Engine/GameInstance.h"
//#include "Misc/AES.h"
//#include "Misc/Base64.h"
#include "Runtime/Core/Public/Misc/AES.h"
#include "Runtime/Core/Public/Misc/Base64.h"
#include "Runtime/Core/Public/Misc/SecureHash.h"
#include "Runtime/Online/HTTP/Public/HttpModule.h"
#include "Abilities/GameplayAbility.h"
#include "OWSGameInstance.generated.h"

/**
 * 
 */
UCLASS()
class OWSPLUGIN_API UOWSGameInstance : public UGameInstance
{
	GENERATED_BODY()
	
public:
	FHttpModule* Http;

	virtual void Init() override;

	UFUNCTION()
		virtual void BeginLoadingScreen(const FString& MapName);
	UFUNCTION()
		virtual void EndLoadingScreen(UWorld* world);
	UFUNCTION(BlueprintCallable, Category = "Init")
		virtual void HideLoadingScreen();
	
	UFUNCTION(BlueprintImplementableEvent, Category = "Init")
		void RPGBeginLoadingScreen();
	UFUNCTION(BlueprintImplementableEvent, Category = "Init")
		void RPGEndLoadingScreen();	
	UFUNCTION(BlueprintImplementableEvent, Category = "Init")
		void HideLoadingDialog();

	UFUNCTION(BlueprintCallable, Category = "JSON")
		FString SerializeStructToJSONString(const UStruct* StructToSerialize);

	UFUNCTION(BlueprintCallable, Category = "Inventory")
		TSubclassOf<class AActor> LoadWeaponActorClassFromPath(FString WeaponActorClassPath);

	UFUNCTION(BlueprintCallable, Category = "Inventory")
		UStaticMesh* LoadStaticMeshFromPath(FString StaticMeshPath);

	UFUNCTION(BlueprintCallable, Category = "Inventory")
		USkeletalMesh* LoadSkeletalMeshFromPath(FString SkeletalMeshPath);

	UFUNCTION(BlueprintCallable, Category = "Skeleton")
		USkeleton* LoadSkeletonFromPath(FString SkeletonPath);

	UFUNCTION(BlueprintCallable, Category = "Material")
		UMaterialInstance* LoadMaterialInstanceFromPath(FString MaterialInstancePath);

	UFUNCTION(BlueprintCallable, Category = "Classes")
		UClass* FindClass(FString ClassName) const;

	UFUNCTION(BlueprintCallable, Category = "Gameplay Abilities")
		TSubclassOf<UGameplayAbility> FindGameplayAbilityClass(FString ClassName) const;
	UFUNCTION(BlueprintCallable, Category = "Gameplay Abilities")
		TSubclassOf<UGameplayAbility> LoadGameplayAbilityClass(FString PathToGameplayAbilityClass) const;

	UFUNCTION(BlueprintCallable, Category = "Encryption")
		static FString EncryptWithAES(FString StringtoEncrypt, FString Key);
	UFUNCTION(BlueprintCallable, Category = "Encryption")
		static FString DecryptWithAES(FString StringToDecrypt, FString Key);

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Sessions")
		FString UserSessionGUID;

	UPROPERTY()
		TMap<FString, int32> LocalMeshItemsMap;
};
