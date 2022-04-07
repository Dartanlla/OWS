// Copyright 2018 Sabre Dart Studios

#include "OWSAIController.h"
#include "Navigation/CrowdFollowingComponent.h"

AOWSAIController::AOWSAIController(const FObjectInitializer& ObjectInitializer)
	: Super(ObjectInitializer.SetDefaultSubobjectClass<UCrowdFollowingComponent>(TEXT("PathFollowingComponent")))
{

}

FGenericTeamId AOWSAIController::GetGenericTeamId() const
{
	return FGenericTeamId(_TeamNumber);
}

void AOWSAIController::SetTeamNumber(const int32 TeamNumber)
{
	_TeamNumber = TeamNumber;
	SetGenericTeamId(FGenericTeamId(TeamNumber));
}