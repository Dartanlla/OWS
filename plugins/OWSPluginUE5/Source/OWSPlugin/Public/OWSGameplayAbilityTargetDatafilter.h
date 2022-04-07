#pragma once

#include "Abilities/GameplayAbilityTargetDataFilter.h"
#include "OWSCharacterWithAbilities.h"
#include "OWSGameplayAbilityTargetDataFilter.generated.h"

USTRUCT(BlueprintType)
struct OWSPLUGIN_API FOWSGameplayTargetDataFilter : public FGameplayTargetDataFilter
{
	GENERATED_USTRUCT_BODY()

public:
	FOWSGameplayTargetDataFilter() {
		TeamNumber = 0;
	}

	/** Returns true if the actor passes the filter and will be targeted */
	virtual bool FilterPassesForActor(const AActor* ActorToBeFiltered) const
	{
		switch (SelfFilter.GetValue())
		{
		case ETargetDataFilterSelf::Type::TDFS_NoOthers:
			if (ActorToBeFiltered != SelfActor)
			{
				return (bReverseFilter ^ false);
			}
			break;
		case ETargetDataFilterSelf::Type::TDFS_NoSelf:
			if (ActorToBeFiltered == SelfActor)
			{
				return (bReverseFilter ^ false);
			}
			break;
		case ETargetDataFilterSelf::Type::TDFS_Any:
		default:
			break;
		}

		if (RequiredActorClass && !ActorToBeFiltered->IsA(RequiredActorClass))
		{
			return (bReverseFilter ^ false);
		}

		if (TeamNumber > 0)
		{
			const AOWSCharacter* OWSCharacter = Cast<AOWSCharacter>(ActorToBeFiltered);

			if (OWSCharacter)
			{
				if (OWSCharacter->TeamNumber == TeamNumber)
				{
					return (bReverseFilter ^ false);
				}
			}
		}

		return (bReverseFilter ^ true);
	}

	/** Filter based on whether or not this actor is "self." */
	UPROPERTY(BlueprintReadWrite, EditAnywhere, meta = (ExposeOnSpawn = true), Category = Filter)
		int TeamNumber;

};


/** Polymorphic handle to filter structure that handles checking if actors should be targeted */
USTRUCT(BlueprintType)
struct OWSPLUGIN_API FOWSGameplayTargetDataFilterHandle
{
	GENERATED_USTRUCT_BODY()

		TSharedPtr<FOWSGameplayTargetDataFilter> Filter;

	/** Returns true if the actor passes the filter and will be targeted */
	bool FilterPassesForActor(const AActor* ActorToBeFiltered) const
	{
		if (!ActorToBeFiltered)
		{
			// If no filter is set, then always return true even if there is no actor.
			// If there is no actor and there is a filter, then always fail.
			return (Filter.IsValid() == false);
		}
		//Eventually, this might iterate through multiple filters. We'll need to decide how to designate OR versus AND functionality.
		if (Filter.IsValid())
		{
			if (!Filter.Get()->FilterPassesForActor(ActorToBeFiltered))
			{
				return false;
			}
		}
		return true;
	}

	bool FilterPassesForActor(const TWeakObjectPtr<AActor> ActorToBeFiltered) const
	{
		return FilterPassesForActor(ActorToBeFiltered.Get());
	}

	bool operator()(const TWeakObjectPtr<AActor> ActorToBeFiltered) const
	{
		return FilterPassesForActor(ActorToBeFiltered.Get());
	}

	bool operator()(const AActor* ActorToBeFiltered) const
	{
		return FilterPassesForActor(ActorToBeFiltered);
	}
};
