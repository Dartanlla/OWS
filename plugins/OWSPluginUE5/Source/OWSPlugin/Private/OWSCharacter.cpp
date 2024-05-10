// Fill out your copyright notice in the Description page of Project Settings.

#include "OWSCharacter.h"
#include "OWSPlugin.h"
#include "Net/UnrealNetwork.h"
#include "Runtime/Online/HTTP/Public/Http.h"
#include "Runtime/JsonUtilities/Public/JsonObjectConverter.h"
#include "Runtime/Engine/Classes/Engine/Texture2D.h"
#include "Runtime/Core/Public/Misc/Guid.h"
#include "Runtime/Core/Public/Misc/ConfigCacheIni.h"
#include "OWSPlayerController.h"
#include "Runtime/Engine/Classes/GameFramework/PlayerState.h"

// Sets default values
AOWSCharacter::AOWSCharacter(const class FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer)
{
 	// Set this character to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

	IsTransferringBetweenMaps = false;

	GConfig->GetString(
		TEXT("/Script/EngineSettings.GeneralProjectSettings"),
		TEXT("OWSAPICustomerKey"),
		OWSAPICustomerKey,
		GGameIni
	);

	Http = &FHttpModule::Get();

	TeamId = FGenericTeamId(TeamNumber);

	bShouldAutoLoadCustomCharacterStats = false;

	//AlwaysRelevantPartyID = 0;
}

// Called when the game starts or when spawned
void AOWSCharacter::BeginPlay()
{
	Super::BeginPlay();
}

void AOWSCharacter::PossessedBy(AController* NewController)
{
	Super::PossessedBy(NewController);

	if (GetLocalRole() == ROLE_Authority)
	{
		if (!IsAMob)
		{
			LoadCharacterStats();
		}
	}
}

// Called every frame
void AOWSCharacter::Tick( float DeltaTime )
{
	Super::Tick( DeltaTime );

}

bool AOWSCharacter::CanJumpInternal_Implementation() const
{
	const bool bCanHoldToJumpHigher = (GetJumpMaxHoldTime() > 0.0f) && IsJumpProvidingForce();

	bool CanJumpFromClimbing = Cast<UOWSCharacterMovementComponent>(GetCharacterMovement())->bIsClimbing;

	return !bIsCrouched && GetCharacterMovement() && (GetCharacterMovement()->IsMovingOnGround() || bCanHoldToJumpHigher || CanJumpFromClimbing) && GetCharacterMovement()->IsJumpAllowed() && !GetCharacterMovement()->bWantsToCrouch;
}

bool AOWSCharacter::IsCharacterFacingActor(AActor* ActorToTest, float MaxAngle)
{
	FVector VectorToActor = ActorToTest->GetActorLocation() - GetActorLocation();

	float dotProduct = FVector::DotProduct(GetActorForwardVector(), VectorToActor);

	if (dotProduct < MaxAngle)
	{
		return true;
	}

	return false;
}

bool AOWSCharacter::IsCharacterFacingActorForward(AActor* ActorToTest, float MaxAngle)
{
	float dotProduct = FVector::DotProduct(GetActorForwardVector(), ActorToTest->GetActorForwardVector());

	if (dotProduct >= MaxAngle)
	{
		return true;
	}

	return false;
}

// Called to bind functionality to input
void AOWSCharacter::SetupPlayerInputComponent(UInputComponent* PlayerInputComponent)
{
	Super::SetupPlayerInputComponent(PlayerInputComponent);
}

AOWSPlayerController* AOWSCharacter::GetOWSPlayerController()
{
	return Cast<AOWSPlayerController>(GetController());
}

FGenericTeamId AOWSCharacter::GetGenericTeamId() const
{
	return TeamId;
}

void AOWSCharacter::LoadCharacterStats()
{
	UE_LOG(OWS, Verbose, TEXT("AOWSCharacter: LoadCharacterStats"));
	GetCharacterStatsBase();
	if (bShouldAutoLoadCustomCharacterStats)
	{
		AutoLoadCustomCharacterStats();
	}
}

void AOWSCharacter::AutoLoadCustomCharacterStats()
{
	UE_LOG(OWS, Verbose, TEXT("AOWSCharacter: AutoLoadCustomCharacterStats"));
}

void AOWSCharacter::GetCharacterStatsBase()
{
	AOWSPlayerController* PC = Cast<AOWSPlayerController>(this->Controller);
	if (PC && PC->PlayerState)
	{
		FString PlayerName = PC->PlayerState->GetPlayerName();		
		PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

		PC->OWSPlayerControllerComponent->GetCharacterStats(PlayerName);
	}
}

void AOWSCharacter::LoadCustomCharacterStats_Implementation() {

}

void AOWSCharacter::UpdateCharacterStatsAfterLoading_Implementation() {

}

void AOWSCharacter::OnRPGInitalizationComplete_Implementation() {

}



void AOWSCharacter::LoadCharacterStatsFromJSON(TSharedPtr<FJsonObject>JsonObject)
{
	UE_LOG(OWS, Verbose, TEXT("AOWSCharacter: Started LoadCharacterStatsFromJSON"));

	CharacterName = JsonObject->GetStringField(TEXT("CharacterName"));

	IsAdmin = JsonObject->GetBoolField(TEXT("IsAdmin"));
	IsModerator = JsonObject->GetBoolField(TEXT("IsModerator"));

	ClassName = JsonObject->GetStringField(TEXT("ClassName"));
	Gender = JsonObject->GetNumberField(TEXT("Gender"));
	CharacterLevel = JsonObject->GetNumberField(TEXT("CharacterLevel"));
	XP = JsonObject->GetNumberField(TEXT("XP"));
	HitDice = JsonObject->GetNumberField(TEXT("HitDie"));
	MaxHP = JsonObject->GetNumberField(TEXT("MaxHealth"));
	Wounds = JsonObject->GetNumberField(TEXT("Wounds"));
	Strength = JsonObject->GetNumberField(TEXT("Strength"));
	Dexterity = JsonObject->GetNumberField(TEXT("Dexterity"));
	Constitution = JsonObject->GetNumberField(TEXT("Constitution"));
	Intellect = JsonObject->GetNumberField(TEXT("Intellect"));
	Wisdom = JsonObject->GetNumberField(TEXT("Wisdom"));
	Charisma = JsonObject->GetNumberField(TEXT("Charisma"));
	Spirit = JsonObject->GetNumberField(TEXT("Spirit"));
	Magic = JsonObject->GetNumberField(TEXT("Magic"));
	Fortitude = JsonObject->GetNumberField(TEXT("Fortitude"));
	Reflex = JsonObject->GetNumberField(TEXT("Reflex"));
	Willpower = JsonObject->GetNumberField(TEXT("Willpower"));
	BaseAttackBonus = JsonObject->GetNumberField(TEXT("BaseAttackBonus"));
	Speed = JsonObject->GetNumberField(TEXT("Speed"));
	Initiative = JsonObject->GetNumberField(TEXT("Initiative"));
	NaturalArmor = JsonObject->GetNumberField(TEXT("NaturalArmor"));
	Resistance = JsonObject->GetNumberField(TEXT("Resistance"));
	TeamNumber = JsonObject->GetNumberField(TEXT("TeamNumber"));

	//Change TeamId for AI Perception
	TeamId = FGenericTeamId(TeamNumber);

	Thirst = JsonObject->GetNumberField(TEXT("Thirst"));
	Hunger = JsonObject->GetNumberField(TEXT("Hunger"));
	Score = JsonObject->GetNumberField(TEXT("Score"));
	MaxHealth = JsonObject->GetNumberField(TEXT("MaxHealth"));
	Health = JsonObject->GetNumberField(TEXT("Health"));
	HealthRegenRate = JsonObject->GetNumberField(TEXT("HealthRegenRate"));
	MaxMana = JsonObject->GetNumberField(TEXT("MaxMana"));
	Mana = JsonObject->GetNumberField(TEXT("Mana"));
	ManaRegenRate = JsonObject->GetNumberField(TEXT("ManaRegenRate"));
	MaxEnergy = JsonObject->GetNumberField(TEXT("MaxEnergy"));
	Energy = JsonObject->GetNumberField(TEXT("Energy"));
	EnergyRegenRate = JsonObject->GetNumberField(TEXT("EnergyRegenRate"));
	MaxFatigue = JsonObject->GetNumberField(TEXT("MaxFatigue"));
	Fatigue = JsonObject->GetNumberField(TEXT("Fatigue"));
	FatigueRegenRate = JsonObject->GetNumberField(TEXT("FatigueRegenRate"));
	MaxStamina = JsonObject->GetNumberField(TEXT("MaxStamina"));
	Stamina = JsonObject->GetNumberField(TEXT("Stamina"));
	StaminaRegenRate = JsonObject->GetNumberField(TEXT("StaminaRegenRate"));
	MaxEndurance = JsonObject->GetNumberField(TEXT("MaxEndurance"));
	Endurance = JsonObject->GetNumberField(TEXT("Endurance"));
	EnduranceRegenRate = JsonObject->GetNumberField(TEXT("EnduranceRegenRate"));
	Agility = JsonObject->GetNumberField(TEXT("Agility"));
	BaseAttack = JsonObject->GetNumberField(TEXT("BaseAttack"));
	BaseAttackBonus = JsonObject->GetNumberField(TEXT("BaseAttackBonus"));
	AttackPower = JsonObject->GetNumberField(TEXT("AttackPower"));
	AttackSpeed = JsonObject->GetNumberField(TEXT("AttackSpeed"));
	CritChance = JsonObject->GetNumberField(TEXT("CritChance"));
	CritMultiplier = JsonObject->GetNumberField(TEXT("CritMultiplier"));
	Haste = JsonObject->GetNumberField(TEXT("Haste"));
	SpellPower = JsonObject->GetNumberField(TEXT("SpellPower"));
	SpellPenetration = JsonObject->GetNumberField(TEXT("SpellPenetration"));
	Defense = JsonObject->GetNumberField(TEXT("Defense"));
	Dodge = JsonObject->GetNumberField(TEXT("Dodge"));
	Parry = JsonObject->GetNumberField(TEXT("Parry"));
	Avoidance = JsonObject->GetNumberField(TEXT("Avoidance"));
	Versatility = JsonObject->GetNumberField(TEXT("Versatility"));
	Multishot = JsonObject->GetNumberField(TEXT("Multishot"));
	Initiative = JsonObject->GetNumberField(TEXT("Initiative"));
	NaturalArmor = JsonObject->GetNumberField(TEXT("NaturalArmor"));
	PhysicalArmor = JsonObject->GetNumberField(TEXT("PhysicalArmor"));
	BonusArmor = JsonObject->GetNumberField(TEXT("BonusArmor"));
	ForceArmor = JsonObject->GetNumberField(TEXT("ForceArmor"));
	MagicArmor = JsonObject->GetNumberField(TEXT("MagicArmor"));
	Resistance = JsonObject->GetNumberField(TEXT("Resistance"));
	ReloadSpeed = JsonObject->GetNumberField(TEXT("ReloadSpeed"));
	Range = JsonObject->GetNumberField(TEXT("Range"));
	Speed = JsonObject->GetNumberField(TEXT("Speed"));

	Gold = JsonObject->GetNumberField(TEXT("Gold"));
	Silver = JsonObject->GetNumberField(TEXT("Silver"));
	Copper = JsonObject->GetNumberField(TEXT("Copper"));
	FreeCurrency = JsonObject->GetNumberField(TEXT("FreeCurrency"));
	PremiumCurrency = JsonObject->GetNumberField(TEXT("PremiumCurrency"));

	Perception = JsonObject->GetNumberField(TEXT("Perception"));
	Acrobatics = JsonObject->GetNumberField(TEXT("Acrobatics"));
	Climb = JsonObject->GetNumberField(TEXT("Climb"));
	Stealth = JsonObject->GetNumberField(TEXT("Stealth"));

	Score = JsonObject->GetNumberField(TEXT("Score"));

	LoadCustomCharacterStats();
	UpdateCharacterStatsAfterLoading();
	OnRPGInitalizationComplete();
}


void AOWSCharacter::UpdateCharacterStatsBase()
{
	AOWSPlayerController* PC = Cast<AOWSPlayerController>(this->Controller);
	if (PC && PC->PlayerState)
	{
		FUpdateCharacterStatsJSONPost CharacterStats;

		FString PlayerName = PC->PlayerState->GetPlayerName();
		PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

		CharacterStats.UpdateCharacterStats.CharName = PlayerName;
		CharacterStats.UpdateCharacterStats.CustomerGUID = OWSAPICustomerKey;

		CharacterStats.UpdateCharacterStats.Gender = Gender;
		//CharacterStats.UpdateCharacterStats.IsAdmin = IsAdmin;
		//CharacterStats.UpdateCharacterStats.IsModerator = IsModerator;
		//CharacterStats.UpdateCharacterStats.IsEnemy = IsEnemy;
		CharacterStats.UpdateCharacterStats.CharacterLevel = CharacterLevel;
		CharacterStats.UpdateCharacterStats.XP = XP;
		CharacterStats.UpdateCharacterStats.TeamNumber = TeamNumber;
		CharacterStats.UpdateCharacterStats.Gold = Gold;
		CharacterStats.UpdateCharacterStats.Silver = Silver;
		CharacterStats.UpdateCharacterStats.Copper = Copper;
		CharacterStats.UpdateCharacterStats.FreeCurrency = FreeCurrency;
		CharacterStats.UpdateCharacterStats.PremiumCurrency = PremiumCurrency;
		CharacterStats.UpdateCharacterStats.Score = Score;
		CharacterStats.UpdateCharacterStats.HitDie = HitDie;
		CharacterStats.UpdateCharacterStats.Wounds = Wounds;
		CharacterStats.UpdateCharacterStats.Thirst = Thirst;
		CharacterStats.UpdateCharacterStats.Hunger = Hunger;
		CharacterStats.UpdateCharacterStats.MaxHealth = MaxHealth;
		CharacterStats.UpdateCharacterStats.Health = Health;
		CharacterStats.UpdateCharacterStats.HealthRegenRate = HealthRegenRate;
		CharacterStats.UpdateCharacterStats.MaxMana = MaxMana;
		CharacterStats.UpdateCharacterStats.Mana = Mana;
		CharacterStats.UpdateCharacterStats.ManaRegenRate = ManaRegenRate;
		CharacterStats.UpdateCharacterStats.MaxEnergy = MaxEnergy;
		CharacterStats.UpdateCharacterStats.Energy = Energy;
		CharacterStats.UpdateCharacterStats.EnergyRegenRate = EnergyRegenRate;
		CharacterStats.UpdateCharacterStats.MaxFatigue = MaxFatigue;
		CharacterStats.UpdateCharacterStats.Fatigue = Fatigue;
		CharacterStats.UpdateCharacterStats.FatigueRegenRate = FatigueRegenRate;
		CharacterStats.UpdateCharacterStats.MaxStamina = MaxStamina;
		CharacterStats.UpdateCharacterStats.Stamina = Stamina;
		CharacterStats.UpdateCharacterStats.StaminaRegenRate = StaminaRegenRate;
		CharacterStats.UpdateCharacterStats.MaxEndurance = MaxEndurance;
		CharacterStats.UpdateCharacterStats.Endurance = Endurance;
		CharacterStats.UpdateCharacterStats.EnduranceRegenRate = EnduranceRegenRate;
		CharacterStats.UpdateCharacterStats.Strength = Strength;
		CharacterStats.UpdateCharacterStats.Dexterity = Dexterity;
		CharacterStats.UpdateCharacterStats.Constitution = Constitution;
		CharacterStats.UpdateCharacterStats.Intellect = Intellect;
		CharacterStats.UpdateCharacterStats.Wisdom = Wisdom;
		CharacterStats.UpdateCharacterStats.Charisma = Charisma;
		CharacterStats.UpdateCharacterStats.Agility = Agility;
		CharacterStats.UpdateCharacterStats.Spirit = Spirit;
		CharacterStats.UpdateCharacterStats.Magic = Magic;
		CharacterStats.UpdateCharacterStats.Fortitude = Fortitude;
		CharacterStats.UpdateCharacterStats.Reflex = Reflex;
		CharacterStats.UpdateCharacterStats.Willpower = Willpower;
		CharacterStats.UpdateCharacterStats.BaseAttack = BaseAttack;
		CharacterStats.UpdateCharacterStats.BaseAttackBonus = BaseAttackBonus;
		CharacterStats.UpdateCharacterStats.AttackPower = AttackPower;
		CharacterStats.UpdateCharacterStats.AttackSpeed = AttackSpeed;
		CharacterStats.UpdateCharacterStats.CritChance = CritChance;
		CharacterStats.UpdateCharacterStats.CritMultiplier = CritMultiplier;
		CharacterStats.UpdateCharacterStats.Haste = Haste;
		CharacterStats.UpdateCharacterStats.SpellPower = SpellPower;
		CharacterStats.UpdateCharacterStats.SpellPenetration = SpellPenetration;
		CharacterStats.UpdateCharacterStats.Defense = Defense;
		CharacterStats.UpdateCharacterStats.Dodge = Dodge;
		CharacterStats.UpdateCharacterStats.Parry = Parry;
		CharacterStats.UpdateCharacterStats.Avoidance = Avoidance;
		CharacterStats.UpdateCharacterStats.Versatility = Versatility;
		CharacterStats.UpdateCharacterStats.Multishot = Multishot;
		CharacterStats.UpdateCharacterStats.Initiative = Initiative;
		CharacterStats.UpdateCharacterStats.NaturalArmor = NaturalArmor;
		CharacterStats.UpdateCharacterStats.PhysicalArmor = PhysicalArmor;
		CharacterStats.UpdateCharacterStats.BonusArmor = BonusArmor;
		CharacterStats.UpdateCharacterStats.ForceArmor = ForceArmor;
		CharacterStats.UpdateCharacterStats.MagicArmor = MagicArmor;
		CharacterStats.UpdateCharacterStats.Resistance = Resistance;
		CharacterStats.UpdateCharacterStats.ReloadSpeed = ReloadSpeed;
		CharacterStats.UpdateCharacterStats.Range = Range;
		CharacterStats.UpdateCharacterStats.Speed = Speed;
		CharacterStats.UpdateCharacterStats.HitDie = HitDice;
		CharacterStats.UpdateCharacterStats.Perception = Perception;
		CharacterStats.UpdateCharacterStats.Acrobatics = Acrobatics;
		CharacterStats.UpdateCharacterStats.Climb = Climb;
		CharacterStats.UpdateCharacterStats.Stealth = Stealth;
		
		FString PostParameters = "";
		if (FJsonObjectConverter::UStructToJsonObjectString(CharacterStats, PostParameters))
		{
			PC->OWSPlayerControllerComponent->UpdateCharacterStats(PostParameters);
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("UpdateCharacterStats Error serializing CharacterStats!"));
		}
	}

	/*
	AOWSPlayerController* PC = Cast<AOWSPlayerController>(this->Controller);
	if (PC && PC->PlayerState)
	{
		FString PlayerName = PC->PlayerState->GetPlayerName();
		PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

		TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
		Request->OnProcessRequestComplete().BindUObject(this, &AOWSCharacter::OnUpdateCharacterStatsBaseResponseReceived);
		//This is the url on which to process the request
		FString url = FString(TEXT("http://" +  + "/RPGServer/UpdateCharacterStats"));

		FString PostParameters = FString(TEXT("id=")) + PlayerName
			+ FString(TEXT("&CharacterLevel=")) + FString::FromInt(this->CharacterLevel)
			+ FString(TEXT("&Gender=")) + FString::FromInt(this->Gender)
			+ FString(TEXT("&Weight=")) + "0"
			+ FString(TEXT("&Size=")) + "0"
			+ FString(TEXT("&Fame=")) + "0"
			+ FString(TEXT("&Alignment=")) + "0"
			+ FString(TEXT("&Description=")) + ""
			+ FString(TEXT("&XP=")) + FString::FromInt(this->XP)
			+ FString(TEXT("&X=")) + FString::SanitizeFloat(this->GetActorLocation().X)
			+ FString(TEXT("&Y=")) + FString::SanitizeFloat(this->GetActorLocation().Y)
			+ FString(TEXT("&Z=")) + FString::SanitizeFloat(this->GetActorLocation().Z)
			+ FString(TEXT("&RX=")) + FString::SanitizeFloat(this->GetActorRotation().Roll)
			+ FString(TEXT("&RY=")) + FString::SanitizeFloat(this->GetActorRotation().Pitch)
			+ FString(TEXT("&RZ=")) + FString::SanitizeFloat(this->GetActorRotation().Yaw)
			+ FString(TEXT("&TeamNumber=")) + FString::FromInt(this->TeamNumber)
			+ FString(TEXT("&HitDice=")) + FString::FromInt(this->HitDice)
			+ FString(TEXT("&Wounds=")) + FString::SanitizeFloat(this->Wounds)
			+ FString(TEXT("&Thirst=")) + FString::SanitizeFloat(this->Thirst)
			+ FString(TEXT("&Hunger=")) + FString::SanitizeFloat(this->Hunger)
			+ FString(TEXT("&MaxHealth=")) + FString::SanitizeFloat(this->MaxHealth)
			+ FString(TEXT("&Health=")) + FString::SanitizeFloat(this->Health)
			+ FString(TEXT("&HealthRegenRate=")) + FString::SanitizeFloat(this->HealthRegenRate)
			+ FString(TEXT("&MaxMana=")) + FString::SanitizeFloat(this->MaxMana)
			+ FString(TEXT("&Mana=")) + FString::SanitizeFloat(this->Mana)
			+ FString(TEXT("&ManaRegenRate=")) + FString::SanitizeFloat(this->ManaRegenRate)
			+ FString(TEXT("&MaxEnergy=")) + FString::SanitizeFloat(this->MaxEnergy)
			+ FString(TEXT("&Energy=")) + FString::SanitizeFloat(this->Energy)
			+ FString(TEXT("&EnergyRegenRate=")) + FString::SanitizeFloat(this->EnergyRegenRate)
			+ FString(TEXT("&MaxFatigue=")) + FString::SanitizeFloat(this->MaxFatigue)
			+ FString(TEXT("&Fatigue=")) + FString::SanitizeFloat(this->Fatigue)
			+ FString(TEXT("&FatigueRegenRate=")) + FString::SanitizeFloat(this->FatigueRegenRate)
			+ FString(TEXT("&MaxStamina=")) + FString::SanitizeFloat(this->MaxStamina)
			+ FString(TEXT("&Stamina=")) + FString::SanitizeFloat(this->Stamina)
			+ FString(TEXT("&StaminaRegenRate=")) + FString::SanitizeFloat(this->StaminaRegenRate)
			+ FString(TEXT("&MaxEndurance=")) + FString::SanitizeFloat(this->MaxEndurance)
			+ FString(TEXT("&Endurance=")) + FString::SanitizeFloat(this->Endurance)
			+ FString(TEXT("&EnduranceRegenRate=")) + FString::SanitizeFloat(this->EnduranceRegenRate)
			+ FString(TEXT("&Strength=")) + FString::SanitizeFloat(this->Strength)
			+ FString(TEXT("&Dexterity=")) + FString::SanitizeFloat(this->Dexterity)
			+ FString(TEXT("&Constitution=")) + FString::SanitizeFloat(this->Constitution)
			+ FString(TEXT("&Intellect=")) + FString::SanitizeFloat(this->Intellect)
			+ FString(TEXT("&Wisdom=")) + FString::SanitizeFloat(this->Wisdom)
			+ FString(TEXT("&Charisma=")) + FString::SanitizeFloat(this->Charisma)
			+ FString(TEXT("&Agility=")) + FString::SanitizeFloat(this->Agility)
			+ FString(TEXT("&Spirit=")) + FString::SanitizeFloat(this->Spirit)
			+ FString(TEXT("&Magic=")) + FString::SanitizeFloat(this->Magic)
			+ FString(TEXT("&Fortitude=")) + FString::SanitizeFloat(this->Fortitude)
			+ FString(TEXT("&Reflex=")) + FString::SanitizeFloat(this->Reflex)
			+ FString(TEXT("&Willpower=")) + FString::SanitizeFloat(this->Willpower)
			+ FString(TEXT("&BaseAttack=")) + FString::SanitizeFloat(this->BaseAttack)
			+ FString(TEXT("&BaseAttackBonus=")) + FString::SanitizeFloat(this->BaseAttackBonus)
			+ FString(TEXT("&AttackPower=")) + FString::SanitizeFloat(this->AttackPower)
			+ FString(TEXT("&AttackSpeed=")) + FString::SanitizeFloat(this->AttackSpeed)
			+ FString(TEXT("&CritChance=")) + FString::SanitizeFloat(this->CritChance)
			+ FString(TEXT("&CritMultiplier=")) + FString::SanitizeFloat(this->CritMultiplier)
			+ FString(TEXT("&Haste=")) + FString::SanitizeFloat(this->Haste)
			+ FString(TEXT("&SpellPower=")) + FString::SanitizeFloat(this->SpellPower)
			+ FString(TEXT("&SpellPenetration=")) + FString::SanitizeFloat(this->SpellPenetration)
			+ FString(TEXT("&Defense=")) + FString::SanitizeFloat(this->Defense)
			+ FString(TEXT("&Dodge=")) + FString::SanitizeFloat(this->Dodge)
			+ FString(TEXT("&Parry=")) + FString::SanitizeFloat(this->Parry)
			+ FString(TEXT("&Avoidance=")) + FString::SanitizeFloat(this->Avoidance)
			+ FString(TEXT("&Versatility=")) + FString::SanitizeFloat(this->Versatility)
			+ FString(TEXT("&Multishot=")) + FString::SanitizeFloat(this->Multishot)
			+ FString(TEXT("&Initiative=")) + FString::SanitizeFloat(this->Initiative)
			+ FString(TEXT("&NaturalArmor=")) + FString::SanitizeFloat(this->NaturalArmor)
			+ FString(TEXT("&PhysicalArmor=")) + FString::SanitizeFloat(this->PhysicalArmor)
			+ FString(TEXT("&BonusArmor=")) + FString::SanitizeFloat(this->BonusArmor)
			+ FString(TEXT("&ForceArmor=")) + FString::SanitizeFloat(this->ForceArmor)
			+ FString(TEXT("&MagicArmor=")) + FString::SanitizeFloat(this->MagicArmor)
			+ FString(TEXT("&Resistance=")) + FString::SanitizeFloat(this->Resistance)
			+ FString(TEXT("&ReloadSpeed=")) + FString::SanitizeFloat(this->ReloadSpeed)
			+ FString(TEXT("&Range=")) + FString::SanitizeFloat(this->Range)
			+ FString(TEXT("&Speed=")) + FString::SanitizeFloat(this->Speed)
			+ FString(TEXT("&Gold=")) + FString::FromInt(this->Gold)
			+ FString(TEXT("&Silver=")) + FString::FromInt(this->Silver)
			+ FString(TEXT("&Copper=")) + FString::FromInt(this->Copper)
			+ FString(TEXT("&FreeCurrency=")) + FString::FromInt(this->FreeCurrency)
			+ FString(TEXT("&PremiumCurrency=")) + FString::FromInt(this->PremiumCurrency)
			+ FString(TEXT("&Perception=")) + FString::SanitizeFloat(this->Perception)
			+ FString(TEXT("&Acrobatics=")) + FString::SanitizeFloat(this->Acrobatics)
			+ FString(TEXT("&Climb=")) + FString::SanitizeFloat(this->Climb)
			+ FString(TEXT("&Stealth=")) + FString::SanitizeFloat(this->Stealth)
			+ FString(TEXT("&Score=")) + FString::FromInt(this->Score)
			+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;
		Request->SetURL(url);
		Request->SetVerb("POST");
		Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
		Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
		Request->SetContentAsString(PostParameters);
		Request->ProcessRequest();
	}
	*/
}

/*
void AOWSCharacter::GetCharacterStatuses()
{
	AOWSPlayerController* PC = Cast<AOWSPlayerController>(this->Controller);
	if (PC && PC->PlayerState)
	{
		FString PlayerName = PC->PlayerState->GetPlayerName();
		PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

		TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
		Request->OnProcessRequestComplete().BindUObject(this, &AOWSCharacter::OnGetCharacterStatusesResponseReceived);
		//This is the url on which to process the request
		FString url = FString(TEXT("http://" +  + "/RPGServer/GetCharacterStatuses"));
		
		FString PostParameters = FString(TEXT("id=")) + PlayerName
			+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

		Request->SetURL(url);
		Request->SetVerb("POST");
		Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
		Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
		Request->SetContentAsString(PostParameters);
		Request->ProcessRequest();
	}
}

void AOWSCharacter::OnGetCharacterStatusesResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			//CharacterName = JsonObject->GetStringField("CharacterName");
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnGetCharacterStatusesResponseReceived Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnGetCharacterStatusesResponseReceived Error accessing server!"));
	}
}

void AOWSCharacter::NotifyGetCharacterStatuses_Implementation() {

}

void AOWSCharacter::ErrorGetCharacterStatuses_Implementation(const FString &ErrorMsg) {

}


void AOWSCharacter::AddOrUpdateCharacterStatus(FString StatusName, int32 StatusValue, int32 StatusDurationMinutes)
{
	AOWSPlayerController* PC = Cast<AOWSPlayerController>(this->Controller);
	if (PC && PC->PlayerState)
	{
		FString PlayerName = PC->PlayerState->GetPlayerName();
		PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

		TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
		Request->OnProcessRequestComplete().BindUObject(this, &AOWSCharacter::OnAddOrUpdateCharacterStatusResponseReceived);
		//This is the url on which to process the request
		FString url = FString(TEXT("http://" +  + "/RPGServer/AddOrUpdateCharacterStatus"));

		FString PostParameters = FString(TEXT("id=")) + PlayerName
			+ FString(TEXT("&StatusName=")) + StatusName
			+ FString(TEXT("&StatusValue=")) + FString::FromInt(StatusValue)
			+ FString(TEXT("&StatusDurationMinutes=")) + FString::FromInt(StatusDurationMinutes)
			+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

		Request->SetURL(url);
		Request->SetVerb("POST");
		Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
		Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
		Request->SetContentAsString(PostParameters);
		Request->ProcessRequest();
	}
}

void AOWSCharacter::OnAddOrUpdateCharacterStatusResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnAddOrUpdateCharacterStatusResponseReceived Error accessing server!"));
	}
}
*/


void AOWSCharacter::GetCustomCharacterData()
{
	AOWSPlayerController* PC = Cast<AOWSPlayerController>(this->Controller);
	if (PC && PC->PlayerState)
	{
		FString PlayerName = PC->PlayerState->GetPlayerName();

		PC->OWSPlayerControllerComponent->GetCustomCharacterData(PlayerName);
	}

	/*
	AOWSPlayerController* PC = Cast<AOWSPlayerController>(this->Controller);
	if (PC && PC->PlayerState)
	{
		FString PlayerName = PC->PlayerState->GetPlayerName();
		PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

		TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
		Request->OnProcessRequestComplete().BindUObject(this, &AOWSCharacter::OnGetCustomCharacterDataResponseReceived);
		//This is the url on which to process the request
		FString url = FString(TEXT("http://" +  + "/RPGServer/GetCustomCharacterData"));

		FString PostParameters = FString(TEXT("id=")) + PlayerName
			 + FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

		Request->SetURL(url);
		Request->SetVerb("POST");
		Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
		Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
		Request->SetContentAsString(PostParameters);
		Request->ProcessRequest();
	}
	*/
}

void AOWSCharacter::ProcessCustomCharacterData(TSharedPtr<FJsonObject> JsonObject)
{
	TArray<FCustomCharacterDataStruct> CustomCharacterData;

	if (JsonObject->HasField(TEXT("rows")))
	{
		TArray<TSharedPtr<FJsonValue>> Rows = JsonObject->GetArrayField(TEXT("rows"));

		for (int RowNum = 0; RowNum != Rows.Num(); RowNum++) {
			FCustomCharacterDataStruct tempCustomData;
			TSharedPtr<FJsonObject> tempRow = Rows[RowNum]->AsObject();
			tempCustomData.CustomFieldName = tempRow->GetStringField(TEXT("CustomFieldName"));
			tempCustomData.FieldValue = tempRow->GetStringField(TEXT("FieldValue"));

			CustomCharacterData.Add(tempCustomData);
		}

		NotifyGetCustomCharacterData(CustomCharacterData);
	}
	else
	{
		UE_LOG(OWS, Warning, TEXT("ProcessCustomCharacterData Server returned no data!  This can happen when the Character has no Custom Data and might not be an error."));
		ErrorGetCustomCharacterData(TEXT("ProcessCustomCharacterData Server returned no data!  This can happen when the Character has no Custom Data and might not be an error."));
	}
}


void AOWSCharacter::NotifyGetCustomCharacterData_Implementation(const TArray<FCustomCharacterDataStruct> &CustomCharacterData) {

}

void AOWSCharacter::ErrorGetCustomCharacterData_Implementation(const FString &ErrorMsg) {

}


void AOWSCharacter::AddOrUpdateCustomCharacterData(FString CustomFieldName, FString CustomValue)
{
	AOWSPlayerController* PC = Cast<AOWSPlayerController>(this->Controller);
	if (PC && PC->PlayerState)
	{
		FString PlayerName = PC->PlayerState->GetPlayerName();

		PC->OWSPlayerControllerComponent->AddOrUpdateCustomCharacterData(PlayerName, CustomFieldName, CustomValue);
	}

	/*
	AOWSPlayerController* PC = Cast<AOWSPlayerController>(this->Controller);
	if (PC)
	{
		FString PlayerName = PC->PlayerState->GetPlayerName();
		PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

		TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
		Request->OnProcessRequestComplete().BindUObject(this, &AOWSCharacter::OnAddOrUpdateCustomCharacterDataResponseReceived);
		//This is the url on which to process the request
		FString url = FString(TEXT("http://" +  + "/RPGServer/AddOrUpdateCustomCharacterData"));
			
		FString PostParameters = FString(TEXT("id=")) + PlayerName
			+ FString(TEXT("&FieldName=")) + CustomFieldName
			+ FString(TEXT("&FieldValue=")) + CustomValue
			+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

		UE_LOG(LogTemp, Log, TEXT("OnAddOrUpdateCustomCharacterDataResponseReceived - PlayerName: %s, FieldName: %s, Value: %s"), *PlayerName, *CustomFieldName, *CustomValue);

		Request->SetURL(url);
		Request->SetVerb("POST");
		Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
		Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
		Request->SetContentAsString(PostParameters);
		Request->ProcessRequest();
	}
	*/
}

void AOWSCharacter::OnAddOrUpdateCustomCharacterDataResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			UE_LOG(OWS, Verbose, TEXT("OnAddOrUpdateCustomCharacterDataResponseReceived Success!"));
		}
		else
		{
		UE_LOG(OWS, Error, TEXT("OnAddOrUpdateCustomCharacterDataResponseReceived Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnAddOrUpdateCustomCharacterDataResponseReceived Error accessing server!"));
	}
}





void AOWSCharacter::GetInventoryItems(FString InventoryName)
{
	/*
	AOWSPlayerController* PC = Cast<AOWSPlayerController>(this->Controller);
	if (PC && PC->PlayerState)
	{
		FString PlayerName = PC->PlayerState->GetPlayerName();
		PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

		TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
		Request->OnProcessRequestComplete().BindUObject(this, &AOWSCharacter::OnGetInventoryItemsResponseReceived);
		//This is the url on which to process the request
		FString url = FString(TEXT("http://" +  + "/RPGInventory/GetInventoryItems"));

		FString PostParameters = FString(TEXT("id=")) + PlayerName
			+ FString(TEXT("&InventoryName=")) + InventoryName
			+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

		Request->SetURL(url);
		Request->SetVerb("POST");
		Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
		Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
		Request->SetContentAsString(PostParameters);
		Request->ProcessRequest();
	}*/
}

void AOWSCharacter::OnGetInventoryItemsResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	/*
	int32 InventorySize = 1.f;

	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			TArray<FInventoryItemStruct> InventoryItems;
			FName InventoryName;

			if (JsonObject->HasField("rows"))
			{
				TArray<TSharedPtr<FJsonValue>> Rows = JsonObject->GetArrayField("rows");

				//Send in JSON and get back InventoryName, InventorySize, and InventoryItems
				ReadInventoryItems(Rows, InventoryName, InventorySize, InventoryItems);
			}

			if (bShouldAutoLoadInventoriesToManage)
			{
				//Make sure the inventory size is in a valid range:
				if (InventorySize < 1)
					InventorySize = 1;
				if (InventorySize > 128)
					InventorySize = 128;

				//If found in InventoriesToManage, use that
				UOWSInventory* InventoryToFill = GetHUDInventoryFromName(InventoryName);
				if (InventoryToFill)
				{
					if (InventoryItems.Num() > 0)
					{
						InventoryToFill->AddItemsFromInventoryItemStruct(InventoryItems);
					}
				}
				else //Create the inventory
				{
					UOWSInventory* NewlyCreatedInventory = CreateHUDInventory(InventoryName, InventorySize, FMath::FloorToInt(FMath::Sqrt((float)InventorySize)));
					if (InventoryItems.Num() > 0)
					{
						NewlyCreatedInventory->AddItemsFromInventoryItemStruct(InventoryItems);
					}
				}

				//GetOWSPlayerController()->SynchUpLocalMeshItemsMap();
			}

			GetInventoryItemsComplete(InventoryName.ToString(), InventoryItems, InventorySize);
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnGetInventoryItemsResponseReceived Server returned no data!"));
			GetInventoryItemsError(TEXT("OnGetInventoryItemsResponseReceived Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnGetInventoryItemsResponseReceived Error accessing server!"));
		GetInventoryItemsError(TEXT("OnGetInventoryItemsResponseReceived Error accessing server!"));
	}
	*/
}

void AOWSCharacter::ReadInventoryItems(const TArray<TSharedPtr<FJsonValue>> Rows, FName& InventoryName, int32& InventorySize, TArray<FInventoryItemStruct>& InventoryItems)
{
	for (int RowNum = 0; RowNum != Rows.Num(); RowNum++) {
		FInventoryItemStruct tempInventoryItem;
		TSharedPtr<FJsonObject> tempRow = Rows[RowNum]->AsObject();
		tempInventoryItem.ItemName = tempRow->GetStringField(TEXT("ItemName"));
		tempInventoryItem.InventoryName = tempRow->GetStringField(TEXT("InventoryName"));
		InventoryName = FName(*tempInventoryItem.InventoryName);
		InventorySize = tempRow->GetIntegerField(TEXT("InventorySize"));

		//Default row when an inventory is empty just to get the inventory name
		if (tempInventoryItem.ItemName == "EmptyInventory")
			continue;

		tempInventoryItem.ItemDescription = tempRow->GetStringField(TEXT("ItemDescription"));
		FString tempUniqueItemGUIDString = tempRow->GetStringField(TEXT("CharInventoryItemGUID"));
		FGuid::Parse(tempUniqueItemGUIDString, tempInventoryItem.UniqueItemGUID);
		tempInventoryItem.InSlotNumber = tempRow->GetIntegerField(TEXT("InSlotNumber"));
		tempInventoryItem.Quantity = tempRow->GetIntegerField(TEXT("Quantity"));
		tempInventoryItem.ItemCanStack = tempRow->GetBoolField(TEXT("ItemCanStack"));
		tempInventoryItem.ItemStackSize = tempRow->GetIntegerField(TEXT("ItemStackSize"));
		tempInventoryItem.IsUsable = tempRow->GetBoolField(TEXT("ItemIsUsable"));
		tempInventoryItem.IsConsumedOnUse = tempRow->GetBoolField(TEXT("ItemIsConsumedOnUse"));
		tempInventoryItem.NumberOfUsesLeft = tempRow->GetIntegerField(TEXT("NumberOfUsesLeft"));
		tempInventoryItem.ItemWeight = (float)tempRow->GetNumberField(TEXT("ItemWeight"));
		tempInventoryItem.ItemTypeID = tempRow->GetIntegerField(TEXT("ItemTypeID"));
		tempInventoryItem.ItemTypeDescription = tempRow->GetStringField(TEXT("ItemTypeDesc"));
		tempInventoryItem.ItemTypeQuality = tempRow->GetIntegerField(TEXT("ItemTypeQuality"));

		tempInventoryItem.UserItemType = tempRow->GetIntegerField(TEXT("UserItemType"));
		tempInventoryItem.EquipmentType = tempRow->GetIntegerField(TEXT("EquipmentType"));
		tempInventoryItem.EquipmentSlotType = tempRow->GetIntegerField(TEXT("EquipmentSlotType"));
		tempInventoryItem.ItemTier = tempRow->GetIntegerField(TEXT("ItemTier"));
		tempInventoryItem.ItemQuality = tempRow->GetIntegerField(TEXT("ItemQuality"));
		tempInventoryItem.ItemDuration = tempRow->GetIntegerField(TEXT("ItemDuration"));
		tempInventoryItem.CanBeDropped = tempRow->GetBoolField(TEXT("CanBeDropped"));
		tempInventoryItem.CanBeDestroyed = tempRow->GetBoolField(TEXT("CanBeDestroyed"));

		tempInventoryItem.CustomData = tempRow->GetStringField(TEXT("CustomData"));
		tempInventoryItem.PerInstanceCustomData = tempRow->GetStringField(TEXT("PerInstanceCustomData"));
		tempInventoryItem.Condition = tempRow->GetIntegerField(TEXT("Condition"));
		tempInventoryItem.PremiumCurrencyPrice = tempRow->GetIntegerField(TEXT("PremiumCurrencyPrice"));
		tempInventoryItem.FreeCurrencyPrice = tempRow->GetIntegerField(TEXT("FreeCurrencyPrice"));

		tempInventoryItem.ItemMeshID = tempRow->GetIntegerField(TEXT("ItemMeshID"));
		tempInventoryItem.WeaponActorClassPath = tempRow->GetStringField(TEXT("WeaponActorClass"));
		tempInventoryItem.StaticMeshPath = tempRow->GetStringField(TEXT("StaticMesh"));
		tempInventoryItem.SkeletalMeshPath = tempRow->GetStringField(TEXT("SkeletalMesh"));

		//TextureIcon
		tempInventoryItem.TextureToUseForIcon = tempRow->GetStringField(TEXT("TextureToUseForIcon"));
		tempInventoryItem.TextureIcon = nullptr;
		if (!tempInventoryItem.TextureToUseForIcon.IsEmpty())
		{
			tempInventoryItem.TextureIcon = LoadObject<UTexture2D>(NULL, *tempInventoryItem.TextureToUseForIcon, NULL, LOAD_None, NULL);

			if (!tempInventoryItem.TextureIcon)
			{
				UE_LOG(LogTemp, Error, TEXT("Error loading Texture Icon!"));
			}
		}

		tempInventoryItem.IconSlotWidth = tempRow->GetIntegerField(TEXT("IconSlotWidth"));
		tempInventoryItem.IconSlotHeight = tempRow->GetIntegerField(TEXT("IconSlotHeight"));

		if (tempInventoryItem.IconSlotWidth < 1)
			tempInventoryItem.IconSlotWidth = 1;

		if (tempInventoryItem.IconSlotHeight < 1)
			tempInventoryItem.IconSlotHeight = 1;

		InventoryItems.Add(tempInventoryItem);
	}
}


//This is an example showing how you can override a BP event in C++.  This will not be called if the BP event node exists in the BP.
void AOWSCharacter::GetInventoryItemsComplete_Implementation(const FString &InventoryName, const TArray<FInventoryItemStruct> &InventoryItems, int32 InventorySize)
{
	UE_LOG(OWS, Error, TEXT("GetInventoryItemsComplete_Implementation called!"));

}

void AOWSCharacter::GetInventoryItemsError_Implementation(const FString &ErrorMsg) {

}


void AOWSCharacter::AddItemToInventory(FString InventoryName, FString ItemName, int InSlotNumber, int Quantity, int NumberOfUsesLeft, int Condition, FGuid &UniqueItemGUID)
{
	/*
	AOWSPlayerController* PC = Cast<AOWSPlayerController>(this->Controller);
	if (PC && PC->PlayerState)
	{
		FString PlayerName = PC->PlayerState->GetPlayerName();
		PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

		UniqueItemGUID = FGuid::NewGuid();

		Client_AddItemToInventory(FName(*InventoryName), ItemName, 1, InSlotNumber, NumberOfUsesLeft, Condition, "", UniqueItemGUID, 0);

		TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
		Request->OnProcessRequestComplete().BindUObject(this, &AOWSCharacter::OnAddItemToInventoryResponseReceived);
		//This is the url on which to process the request
		FString url = FString(TEXT("http://" +  + "/RPGInventory/AddItemToInventory"));

		FString PostParameters = FString(TEXT("id=")) + PlayerName
			+ FString(TEXT("&InventoryName=")) + InventoryName
			+ FString(TEXT("&UniqueItemGUID=")) + UniqueItemGUID.ToString()
			+ FString(TEXT("&ItemName=")) + ItemName
			+ FString(TEXT("&InSlotNumber=")) + FString::FromInt(InSlotNumber)
			+ FString(TEXT("&Quantity=")) + FString::FromInt(Quantity)
			+ FString(TEXT("&NumberOfUsesLeft=")) + FString::FromInt(NumberOfUsesLeft)
			+ FString(TEXT("&Condition=")) + FString::FromInt(Condition)
			+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

		Request->SetURL(url);
		Request->SetVerb("POST");
		Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
		Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
		Request->SetContentAsString(PostParameters);
		Request->ProcessRequest();
	}
	*/
}

void AOWSCharacter::OnAddItemToInventoryResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	/*
	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			AddItemToInventoryComplete();
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnAddItemToInventoryResponseReceived Server returned no data!"));
			AddItemToInventoryError();
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnAddItemToInventoryResponseReceived Error accessing server!"));
		AddItemToInventoryError();
	}
	*/
}


void AOWSCharacter::AddItemToInventoryComplete_Implementation()
{

}

void AOWSCharacter::AddItemToInventoryError_Implementation()
{

}

void AOWSCharacter::AddItemToInventoryWithCustomData(FString InventoryName, FString ItemName, int InSlotNumber, int Quantity, int NumberOfUsesLeft, int Condition, FString CustomData, FGuid &UniqueItemGUID)
{
	/*
	AOWSPlayerController* PC = Cast<AOWSPlayerController>(this->Controller);
	if (PC && PC->PlayerState)
	{
		FString PlayerName = PC->PlayerState->GetPlayerName();
		PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

		UniqueItemGUID = FGuid::NewGuid();

		Client_AddItemToInventory(FName(*InventoryName), ItemName, 1, InSlotNumber, NumberOfUsesLeft, Condition, CustomData, UniqueItemGUID, 0);

		TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
		Request->OnProcessRequestComplete().BindUObject(this, &AOWSCharacter::OnAddItemToInventoryWithCustomDataResponseReceived);
		//This is the url on which to process the request
		FString url = FString(TEXT("http://" +  + "/RPGInventory/AddItemToInventoryWithCustomData"));

		FString PostParameters = FString(TEXT("id=")) + PlayerName
			+ FString(TEXT("&InventoryName=")) + InventoryName
			+ FString(TEXT("&UniqueItemGUID=")) + UniqueItemGUID.ToString()
			+ FString(TEXT("&ItemName=")) + ItemName
			+ FString(TEXT("&InSlotNumber=")) + FString::FromInt(InSlotNumber)
			+ FString(TEXT("&Quantity=")) + FString::FromInt(Quantity)
			+ FString(TEXT("&NumberOfUsesLeft=")) + FString::FromInt(NumberOfUsesLeft)
			+ FString(TEXT("&Condition=")) + FString::FromInt(Condition)
			+ FString(TEXT("&CustomData=")) + CustomData
			+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

		Request->SetURL(url);
		Request->SetVerb("POST");
		Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
		Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
		Request->SetContentAsString(PostParameters);
		Request->ProcessRequest();
	}
	*/
}

void AOWSCharacter::OnAddItemToInventoryWithCustomDataResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	/*
	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			AddItemToInventoryWithCustomDataComplete();
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnAddItemToInventoryWithCustomDataResponseReceived Server returned no data!"));
			AddItemToInventoryWithCustomDataError();
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnAddItemToInventoryWithCustomDataResponseReceived Error accessing server!"));
		AddItemToInventoryWithCustomDataError();
	}
	*/
}

void AOWSCharacter::AddItemToInventoryWithCustomDataComplete_Implementation() {

}

void AOWSCharacter::AddItemToInventoryWithCustomDataError_Implementation() {

}

void AOWSCharacter::RemoveItemFromInventory(FGuid UniqueItemGUID)
{
	/*
	AOWSPlayerController* PC = Cast<AOWSPlayerController>(this->Controller);
	if (PC && PC->PlayerState)
	{
		FString PlayerName = PC->PlayerState->GetPlayerName();
		PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

		TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
		Request->OnProcessRequestComplete().BindUObject(this, &AOWSCharacter::OnRemoveItemFromInventoryResponseReceived);
		//This is the url on which to process the request
		FString url = FString(TEXT("http://" +  + "/RPGInventory/RemoveItemFromInventory"));

		FString PostParameters = FString(TEXT("id=")) + PlayerName
			+ FString(TEXT("&UniqueItemGUID=")) + UniqueItemGUID.ToString()
			+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

		Request->SetURL(url);
		Request->SetVerb("POST");
		Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
		Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
		Request->SetContentAsString(PostParameters);
		Request->ProcessRequest();
	}
	*/
}

void AOWSCharacter::OnRemoveItemFromInventoryResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	/*
	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnRemoveItemFromInventoryResponseReceived Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnRemoveItemFromInventoryResponseReceived Error accessing server!"));
	}
	*/
}


void AOWSCharacter::SaveInventory(FString InventoryName, FString InventoryData)
{
	/*
	AOWSPlayerController* PC = Cast<AOWSPlayerController>(this->Controller);
	if (PC && PC->PlayerState)
	{
		FString PlayerName = PC->PlayerState->GetPlayerName();
		PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

		TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
		Request->OnProcessRequestComplete().BindUObject(this, &AOWSCharacter::OnSaveInventoryResponseReceived);
		//This is the url on which to process the request
		FString url = FString(TEXT("http://" +  + "/RPGInventory/SaveInventory"));

		FString PostParameters = FString(TEXT("id=")) + PlayerName
			+ FString(TEXT("&InventoryName=")) + InventoryName
			+ FString(TEXT("&InventoryData=")) + InventoryData
			+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

		Request->SetURL(url);
		Request->SetVerb("POST");
		Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
		Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
		Request->SetContentAsString(PostParameters);
		Request->ProcessRequest();
	}
	*/
}

void AOWSCharacter::OnSaveInventoryResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	/*
	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnSaveInventoryResponseReceived Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnSaveInventoryResponseReceived Error accessing server!"));
	}
	*/
}


void AOWSCharacter::SerializeAndSaveInventory(FName InventoryName)
{
	/*
	AOWSPlayerController* PC = Cast<AOWSPlayerController>(this->Controller);
	if (PC && PC->PlayerState)
	{
		FString PlayerName = PC->PlayerState->GetPlayerName();
		PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

		TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
		Request->OnProcessRequestComplete().BindUObject(this, &AOWSCharacter::OnSerializeAndSaveInventoryResponseReceived);
		//This is the url on which to process the request
		FString url = FString(TEXT("http://" +  + "/RPGInventory/SaveInventory"));

		FString PostParameters = FString(TEXT("id=")) + PlayerName
			+ FString(TEXT("&InventoryName=")) + InventoryName.ToString()
			+ FString(TEXT("&InventoryData=")) + SerializeInventory(InventoryName)
			+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

		Request->SetURL(url);
		Request->SetVerb("POST");
		Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
		Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
		Request->SetContentAsString(PostParameters);
		Request->ProcessRequest();
	}
	*/
}

void AOWSCharacter::OnSerializeAndSaveInventoryResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	/*
	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnSerializeAndSaveInventoryResponseReceived Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnSerializeAndSaveInventoryResponseReceived Error accessing server!"));
	}
	*/
}


void AOWSCharacter::CreateInventory(FString InventoryName, int InventorySize)
{
	/*
	AOWSPlayerController* PC = Cast<AOWSPlayerController>(this->Controller);
	if (PC && PC->PlayerState)
	{
		FString PlayerName = PC->PlayerState->GetPlayerName();
		PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

		TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
		Request->OnProcessRequestComplete().BindUObject(this, &AOWSCharacter::OnSaveInventoryResponseReceived);
		//This is the url on which to process the request
		FString url = FString(TEXT("http://" +  + "/RPGInventory/CreateInventory"));

		FString PostParameters = FString(TEXT("id=")) + PlayerName
			+ FString(TEXT("&InventoryName=")) + InventoryName
			+ FString(TEXT("&InventorySize=")) + FString::FromInt(InventorySize)
			+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

		Request->SetURL(url);
		Request->SetVerb("POST");
		Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
		Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
		Request->SetContentAsString(PostParameters);
		Request->ProcessRequest();
	}
	*/
}

void AOWSCharacter::OnCreateInventoryResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	/*
	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			CreateInventoryComplete();
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnSaveInventoryResponseReceived Server returned no data!"));
			CreateInventoryError(TEXT("OnSaveInventoryResponseReceived Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnSaveInventoryResponseReceived Error accessing server!"));
		CreateInventoryError(TEXT("OnSaveInventoryResponseReceived Error accessing server!"));
	}
	*/
}


void AOWSCharacter::CreateInventoryComplete_Implementation() {

}

void AOWSCharacter::CreateInventoryError_Implementation(const FString &ErrorMsg) {

}


void AOWSCharacter::ParseInventoryCustomData(FString InventoryCustomData, TArray<FCustomInventoryItemDataStruct> &ItemCustomDataArray)
{
	TArray<FCustomInventoryItemDataStruct> tempCustomInventoryData;

	FCustomInventoryItemDataStruct tempInventoryItemData;

	TSharedPtr<FJsonObject> JsonObject;
	TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(InventoryCustomData);

	if (FJsonSerializer::Deserialize(Reader, JsonObject))
	{
		for (auto It = JsonObject->Values.CreateConstIterator(); It; ++It)
		{
			tempInventoryItemData.CustomFieldName = It.Key();
			tempInventoryItemData.FieldValue = It.Value()->AsString();
			tempCustomInventoryData.Add(tempInventoryItemData);
		}

		ItemCustomDataArray = tempCustomInventoryData;
	}		
}


/***** Abilities *****/
void AOWSCharacter::GetCharacterAbilities()
{
	AOWSPlayerController* PC = Cast<AOWSPlayerController>(this->Controller);
	if (PC && PC->PlayerState)
	{
		FString PlayerName = PC->PlayerState->GetPlayerName();

		PC->OWSPlayerControllerComponent->GetCharacterAbilities(PlayerName);
	}
}

void AOWSCharacter::GetCharacterAbilitiesComplete_Implementation(const TArray<FAbility> &AbilityBars) {

}

void AOWSCharacter::GetCharacterAbilitiesError_Implementation(const FString &ErrorMsg) {

}


void AOWSCharacter::GetAbilityBars()
{
	/*
	AOWSPlayerController* PC = Cast<AOWSPlayerController>(this->Controller);
	if (PC && PC->PlayerState)
	{
		FString PlayerName = PC->PlayerState->GetPlayerName();
		PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

		TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
		Request->OnProcessRequestComplete().BindUObject(this, &AOWSCharacter::OnGetAbilityBarsResponseReceived);
		//This is the url on which to process the request
		FString url = FString(TEXT("http://" +  + "/RPGAbility/GetAbilityBars"));

		FString PostParameters = FString(TEXT("id=")) + PlayerName
			+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

		Request->SetURL(url);
		Request->SetVerb("POST");
		Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
		Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
		Request->SetContentAsString(PostParameters);
		Request->ProcessRequest();
	}
	*/

	AOWSPlayerController* PC = Cast<AOWSPlayerController>(this->Controller);
	if (PC && PC->PlayerState)
	{
		FString PlayerName = PC->PlayerState->GetPlayerName();

		PC->OWSPlayerControllerComponent->GetAbilityBars(PlayerName);
	}	
}

void AOWSCharacter::OnGetAbilityBarsResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	/*
	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			TArray<FAbilityBarStruct> AbilityBars;

			if (JsonObject->HasField("rows"))
			{
				TArray<TSharedPtr<FJsonValue>> Rows = JsonObject->GetArrayField("rows");

				int32 CharAbilityBarID = 0;

				if (Rows.Num() > 0)
				{
					for (int RowNum = 0; RowNum != Rows.Num(); RowNum++) {
						FAbilityBarStruct tempAbilityBar;
						TSharedPtr<FJsonObject> tempRow = Rows[RowNum]->AsObject();

						tempAbilityBar.AbilityBarID = tempRow->GetNumberField("CharAbilityBarID");
						tempAbilityBar.AbilityBarName = tempRow->GetStringField("AbilityBarName");
						tempAbilityBar.MaxNumberOfSlots = tempRow->GetNumberField("MaxNumberOfSlots");
						tempAbilityBar.NumberOfUnlockedSlots = tempRow->GetNumberField("NumberOfUnlockedSlots");
						tempAbilityBar.AbilityBarCustomJSON = tempRow->GetStringField("CharAbilityBarsCustomJSON");

						AbilityBars.Add(tempAbilityBar);
					}
				}
			}
			GetAbilityBarsComplete(AbilityBars);
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnGetAbilityBarsResponseReceived Server returned no data!"));
			GetAbilityBarsError(TEXT("OnGetAbilityBarsResponseReceived Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnGetAbilityBarsResponseReceived Error accessing server!"));
		GetAbilityBarsError(TEXT("OnGetAbilityBarsResponseReceived Error accessing server!"));
	}
	*/
}


void AOWSCharacter::GetAbilityBarsComplete_Implementation(const TArray<FAbilityBar> &AbilityBars) {

}

void AOWSCharacter::GetAbilityBarsError_Implementation(const FString &ErrorMsg) {

}


void AOWSCharacter::GetAbilityBarsAndAbilities()
{
	/*
	AOWSPlayerController* PC = Cast<AOWSPlayerController>(this->Controller);
	if (PC && PC->PlayerState)
	{
		FString PlayerName = PC->PlayerState->GetPlayerName();
		PlayerName = PlayerName.Replace(TEXT(" "), TEXT("%20"));

		TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = Http->CreateRequest();
		Request->OnProcessRequestComplete().BindUObject(this, &AOWSCharacter::OnGetAbilityBarsAndAbilitiesResponseReceived);
		//This is the url on which to process the request
		FString url = FString(TEXT("http://" +  + "/RPGAbility/GetAbilityBarsAndAbilities"));

		FString PostParameters = FString(TEXT("id=")) + PlayerName
			+ FString(TEXT("&CustomerGUID=")) + RPGAPICustomerKey;

		Request->SetURL(url);
		Request->SetVerb("POST");
		Request->SetHeader(TEXT("User-Agent"), "X-UnrealEngine-Agent");
		Request->SetHeader("Content-Type", TEXT("application/x-www-form-urlencoded"));
		Request->SetContentAsString(PostParameters);
		Request->ProcessRequest();
	}
	*/
}

void AOWSCharacter::OnGetAbilityBarsAndAbilitiesResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful)
{
	/*
	if (bWasSuccessful)
	{
		TSharedPtr<FJsonObject> JsonObject;
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Response->GetContentAsString());

		if (FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			TArray<FAbilityBarStruct> AbilityBars;

			if (JsonObject->HasField("rows"))
			{
				TArray<TSharedPtr<FJsonValue>> Rows = JsonObject->GetArrayField("rows");

				int32 CharAbilityBarID = 0;

				FAbilityBarStruct tempAbilityBar;
				FAbilityStruct tempAbility;

				if (Rows.Num() > 0)
				{
					TSharedPtr<FJsonObject> tempFirstRow = Rows[0]->AsObject();
					CharAbilityBarID = tempFirstRow->GetNumberField("CharAbilityBarID");

					for (int RowNum = 0; RowNum != Rows.Num(); RowNum++) {
						TSharedPtr<FJsonObject> tempRow = Rows[RowNum]->AsObject();

						tempAbilityBar.AbilityBarID = tempRow->GetNumberField("CharAbilityBarID");
						tempAbilityBar.AbilityBarCustomJSON = tempRow->GetStringField("CharAbilityBarsCustomJSON");

						if (CharAbilityBarID != tempAbilityBar.AbilityBarID)
						{
							AbilityBars.Add(tempAbilityBar);
							tempAbilityBar.Abilities.Empty(0);
						}

						tempAbilityBar.AbilityBarName = tempRow->GetStringField("AbilityBarName");
						tempAbilityBar.MaxNumberOfSlots = tempRow->GetNumberField("MaxNumberOfSlots");
						tempAbilityBar.NumberOfUnlockedSlots = tempRow->GetNumberField("NumberOfUnlockedSlots");
						tempAbilityBar.AbilityBarCustomJSON = tempRow->GetStringField("CharAbilityBarsCustomJSON");

						tempAbility.AbilityName = tempRow->GetStringField("AbilityName");
						tempAbility.AbilityLevel = tempRow->GetNumberField("AbilityLevel");
						tempAbility.AbilityInSlotNumber = tempRow->GetNumberField("InSlotNumber");
						tempAbility.GameplayAbilityClassName = tempRow->GetStringField("GameplayAbilityClassName");
						tempAbility.TextureToUseForIcon = tempRow->GetStringField("TextureToUseForIcon");

						tempAbility.AbilityCustomJSON = tempRow->GetStringField("AbilityCustomJSON");
						tempAbility.CharHasAbilitiesCustomJSON = tempRow->GetStringField("CharHasAbilitiesCustomJSON");

						tempAbilityBar.Abilities.Add(tempAbility);
						
						CharAbilityBarID = tempAbilityBar.AbilityBarID;
					}

					AbilityBars.Add(tempAbilityBar);
				}
			}
			GetAbilityBarsAndAbilitiesComplete(AbilityBars);
		}
		else
		{
			UE_LOG(OWS, Error, TEXT("OnGetAbilityBarsAndAbilitiesResponseReceived Server returned no data!"));
			GetAbilityBarsAndAbilitiesError(TEXT("OnGetAbilityBarsAndAbilitiesResponseReceived Server returned no data!"));
		}
	}
	else
	{
		UE_LOG(OWS, Error, TEXT("OnGetAbilityBarsAndAbilitiesResponseReceived Error accessing server!"));
		GetAbilityBarsAndAbilitiesError(TEXT("OnGetAbilityBarsAndAbilitiesResponseReceived Error accessing server!"));
	}
	*/
}

void AOWSCharacter::GetAbilityBarsAndAbilitiesComplete_Implementation(const TArray<FAbilityBarWithAbilities> &AbilityBars) {

}

void AOWSCharacter::GetAbilityBarsAndAbilitiesError_Implementation(const FString &ErrorMsg) {

}


AOWSGameMode* AOWSCharacter::GetGameMode()
{
	return (AOWSGameMode*)GetWorld()->GetAuthGameMode();
}


//HUD Inventory System
void AOWSCharacter::OnRep_InventoriesToManage()
{
	if (InventoriesToManage.Num() < 1)
	{
		UE_LOG(OWS, Error, TEXT("OnRep_InventoriesToManage.Num() < 1"));
		return;
	}

	if (InventoriesToManage[0])
	{
		UE_LOG(OWS, Error, TEXT("OnRep_InventoriesToManage First Entry is Valid!"));
		return;
	}

	UE_LOG(OWS, Error, TEXT("OnRep_InventoriesToManage"));
}

UOWSInventory* AOWSCharacter::CreateHUDInventory(FName InventoryName, int32 Size, int32 NumberOfColumns)
{
	if (GetLocalRole() == ROLE_Authority)
	{
		UOWSInventory* Inventory = NewObject<UOWSInventory>();
		Inventory->SetInventoryName(InventoryName);
		Inventory->SetInventorySize(Size, NumberOfColumns);
		Inventory->SetOwningPlayerCharacter(this);
		InventoriesToManage.Add(Inventory);
		Client_CreateHUDInventory(InventoryName, Size, NumberOfColumns);
		return Inventory;
	}

	return nullptr;
}

void AOWSCharacter::Client_CreateHUDInventory_Implementation(FName InventoryName, int32 Size, int32 NumberOfColumns)
{
	if (GetLocalRole() != ROLE_Authority)
	{
		UOWSInventory* Inventory = NewObject<UOWSInventory>();
		Inventory->SetInventoryName(InventoryName);
		Inventory->SetInventorySize(Size, NumberOfColumns);
		Inventory->SetOwningPlayerCharacter(this);
		InventoriesToManage.Add(Inventory);
	}
}

void AOWSCharacter::Client_AddItemToInventory_Implementation(const FName& InventoryName, const FString& ItemName, const int32 StackSize, const int32 InSlotNumber, const int32 NumberOfUsesLeft, const int32 Condition,
	const FString& PerInstanceCustomData, const FGuid UniqueItemGUID, const int32 ItemMeshID)
{
	AOWSInventoryItem* Item = NewObject<AOWSInventoryItem>();
	Item->ItemName = ItemName;
	Item->StackSize = StackSize;
	Item->NumberOfUsesLeft = NumberOfUsesLeft;
	Item->Condition = Condition;
	Item->PerInstanceCustomData = PerInstanceCustomData;
	Item->UniqueItemGUID = UniqueItemGUID;
	Item->ItemMeshID = ItemMeshID;

	auto FoundEntry = LocalInventoryItems.FindByPredicate([&](FInventoryItemStruct& InItem)
	{
		return InItem.ItemName == ItemName;
	});

	if (FoundEntry)
	{
		AOWSPlayerController* PlayerController = Cast<AOWSPlayerController>(GetController());

		Item->IconTexture = PlayerController->LoadTextureReference(FoundEntry->TextureToUseForIcon);
		Item->IconSlotWidth = FoundEntry->IconSlotWidth;
		Item->IconSlotHeight = FoundEntry->IconSlotHeight;
	}

	UOWSInventory* FoundInventory = GetHUDInventoryFromName(InventoryName);

	if (FoundInventory)
	{
		FoundInventory->AddItemToSlot_Internal(Item, InSlotNumber);
	}
}

bool AOWSCharacter::AddItemToLocalInventoryItems(const FString& ItemName, const bool ItemCanStack, const bool IsUsable, const bool IsConsumedOnUse, const int32 ItemTypeID,
	const FString& TextureToUseForIcon, const int32 IconSlotWidth, const int32 IconSlotHeight, const int32 ItemMeshID, const FString& CustomData)
{
	auto FoundEntry = LocalInventoryItems.FindByPredicate([&](FInventoryItemStruct& InItem)
	{
		return InItem.ItemName == ItemName;
	});

	if (FoundEntry)
		return false;

	FInventoryItemStruct tempItem;

	tempItem.ItemName = ItemName;
	tempItem.ItemCanStack = ItemCanStack;
	tempItem.IsUsable = IsUsable;
	tempItem.IsConsumedOnUse = IsConsumedOnUse;
	tempItem.ItemTypeID = ItemTypeID;
	tempItem.TextureToUseForIcon = TextureToUseForIcon;
	tempItem.IconSlotWidth = IconSlotWidth;
	tempItem.IconSlotHeight = IconSlotHeight;
	tempItem.ItemMeshID = ItemMeshID;
	tempItem.CustomData = CustomData;

	LocalInventoryItems.Add(tempItem);

	return true;
}

void AOWSCharacter::Client_AddItemToLocalInventoryItems_Implementation(const FString& ItemName, const bool ItemCanStack, const bool IsUsable, const bool IsConsumedOnUse, const int32 ItemTypeID,
	const FString& TextureToUseForIcon, const int32 IconSlotWidth, const int32 IconSlotHeight, const int32 ItemMeshID, const FString& CustomData)
{
	FInventoryItemStruct tempItem;

	tempItem.ItemName = ItemName;
	tempItem.ItemCanStack = ItemCanStack;
	tempItem.IsUsable = IsUsable;
	tempItem.IsConsumedOnUse = IsConsumedOnUse;
	tempItem.ItemTypeID = ItemTypeID;
	tempItem.TextureToUseForIcon = TextureToUseForIcon;
	tempItem.IconSlotWidth = IconSlotWidth;
	tempItem.IconSlotHeight = IconSlotHeight;
	tempItem.ItemMeshID = ItemMeshID;
	tempItem.CustomData = CustomData;

	LocalInventoryItems.Add(tempItem);
}

UOWSInventory* AOWSCharacter::GetHUDInventoryFromName(FName InventoryName)
{
	if (InventoriesToManage.Num() < 1)
		return nullptr;

	/*
	for (TArray<UOWSInventory*>::TConstIterator InventoryIter(InventoriesToManage); InventoryIter; ++InventoryIter)
	{
		if ((*InventoryIter)->InventoryName == InventoryName)
		{
			return (*InventoryIter);
		}
	}*/

	/*for (auto CurInventory : InventoriesToManage)
	{
		if (CurInventory->InventoryName == InventoryName)
		{
			return CurInventory;
		}
	}*/

	auto FoundEntry = InventoriesToManage.FindByPredicate([&](UOWSInventory* InItem)
	{
		//return InItem->Choices.Contains(InputString);
		return InItem != nullptr && InItem->InventoryName == InventoryName;
	});

	if (FoundEntry)
	{
		return (*FoundEntry);
	}
	else
	{
		return nullptr;
	}
}


FString AOWSCharacter::SerializeInventory(FName InventoryName)
{
	FString output = "";

	UOWSInventory* InventoryToSerialize = GetHUDInventoryFromName(InventoryName);

	if (InventoryToSerialize)
	{
		//Loop through the ItemStacks and serialize them to a string
		for (auto ItemStack : InventoryToSerialize->InventoryItemStacks)
		{
			AOWSInventoryItem* InventoryItem = ItemStack->GetTopItemFromStack();

			//Only save valid items
			if (InventoryItem)
			{
				output += InventoryItem->UniqueItemGUID.ToString() + "*" + FString::FromInt(ItemStack->SlotNumber) + "*" + FString::FromInt(ItemStack->InventoryItems.Num()) + "*" + FString::FromInt(InventoryItem->NumberOfUsesLeft) + "*" + FString::FromInt(InventoryItem->Condition);
				output += "|";
			}
		}

		//Remove the trailing "|"
		if (output.Len() > 1)
		{
			output = output.Left(output.Len() - 1);
		}
	}

	return output;
}


void AOWSCharacter::GetLifetimeReplicatedProps(TArray< FLifetimeProperty > & OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);

	// Replicate to everyone
	DOREPLIFETIME(AOWSCharacter, CharacterName);
	DOREPLIFETIME(AOWSCharacter, ClassName);	
	DOREPLIFETIME(AOWSCharacter, Gender);
	DOREPLIFETIME(AOWSCharacter, IsEnemy);
	DOREPLIFETIME(AOWSCharacter, CharacterLevel);
	DOREPLIFETIME(AOWSCharacter, XP);
	DOREPLIFETIME(AOWSCharacter, HitDice);
	DOREPLIFETIME(AOWSCharacter, HitDie);
	DOREPLIFETIME(AOWSCharacter, MaxHP);
	DOREPLIFETIME(AOWSCharacter, Wounds);
	DOREPLIFETIME(AOWSCharacter, Strength);
	DOREPLIFETIME(AOWSCharacter, Dexterity);
	DOREPLIFETIME(AOWSCharacter, Constitution);
	DOREPLIFETIME(AOWSCharacter, Intellect);
	DOREPLIFETIME(AOWSCharacter, Wisdom);
	DOREPLIFETIME(AOWSCharacter, Charisma);
	DOREPLIFETIME(AOWSCharacter, Spirit);
	DOREPLIFETIME(AOWSCharacter, Magic);
	DOREPLIFETIME(AOWSCharacter, Fortitude);
	DOREPLIFETIME(AOWSCharacter, Reflex);
	DOREPLIFETIME(AOWSCharacter, Willpower);
	DOREPLIFETIME(AOWSCharacter, TeamNumber);
	DOREPLIFETIME(AOWSCharacter, Perception);
	DOREPLIFETIME(AOWSCharacter, Acrobatics);
	DOREPLIFETIME(AOWSCharacter, Climb);
	DOREPLIFETIME(AOWSCharacter, Stealth);
	DOREPLIFETIME(AOWSCharacter, Thirst);
	DOREPLIFETIME(AOWSCharacter, Hunger);
	DOREPLIFETIME(AOWSCharacter, Gold);
	DOREPLIFETIME(AOWSCharacter, Score);
	DOREPLIFETIME(AOWSCharacter, MaxHealth);
	DOREPLIFETIME(AOWSCharacter, Health);
	DOREPLIFETIME(AOWSCharacter, HealthRegenRate);
	DOREPLIFETIME(AOWSCharacter, MaxMana);
	DOREPLIFETIME(AOWSCharacter, Mana);
	DOREPLIFETIME(AOWSCharacter, ManaRegenRate);
	DOREPLIFETIME(AOWSCharacter, MaxEnergy);
	DOREPLIFETIME(AOWSCharacter, Energy);
	DOREPLIFETIME(AOWSCharacter, EnergyRegenRate);
	DOREPLIFETIME(AOWSCharacter, MaxFatigue);
	DOREPLIFETIME(AOWSCharacter, Fatigue);
	DOREPLIFETIME(AOWSCharacter, FatigueRegenRate);
	DOREPLIFETIME(AOWSCharacter, MaxStamina);
	DOREPLIFETIME(AOWSCharacter, Stamina);
	DOREPLIFETIME(AOWSCharacter, StaminaRegenRate);
	DOREPLIFETIME(AOWSCharacter, MaxEndurance);
	DOREPLIFETIME(AOWSCharacter, Endurance);
	DOREPLIFETIME(AOWSCharacter, EnduranceRegenRate);
	DOREPLIFETIME(AOWSCharacter, Agility);
	DOREPLIFETIME(AOWSCharacter, BaseAttack);
	DOREPLIFETIME(AOWSCharacter, BaseAttackBonus);
	DOREPLIFETIME(AOWSCharacter, AttackPower);
	DOREPLIFETIME(AOWSCharacter, AttackSpeed);
	DOREPLIFETIME(AOWSCharacter, CritChance);
	DOREPLIFETIME(AOWSCharacter, CritMultiplier);
	DOREPLIFETIME(AOWSCharacter, Haste);
	DOREPLIFETIME(AOWSCharacter, SpellPower);
	DOREPLIFETIME(AOWSCharacter, SpellPenetration);
	DOREPLIFETIME(AOWSCharacter, Defense);
	DOREPLIFETIME(AOWSCharacter, Dodge);
	DOREPLIFETIME(AOWSCharacter, Parry);
	DOREPLIFETIME(AOWSCharacter, Avoidance);
	DOREPLIFETIME(AOWSCharacter, Versatility);
	DOREPLIFETIME(AOWSCharacter, Multishot);
	DOREPLIFETIME(AOWSCharacter, Initiative);
	DOREPLIFETIME(AOWSCharacter, NaturalArmor);
	DOREPLIFETIME(AOWSCharacter, PhysicalArmor);
	DOREPLIFETIME(AOWSCharacter, BonusArmor);
	DOREPLIFETIME(AOWSCharacter, ForceArmor);
	DOREPLIFETIME(AOWSCharacter, MagicArmor);
	DOREPLIFETIME(AOWSCharacter, Resistance);
	DOREPLIFETIME(AOWSCharacter, ReloadSpeed);

	DOREPLIFETIME(AOWSCharacter, PremiumCurrency);
	DOREPLIFETIME(AOWSCharacter, FreeCurrency);
	DOREPLIFETIME(AOWSCharacter, Silver);
	DOREPLIFETIME(AOWSCharacter, Copper);

	DOREPLIFETIME(AOWSCharacter, Range);
	DOREPLIFETIME(AOWSCharacter, Speed);

	DOREPLIFETIME(AOWSCharacter, IsAdmin);
	DOREPLIFETIME(AOWSCharacter, IsModerator);

	//DOREPLIFETIME_CONDITION(AOWSCharacter, InventoriesToManage, COND_OwnerOnly)
}