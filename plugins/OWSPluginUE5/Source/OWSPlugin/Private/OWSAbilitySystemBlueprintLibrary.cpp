// Copyright 2018 Sabre Dart Studios

#include "OWSAbilitySystemBlueprintLibrary.h"



FGameplayAbilityTargetDataHandle UOWSAbilitySystemBlueprintLibrary::FilterTargetData(const FGameplayAbilityTargetDataHandle& TargetDataHandle, FOWSGameplayTargetDataFilterHandle FilterHandle)
{
	FGameplayAbilityTargetDataHandle ReturnDataHandle;

	for (int32 i = 0; TargetDataHandle.IsValid(i); ++i)
	{
		const FGameplayAbilityTargetData* UnfilteredData = TargetDataHandle.Get(i);
		check(UnfilteredData);
		const TArray<TWeakObjectPtr<AActor>> UnfilteredActors = UnfilteredData->GetActors();
		if (UnfilteredActors.Num() > 0)
		{
			TArray<TWeakObjectPtr<AActor>> FilteredActors = UnfilteredActors.FilterByPredicate(FilterHandle);
			if (FilteredActors.Num() > 0)
			{
				//Copy the data first, since we don't understand the internals of it
				const UScriptStruct* ScriptStruct = UnfilteredData->GetScriptStruct();
				FGameplayAbilityTargetData* NewData = (FGameplayAbilityTargetData*)FMemory::Malloc(ScriptStruct->GetCppStructOps()->GetSize());
				ScriptStruct->InitializeStruct(NewData);
				ScriptStruct->CopyScriptStruct(NewData, UnfilteredData);
				ReturnDataHandle.Data.Add(TSharedPtr<FGameplayAbilityTargetData>(NewData));
				if (FilteredActors.Num() < UnfilteredActors.Num())
				{
					//We have lost some, but not all, of our actors, so replace the array. This should only be possible with targeting types that permit actor-array setting.
					if (!NewData->SetActors(FilteredActors))
					{
						//This is an error, though we could ignore it. We somehow filtered out part of a list, but the class doesn't support changing the list, so now it's all or nothing.
						check(false);
					}
				}
			}
		}
	}

	return ReturnDataHandle;
}

FOWSGameplayTargetDataFilterHandle UOWSAbilitySystemBlueprintLibrary::MakeFilterHandle(FOWSGameplayTargetDataFilter Filter, AActor* FilterActor)
{
	FOWSGameplayTargetDataFilterHandle FilterHandle;
	FOWSGameplayTargetDataFilter* NewFilter = new FOWSGameplayTargetDataFilter(Filter);
	NewFilter->InitializeFilterContext(FilterActor);
	FilterHandle.Filter = TSharedPtr<FOWSGameplayTargetDataFilter>(NewFilter);
	return FilterHandle;
}


float UOWSAbilitySystemBlueprintLibrary::GetOWSChargeTimeFromTargetData(const FGameplayAbilityTargetDataHandle& TargetData, int32 Index)
{
	float OutputValue = 0.f;

	/*if (TargetData.Data.IsValidIndex(Index))
	{
		FGameplayAbilityTargetData* Data = TargetData.Data[Index].Get();
		FGameplayAbilityCastingTargetingLocationInfo* MyData = dynamic_cast<FGameplayAbilityCastingTargetingLocationInfo*>(Data);
		TArray<AActor*>	ResolvedArray;
		if (MyData)
		{
			OutputValue = MyData->ChargeTime;
		}
		return OutputValue;
	}*/
	return OutputValue;
}


bool FGameplayAbilityCastingTargetingLocationInfo::NetSerialize(FArchive& Ar, class UPackageMap* Map, bool& bOutSuccess)
{
	SourceLocation.NetSerialize(Ar, Map, bOutSuccess);
	TargetLocation.NetSerialize(Ar, Map, bOutSuccess);

	Ar << ChargeTime;
	Ar << UniqueID;

	bOutSuccess = true;
	return true;
}