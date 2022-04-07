#include "OWSReplicationGraph.h"
#include "Net/UnrealNetwork.h"
#include "Engine/LevelStreaming.h"
#include "EngineUtils.h"
#include "CoreGlobals.h"
#include "UObject/UObjectIterator.h"
#include "Engine/NetConnection.h"

UOWSReplicationGraph::UOWSReplicationGraph()
{

}

void UOWSReplicationGraph::InitGlobalActorClassSettings()
{
	Super::InitGlobalActorClassSettings();

	// ReplicationGraph stores internal associative data for actor classes. 
	// We build this data here based on actor CDO values.
	for (TObjectIterator<UClass> It; It; ++It)
	{
		UClass* Class = *It;
		AActor* ActorCDO = Cast<AActor>(Class->GetDefaultObject());
		if (!ActorCDO || !ActorCDO->GetIsReplicated())
		{
			continue;
		}

		// Skip SKEL and REINST classes.
		if (Class->GetName().StartsWith(TEXT("SKEL_")) || Class->GetName().StartsWith(TEXT("REINST_")))
		{
			continue;
		}

		FClassReplicationInfo ClassInfo;

		// Replication Graph is frame based. Convert NetUpdateFrequency to ReplicationPeriodFrame based on Server MaxTickRate.
		ClassInfo.ReplicationPeriodFrame = FMath::Max<uint32>((uint32)FMath::RoundToFloat(NetDriver->NetServerMaxTickRate / ActorCDO->NetUpdateFrequency), 1);

		if (ActorCDO->bAlwaysRelevant || ActorCDO->bOnlyRelevantToOwner)
		{
			ClassInfo.SetCullDistanceSquared(0.f);
		}
		else
		{
			ClassInfo.SetCullDistanceSquared(ActorCDO->NetCullDistanceSquared);
		}

		TArray<UClass*> ExplicitlySetClasses;
		auto SetClassInfo = [&](UClass* Class, const FClassReplicationInfo& Info) { GlobalActorReplicationInfoMap.SetClassInfo(Class, Info); ExplicitlySetClasses.Add(Class); };

		FClassReplicationInfo PlayerStateRepInfo;
		PlayerStateRepInfo.DistancePriorityScale = 0.f;
		PlayerStateRepInfo.ActorChannelFrameTimeout = 0;
		SetClassInfo(APlayerState::StaticClass(), PlayerStateRepInfo);

		if (Class != APlayerState::StaticClass())
		{
			GlobalActorReplicationInfoMap.SetClassInfo(Class, ClassInfo);
		}
	}
}

void UOWSReplicationGraph::InitGlobalGraphNodes()
{
	// -----------------------------------------------
	//	Spatial Actors
	// -----------------------------------------------

	GridNode = CreateNewNode<UReplicationGraphNode_GridSpatialization2D>();
	GridNode->CellSize = 10000.f;
	GridNode->SpatialBias = FVector2D(-WORLD_MAX, -WORLD_MAX);

	AddGlobalGraphNode(GridNode);

	// -----------------------------------------------
	//	Always Relevant (to everyone) Actors
	// -----------------------------------------------
	AlwaysRelevantNode = CreateNewNode<UReplicationGraphNode_ActorList>();
	AddGlobalGraphNode(AlwaysRelevantNode);

	// -----------------------------------------------
	//	Player State specialization. This will return a rolling subset of the player states to replicate
	// -----------------------------------------------
	UOWSReplicationGraphNode_PlayerStateFrequencyLimiter* PlayerStateNode = CreateNewNode<UOWSReplicationGraphNode_PlayerStateFrequencyLimiter>();
	AddGlobalGraphNode(PlayerStateNode);
}

void UOWSReplicationGraph::InitConnectionGraphNodes(UNetReplicationGraphConnection* RepGraphConnection)
{
	Super::InitConnectionGraphNodes(RepGraphConnection);

	UReplicationGraphNode_AlwaysRelevant_ForConnection* AlwaysRelevantNodeForConnection = CreateNewNode<UReplicationGraphNode_AlwaysRelevant_ForConnection>();
	AddConnectionGraphNode(AlwaysRelevantNodeForConnection, RepGraphConnection);

	AlwaysRelevantForConnectionList.Emplace(RepGraphConnection->NetConnection, AlwaysRelevantNodeForConnection);
}

void UOWSReplicationGraph::RouteAddNetworkActorToNodes(const FNewReplicatedActorInfo& ActorInfo, FGlobalActorReplicationInfo& GlobalInfo)
{
	if (ActorInfo.Actor->bAlwaysRelevant)
	{
		AlwaysRelevantNode->NotifyAddNetworkActor(ActorInfo);
	}
	else if (ActorInfo.Actor->bOnlyRelevantToOwner)
	{
		ActorsWithoutNetConnection.Add(ActorInfo.Actor);
	}
	else
	{
		// Note that UReplicationGraphNode_GridSpatialization2D has 3 methods for adding actor based on the mobility of the actor. Since AActor lacks this information, we will
		// add all spatialized actors as dormant actors: meaning they will be treated as possibly dynamic (moving) when not dormant, and as static (not moving) when dormant.
		GridNode->AddActor_Dormancy(ActorInfo, GlobalInfo);
	}
}

void UOWSReplicationGraph::RouteRemoveNetworkActorToNodes(const FNewReplicatedActorInfo& ActorInfo)
{
	if (ActorInfo.Actor->bAlwaysRelevant)
	{
		AlwaysRelevantNode->NotifyRemoveNetworkActor(ActorInfo);
	}
	else if (ActorInfo.Actor->bOnlyRelevantToOwner)
	{
		if (ActorInfo.Actor->GetNetConnection())
		{
			if (UReplicationGraphNode* Node = GetAlwaysRelevantNodeForConnection(ActorInfo.Actor->GetNetConnection()))
			{
				Node->NotifyRemoveNetworkActor(ActorInfo);
			}
		}
	}
	else
	{
		GridNode->RemoveActor_Dormancy(ActorInfo);
	}
}

UReplicationGraphNode_AlwaysRelevant_ForConnection* UOWSReplicationGraph::GetAlwaysRelevantNodeForConnection(UNetConnection* Connection)
{
	UReplicationGraphNode_AlwaysRelevant_ForConnection* Node = nullptr;
	if (Connection)
	{
		if (FOWSConnectionAlwaysRelevantNodePair* Pair = AlwaysRelevantForConnectionList.FindByKey(Connection))
		{
			if (Pair->Node)
			{
				Node = Pair->Node;
			}
			else
			{
				UE_LOG(LogNet, Warning, TEXT("AlwaysRelevantNode for connection %s is null."), *GetNameSafe(Connection));
			}
		}
		else
		{
			UE_LOG(LogNet, Warning, TEXT("Could not find AlwaysRelevantNode for connection %s. This should have been created in UBasicReplicationGraph::InitConnectionGraphNodes."), *GetNameSafe(Connection));
		}
	}
	else
	{
		// Basic implementation requires owner is set on spawn that never changes. A more robust graph would have methods or ways of listening for owner to change
		UE_LOG(LogNet, Warning, TEXT("Actor: %s is bOnlyRelevantToOwner but does not have an owning Netconnection. It will not be replicated"));
	}

	return Node;
}

int32 UOWSReplicationGraph::ServerReplicateActors(float DeltaSeconds)
{
	// Route Actors needing owning net connections to appropriate nodes
	for (int32 idx = ActorsWithoutNetConnection.Num() - 1; idx >= 0; --idx)
	{
		bool bRemove = false;
		if (AActor* Actor = ActorsWithoutNetConnection[idx])
		{
			if (UNetConnection* Connection = Actor->GetNetConnection())
			{
				bRemove = true;
				if (Connection)
				{
					if (UReplicationGraphNode_AlwaysRelevant_ForConnection* Node = GetAlwaysRelevantNodeForConnection(Actor->GetNetConnection()))
					{
						Node->NotifyAddNetworkActor(FNewReplicatedActorInfo(Actor));
					}
				}
			}
		}
		else
		{
			bRemove = true;
		}

		if (bRemove)
		{
			ActorsWithoutNetConnection.RemoveAtSwap(idx, 1, false);
		}
	}


	return Super::ServerReplicateActors(DeltaSeconds);
}

UOWSReplicationGraphNode_AlwaysRelevantToParty* UOWSReplicationGraph::GetNodeForParty(int32 PartyID)
{
	UOWSReplicationGraphNode_AlwaysRelevantToParty* PartyNode = PartyMap.FindRef(PartyID);
	if (PartyNode == nullptr)
	{
		PartyNode = CreateNewNode<UOWSReplicationGraphNode_AlwaysRelevantToParty>();
		PartyNode->PartyID = PartyID;
		PartyMap.Add(PartyID, PartyNode);
	}

	return PartyNode;
}

#if WITH_EDITOR
#define CHECK_WORLDS(X) if (X->GetWorld() != GetWorld()) return;
#else
#define CHECK_WORLDS(X)
#endif

void UOWSReplicationGraph::AddPlayerToParty(AOWSPlayerState* PS)
{	
	if (PS)
	{
		CHECK_WORLDS(PS)
		if (UNetConnection* NetConnection = PS->GetNetConnection())
		{
			UOWSReplicationGraphNode_AlwaysRelevantToParty* PartyNode = GetNodeForParty(PS->AlwaysRelevantPartyID);
			PartyNode->PlayerStates.AddUnique(PS);
			AddConnectionGraphNode(PartyNode, NetConnection);
		}
	}
}



UOWSReplicationGraphNode_PlayerStateFrequencyLimiter::UOWSReplicationGraphNode_PlayerStateFrequencyLimiter()
{
	bRequiresPrepareForReplicationCall = true;
}

void UOWSReplicationGraphNode_PlayerStateFrequencyLimiter::PrepareForReplication()
{
	QUICK_SCOPE_CYCLE_COUNTER(UOWSReplicationGraphNode_PlayerStateFrequencyLimiter_GlobalPrepareForReplication);

	ReplicationActorLists.Reset();
	ForceNetUpdateReplicationActorList.Reset();

	ReplicationActorLists.AddDefaulted();
	FActorRepListRefView* CurrentList = &ReplicationActorLists[0];

	// We rebuild our lists of player states each frame. This is not as efficient as it could be but its the simplest way
	// to handle players disconnecting and keeping the lists compact. If the lists were persistent we would need to defrag them as players left.

	for (TActorIterator<APlayerState> It(GetWorld()); It; ++It)
	{
		APlayerState* PS = *It;
		if (IsActorValidForReplicationGather(PS) == false)
		{
			continue;
		}

		if (CurrentList->Num() >= TargetActorsPerFrame)
		{
			ReplicationActorLists.AddDefaulted();
			CurrentList = &ReplicationActorLists.Last();
		}

		CurrentList->Add(PS);
	}
}

void UOWSReplicationGraphNode_PlayerStateFrequencyLimiter::GatherActorListsForConnection(const FConnectionGatherActorListParameters& Params)
{
	const int32 ListIdx = Params.ReplicationFrameNum % ReplicationActorLists.Num();
	Params.OutGatheredReplicationLists.AddReplicationActorList(ReplicationActorLists[ListIdx]);

	if (ForceNetUpdateReplicationActorList.Num() > 0)
	{
		Params.OutGatheredReplicationLists.AddReplicationActorList(ForceNetUpdateReplicationActorList);
	}
}

void UOWSReplicationGraphNode_PlayerStateFrequencyLimiter::LogNode(FReplicationGraphDebugInfo& DebugInfo, const FString& NodeName) const
{
	DebugInfo.Log(NodeName);
	DebugInfo.PushIndent();

	int32 i = 0;
	for (const FActorRepListRefView& List : ReplicationActorLists)
	{
		LogActorRepList(DebugInfo, FString::Printf(TEXT("Bucket[%d]"), i++), List);
	}

	DebugInfo.PopIndent();
}



UOWSReplicationGraphNode_AlwaysRelevantToParty::UOWSReplicationGraphNode_AlwaysRelevantToParty()
{
	bRequiresPrepareForReplicationCall = true;
}

void UOWSReplicationGraphNode_AlwaysRelevantToParty::PrepareForReplication()
{
	if (PlayerStates.Num() > 1)
	{
		const int32 PreviousNum = ReplicationActorList.Num();

		ReplicationActorList.Reset();
		for (AOWSPlayerState* PS : PlayerStates)
		{
			if (PS)
			{
				if (AOWSCharacter* Pawn = PS->GetCurrentPawn())
				{
					ReplicationActorList.ConditionalAdd(Pawn);
				}
			}
		}

		UpdatePerConnectionCullDistance = PreviousNum != ReplicationActorList.Num();
	}
}

void UOWSReplicationGraphNode_AlwaysRelevantToParty::GatherActorListsForConnection(const FConnectionGatherActorListParameters& Params)
{
	Params.OutGatheredReplicationLists.AddReplicationActorList(ReplicationActorList);

	if (UpdatePerConnectionCullDistance)
	{
		for (FActorRepListType Actor : ReplicationActorList)
		{
			FConnectionReplicationActorInfo& ConnectionActorInfo = Params.ConnectionManager.ActorInfoMap.FindOrAdd( Actor );
			ConnectionActorInfo.SetCullDistanceSquared(0.f);
		}
	}
}

void UOWSReplicationGraphNode_AlwaysRelevantToParty::LogNode(FReplicationGraphDebugInfo& DebugInfo, const FString& NodeName) const
{
	DebugInfo.Log(NodeName);
	DebugInfo.PushIndent();
	LogActorRepList(DebugInfo, FString::Printf(TEXT("Party: %u"), PartyID), ReplicationActorList);
	DebugInfo.PopIndent();
}