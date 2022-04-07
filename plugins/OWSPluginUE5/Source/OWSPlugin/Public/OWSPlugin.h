// Copyright 1998-2018 Epic Games, Inc. All Rights Reserved.

#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"

DECLARE_LOG_CATEGORY_EXTERN(OWS, Log, All);

#define COLLISION_PROJECTILE ECC_GameTraceChannel1
#define COLLISION_TRACE_WEAPON ECC_GameTraceChannel2
#define COLLISION_PROJECTILE_SHOOTABLE ECC_GameTraceChannel3
#define COLLISION_TELEPORTING_OBJECT ECC_GameTraceChannel4
#define COLLISION_PAWNOVERLAP ECC_GameTraceChannel5
#define COLLISION_TRACE_WEAPONNOCHARACTER ECC_GameTraceChannel6

class FOWSPluginModule : public IModuleInterface
{
public:

	/** IModuleInterface implementation */
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;
};