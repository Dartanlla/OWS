// Fill out your copyright notice in the Description page of Project Settings.

#include "OWSGameInstance.h"
#include "OWSPlugin.h"
#include "Runtime/MoviePlayer/Public/MoviePlayer.h"
#include "Runtime/CoreUObject/Public/UObject/UObjectGlobals.h"
#include "Runtime/Engine/Classes/Engine/StaticMesh.h"
#include "Runtime/Engine/Classes/Components/StaticMeshComponent.h"
#include "Runtime/Engine/Classes/Components/SkeletalMeshComponent.h"
#include "OWSPlayerController.h"

void UOWSGameInstance::Init()
{
	UGameInstance::Init();

	Http = &FHttpModule::Get();

	FCoreUObjectDelegates::PreLoadMap.AddUObject(this, &UOWSGameInstance::BeginLoadingScreen);
	FCoreUObjectDelegates::PostLoadMapWithWorld.AddUObject(this, &UOWSGameInstance::EndLoadingScreen);
}

void UOWSGameInstance::BeginLoadingScreen(const FString& MapName)
{
	if (!IsRunningDedicatedServer())
	{
		RPGBeginLoadingScreen();

		/*FLoadingScreenAttributes LoadingScreen;
		LoadingScreen.bAutoCompleteWhenLoadingCompletes = false;
		LoadingScreen.WidgetLoadingScreen = FLoadingScreenAttributes::NewTestLoadingScreenWidget();

		GetMoviePlayer()->SetupLoadingScreen(LoadingScreen);*/
	}
}



void UOWSGameInstance::EndLoadingScreen(UWorld* world)
{
	if (!IsRunningDedicatedServer())
	{
		RPGEndLoadingScreen();
	}
}

void UOWSGameInstance::HideLoadingScreen()
{
	if (!IsRunningDedicatedServer())
	{
		HideLoadingDialog();
	}
}

TSubclassOf<class AActor> UOWSGameInstance::LoadWeaponActorClassFromPath(FString WeaponActorClassPath)
{
	auto cls = StaticLoadObject(UObject::StaticClass(), nullptr, *WeaponActorClassPath);

	if (cls)
	{
		UBlueprint * bp = Cast<UBlueprint>(cls);
		if (bp)
		{
			return (UClass*)bp->GeneratedClass;
		}
	}

	return NULL;
}

UStaticMesh* UOWSGameInstance::LoadStaticMeshFromPath(FString StaticMeshPath)
{
	UStaticMesh* tempStaticMesh;
	tempStaticMesh = LoadObject<UStaticMesh>(NULL, *StaticMeshPath, NULL, LOAD_None, NULL);

	if (!tempStaticMesh)
	{
		UE_LOG(OWS, Error, TEXT("LoadStaticMeshFromPath - Error loading Static Mesh: %s"), *StaticMeshPath);
		return NULL;
	}

	return tempStaticMesh;
}

USkeletalMesh* UOWSGameInstance::LoadSkeletalMeshFromPath(FString SkeletalMeshPath)
{
	USkeletalMesh* tempSkeletalMesh;
	tempSkeletalMesh = LoadObject<USkeletalMesh>(NULL, *SkeletalMeshPath, NULL, LOAD_None, NULL);

	if (!tempSkeletalMesh)
	{
		UE_LOG(OWS, Error, TEXT("LoadSkeletalMeshFromPath - Error loading Skeletal Mesh: %s"), *SkeletalMeshPath);
		return NULL;
	}

	return tempSkeletalMesh;
}

USkeleton* UOWSGameInstance::LoadSkeletonFromPath(FString SkeletonPath)
{
	USkeleton* tempSkeleton;
	tempSkeleton = LoadObject<USkeleton>(NULL, *SkeletonPath, NULL, LOAD_None, NULL);

	if (!tempSkeleton)
	{
		UE_LOG(OWS, Error, TEXT("LoadSkeletonFromPath - Error loading Skeleton: %s"), *SkeletonPath);
		return NULL;
	}

	return tempSkeleton;
}

UMaterialInstance* UOWSGameInstance::LoadMaterialInstanceFromPath(FString MaterialInstancePath)
{
	UMaterialInstance* tempMaterial;
	tempMaterial = LoadObject<UMaterialInstance>(NULL, *MaterialInstancePath, NULL, LOAD_None, NULL);

	if (!tempMaterial)
	{
		UE_LOG(OWS, Error, TEXT("LoadMaterialInstanceFromPath - Error loading Material Instance: %s"), *MaterialInstancePath);
		return NULL;
	}

	return tempMaterial;
}

FString UOWSGameInstance::EncryptWithAES(FString StringToEncrypt, FString Key)
{
	if (StringToEncrypt.IsEmpty()) return "";
	if (Key.IsEmpty()) return "";

	FString SplitSymbol = "OWS#@!";
	StringToEncrypt.Append(SplitSymbol);

	Key = FMD5::HashAnsiString(*Key);
	TCHAR* KeyTChar = Key.GetCharArray().GetData();

	uint32 Size = StringToEncrypt.Len();
	Size = Size + (FAES::AESBlockSize - (Size % FAES::AESBlockSize));

	uint8* ByteString = new uint8[Size];

	if (StringToBytes(StringToEncrypt, ByteString, Size)) {

		FAES::EncryptData(ByteString, Size, StringCast<ANSICHAR>((KeyTChar)).Get());
		StringToEncrypt = FString::FromHexBlob(ByteString, Size);

		delete[] ByteString;
		return StringToEncrypt;
	}

	delete[] ByteString;
	return "";
}

FString UOWSGameInstance::DecryptWithAES(FString StringToDecrypt, FString Key)
{
	if (StringToDecrypt.IsEmpty()) return "";
	if (Key.IsEmpty()) return "";

	FString SplitSymbol = "OWS#@!";

	Key = FMD5::HashAnsiString(*Key);
	TCHAR* KeyTChar = Key.GetCharArray().GetData();

	uint32 Size = StringToDecrypt.Len();
	Size = Size + (FAES::AESBlockSize - (Size % FAES::AESBlockSize));

	uint8* ByteString = new uint8[Size];

	if (FString::ToHexBlob(StringToDecrypt, ByteString, Size)) {

		FAES::DecryptData(ByteString, Size, StringCast<ANSICHAR>((KeyTChar)).Get());
		StringToDecrypt = BytesToString(ByteString, Size);

		FString LeftPart;
		FString RightPart;
		StringToDecrypt.Split(SplitSymbol, &LeftPart, &RightPart, ESearchCase::CaseSensitive, ESearchDir::FromStart);
		StringToDecrypt = LeftPart;

		delete[] ByteString;
		return StringToDecrypt;
	}

	delete[] ByteString;
	return "";
}


FString UOWSGameInstance::SerializeStructToJSONString(const UStruct* StructToSerialize)
{
	FString TempOutputString;
	for (TFieldIterator<FProperty> PropIt(StructToSerialize->GetClass()); PropIt; ++PropIt)
	{
		FProperty* Property = *PropIt;
		TempOutputString += Property->GetNameCPP();
	}

	return TempOutputString;
}


UClass* UOWSGameInstance::FindClass(FString ClassName) const
{
	UObject* ClassPackage = ANY_PACKAGE;
	UClass* Result = FindObject<UClass>(ClassPackage, *ClassName);

	return Result;
}

//This only works if the ability is already in memory.  Use LoadGameplayAbilityClass instead
TSubclassOf<UGameplayAbility> UOWSGameInstance::FindGameplayAbilityClass(FString ClassName) const
{
	UObject* ClassPackage = ANY_PACKAGE;
	UClass* Result = FindObject<UClass>(ClassPackage, *ClassName);

	TSubclassOf<UGameplayAbility> GameplayAbilityClass = Result;

	if (GameplayAbilityClass)
		return Result;
	else
		return nullptr;
}


TSubclassOf<UGameplayAbility> UOWSGameInstance::LoadGameplayAbilityClass(FString PathToGameplayAbilityClass) const
{
	return LoadClass<UGameplayAbility>(NULL, *PathToGameplayAbilityClass, NULL, LOAD_None, NULL);
}