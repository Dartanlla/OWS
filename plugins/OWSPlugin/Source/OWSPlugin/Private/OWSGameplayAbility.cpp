// Copyright 2018 Sabre Dart Studios


#include "OWSGameplayAbility.h"

AOWSCharacterWithAbilities* UOWSGameplayAbility::GetOWSAvatarActor()
{
	AOWSCharacterWithAbilities* OWSCharacterWithAbilities = Cast<AOWSCharacterWithAbilities>(GetAvatarActorFromActorInfo());

	if (!ensure(OWSCharacterWithAbilities))
	{
		K2_EndAbility();
		return nullptr;
	}

	return OWSCharacterWithAbilities;
}

AOWSPlayerController* UOWSGameplayAbility::GetOWSPlayerController()
{
	AOWSCharacterWithAbilities* OWSCharacterWithAbilities = Cast<AOWSCharacterWithAbilities>(GetAvatarActorFromActorInfo());

	if (!ensure(OWSCharacterWithAbilities))
	{
		K2_EndAbility();
		return nullptr;
	}

	AOWSPlayerController* OWSPlayerController = Cast<AOWSPlayerController>(OWSCharacterWithAbilities->GetController());

	if (!ensure(OWSPlayerController))
	{
		K2_EndAbility();
		return nullptr;
	}

	return OWSPlayerController;
}

FGameplayAbilityTargetingLocationInfo UOWSGameplayAbility::MakeTargetLocationInfoFromCamera(const UCameraComponent* FollowCamera)
{
	FGameplayAbilityTargetingLocationInfo ReturnLocation;

	ReturnLocation.LocationType = EGameplayAbilityTargetingLocationType::LiteralTransform;
	ReturnLocation.SourceAbility = this;

	if (FollowCamera)
	{
		ReturnLocation.LiteralTransform.SetLocation(FollowCamera->GetComponentLocation());
		ReturnLocation.LiteralTransform.SetRotation(FollowCamera->GetComponentRotation().Quaternion());
	}

	return ReturnLocation;
}


void UOWSGameplayAbility::OnlyEndAbilityOnServer()
{
	if (GetCurrentActorInfo()->IsNetAuthority())
	{
		K2_EndAbility();
	}
}