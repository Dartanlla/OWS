// Fill out your copyright notice in the Description page of Project Settings.

#include "OWSGameInstance.h"
#include "OWSPlugin.h"


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
		//RPGBeginLoadingScreen();

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

FString UOWSGameInstance::EncryptWithAES(FString StringToEncrypt, FString Key)
{
	if (StringToEncrypt.IsEmpty()) return "";
	if (Key.IsEmpty()) return "";

	FString SplitSymbol = "OWS#@!";
	StringToEncrypt.Append(SplitSymbol);

	Key = FMD5::HashAnsiString(*Key);
	TCHAR *KeyTChar = Key.GetCharArray().GetData();
	ANSICHAR *KeyAnsi = (ANSICHAR*)TCHAR_TO_ANSI(KeyTChar);

	uint32 Size = StringToEncrypt.Len();
	Size = Size + (FAES::AESBlockSize - (Size % FAES::AESBlockSize));

	uint8* ByteString = new uint8[Size];

	if (StringToBytes(StringToEncrypt, ByteString, Size)) {

		FAES::EncryptData(ByteString, Size, KeyAnsi);
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
	TCHAR *KeyTChar = Key.GetCharArray().GetData();
	ANSICHAR *KeyAnsi = (ANSICHAR*)TCHAR_TO_ANSI(KeyTChar);

	uint32 Size = StringToDecrypt.Len();
	Size = Size + (FAES::AESBlockSize - (Size % FAES::AESBlockSize));

	uint8* ByteString = new uint8[Size];

	if (FString::ToHexBlob(StringToDecrypt, ByteString, Size)) {

		FAES::DecryptData(ByteString, Size, KeyAnsi);
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


