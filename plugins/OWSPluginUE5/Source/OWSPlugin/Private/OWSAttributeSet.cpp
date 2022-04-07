// Fill out your copyright notice in the Description page of Project Settings.

#include "OWSAttributeSet.h"
#include "Net/UnrealNetwork.h"
#include "GameplayTagContainer.h"
#include "GameplayEffect.h"
#include "GameplayTagsModule.h"
#include "GameplayEffectExtension.h"
#include "OWSCharacterWithAbilities.h"


UOWSAttributeSet::UOWSAttributeSet(const FObjectInitializer& ObjectInitializer)
	: Super(ObjectInitializer)
{
	MaxHealth = 100.f;
	MaxEnergy = 100.f;
	MaxMana = 100.f;
	MaxFatigue = 100.f;
	Health = 100.f;
	Energy = 100.f;
	Mana = 100.f;
	Fatigue = 0.f;

	EnergyRegenRate = 1.f;
	ManaRegenRate = 0.5f;

	Strength = 10;
	Dexterity = 10;
	Constitution = 10;
	Intellect = 10;
	Wisdom = 10;
	Charisma = 10;

	BaseAttack = 5;

	Dodge = 0.05f;
	CritChance = 0.1f;
	CritMultiplier = 2.0f;
	Defense = 0.03f;
}

bool UOWSAttributeSet::PreGameplayEffectExecute(struct FGameplayEffectModCallbackData &Data)
{
	static FProperty *HealthProperty = FindFieldChecked<FProperty>(UOWSAttributeSet::StaticClass(), GET_MEMBER_NAME_CHECKED(UOWSAttributeSet, Health));
	static FProperty *DamageProperty = FindFieldChecked<FProperty>(UOWSAttributeSet::StaticClass(), GET_MEMBER_NAME_CHECKED(UOWSAttributeSet, Damage));
	static FProperty* HealingProperty = FindFieldChecked<FProperty>(UOWSAttributeSet::StaticClass(), GET_MEMBER_NAME_CHECKED(UOWSAttributeSet, Healing));

	FProperty* ModifiedProperty = Data.EvaluatedData.Attribute.GetUProperty();

	// Is Damage about to be applied?
	if (DamageProperty == ModifiedProperty)
	{
		/*
		//Dodge chance
		if (Dodge > 0.f)
		{
			if (FMath::FRand() <= Dodge)
			{
				Data.EvaluatedData.Magnitude = 0.f;
			}
		}

		if (Data.EvaluatedData.Magnitude > 0.f)
		{
			const UOWSAttributeSet* SourceAttributes = Data.EffectSpec.GetContext().GetOriginalInstigatorAbilitySystemComponent()->GetSet<UOWSAttributeSet>();
			if (SourceAttributes && SourceAttributes->CritChance > 0.f)
			{
				if (FMath::FRand() <= SourceAttributes->CritChance)
				{
					//This is a Crit!
					Data.EvaluatedData.Magnitude *= SourceAttributes->CritMultiplier;
				}
			}

			Data.EvaluatedData.Magnitude *= (1.f - Defense);
		}*/

		AOWSCharacterWithAbilities* MyCharacter = CastChecked<AOWSCharacterWithAbilities>(GetOwningActor());
		float NewMagnitude = Data.EvaluatedData.Magnitude;
		bool IsCritical = false;
		const UOWSAttributeSet* SourceAttributes = Data.EffectSpec.GetContext().GetOriginalInstigatorAbilitySystemComponent()->GetSet<UOWSAttributeSet>();

		WhoAttackedUsLast = Cast<AOWSCharacter>(Data.EffectSpec.GetContext().GetOriginalInstigatorAbilitySystemComponent()->GetOwner());

		FGameplayTagContainer EffectTags;
		Data.EffectSpec.GetAllAssetTags(EffectTags);

		MyCharacter->CalculateUpdatedDamage(Data.EvaluatedData.Magnitude, SourceAttributes, EffectTags, NewMagnitude, IsCritical);

		Data.EvaluatedData.Magnitude = NewMagnitude;

		if (NewMagnitude > 0.f)
		{
			AOWSCharacterWithAbilities* DamagedCharacter = Cast<AOWSCharacterWithAbilities>(GetOwningActor());
			if (DamagedCharacter)
			{
				DamagedCharacter->OnTakeDamage(WhoAttackedUsLast, NewMagnitude, IsCritical);

				if (WhoAttackedUsLast)
				{
					AOWSCharacterWithAbilities* AttackingCharacter = Cast<AOWSCharacterWithAbilities>(WhoAttackedUsLast);

					if (AttackingCharacter)
					{
						AttackingCharacter->OnInflictDamage(DamagedCharacter, NewMagnitude, IsCritical);
					}
				}
			}
		}
	}

/*
#if 0
	static UProperty *HealthProperty = FindFieldChecked<UProperty>(UAbilitySystemTestAttributeSet::StaticClass(), GET_MEMBER_NAME_CHECKED(UAbilitySystemTestAttributeSet, Health));
	static UProperty *DamageProperty = FindFieldChecked<UProperty>(UAbilitySystemTestAttributeSet::StaticClass(), GET_MEMBER_NAME_CHECKED(UAbilitySystemTestAttributeSet, Damage));

	// In this function, our GameplayEffect mod has been evaluated. We have a magnitude and a Tags collection that we can still modify before it is applied.
	// We also still have the Aggregation data that calculated Data.EvaluatedData. If we really needed to, we could look at this, remove or change things at the aggregator level, and reevaluate ourselves.
	// But that would be considered very advanced/rare.

	UProperty *ModifiedProperty = Data.ModifierSpec.Info.Attribute.GetUProperty();

	// Is Damage about to be applied?
	if (DamageProperty == ModifiedProperty)
	{
		// Can the target dodge this completely?
		if (DodgeChance > 0.f)
		{
			if (FMath::FRand() <= DodgeChance)
			{
				// Dodge!
				Data.EvaluatedData.Magnitude = 0.f;
				Data.EvaluatedData.Tags.AddTag(FGameplayTag::RequestGameplayTag(FName(TEXT("Dodged"))));

				// How dodge is handled will be game dependant. There are a few options I think of:
				// -We still apply 0 damage, but tag it as Dodged. The GameplayCue system could pick up on this and play a visual effect. The combat log could pick up in and print it out too.
				// -We throw out this GameplayEffect right here, and apply another GameplayEffect for 'Dodge' it wouldn't modify an attribute but could trigger gameplay cues, it could serve as a 'cooldown' for dodge
				//		if the game wanted rules like 'you can't dodge more than once every .5 seconds', etc.
			}
		}

		if (Data.EvaluatedData.Magnitude > 0.f)
		{
			// Check the source - does he have Crit?
			const UAbilitySystemTestAttributeSet* SourceAttributes = Data.EffectSpec.EffectContext.GetOriginalInstigatorAbilitySystemComponent()->GetSet<UAbilitySystemTestAttributeSet>();
			if (SourceAttributes && SourceAttributes->CritChance > 0.f)
			{
				if (FMath::FRand() <= SourceAttributes->CritChance)
				{
					// Crit!
					Data.EvaluatedData.Magnitude *= SourceAttributes->CritMultiplier;
					Data.EvaluatedData.Tags.AddTag(FGameplayTag::RequestGameplayTag(FName(TEXT("Damage.Crit"))));
				}
			}

			// Now apply armor reduction
			if (Data.EvaluatedData.Tags.HasTag(FGameplayTag::RequestGameplayTag(FName(TEXT("Damage.Physical")))))
			{
				// This is a trivial/naive implementation of armor. It assumes the rmorDamageReduction is an actual % to reduce physics damage by.
				// Real games would probably use armor rating as an attribute and calculate a % here based on the damage source's level, etc.
				Data.EvaluatedData.Magnitude *= (1.f - ArmorDamageReduction);
				Data.EvaluatedData.Tags.AddTag(FGameplayTag::RequestGameplayTag(FName(TEXT("Damage.Mitigated.Armor"))));
			}
		}

		// At this point, the Magnitude of the applied damage may have been modified by us. We still do the translation to Health in UAbilitySystemTestAttributeSet::PostAttributeModify.
	}

#endif
*/
	return true;
}

void UOWSAttributeSet::PostGameplayEffectExecute(const struct FGameplayEffectModCallbackData &Data)
{
	static FProperty* DamageProperty = FindFieldChecked<FProperty>(UOWSAttributeSet::StaticClass(), GET_MEMBER_NAME_CHECKED(UOWSAttributeSet, Damage));
	static FProperty* HealingProperty = FindFieldChecked<FProperty>(UOWSAttributeSet::StaticClass(), GET_MEMBER_NAME_CHECKED(UOWSAttributeSet, Healing));
	static FProperty* EnergyProperty = FindFieldChecked<FProperty>(UOWSAttributeSet::StaticClass(), GET_MEMBER_NAME_CHECKED(UOWSAttributeSet, Energy));
	static FProperty* ManaProperty = FindFieldChecked<FProperty>(UOWSAttributeSet::StaticClass(), GET_MEMBER_NAME_CHECKED(UOWSAttributeSet, Mana));
	static FProperty* FatigueProperty = FindFieldChecked<FProperty>(UOWSAttributeSet::StaticClass(), GET_MEMBER_NAME_CHECKED(UOWSAttributeSet, Fatigue));
	static FProperty* StaminaProperty = FindFieldChecked<FProperty>(UOWSAttributeSet::StaticClass(), GET_MEMBER_NAME_CHECKED(UOWSAttributeSet, Stamina));
	static FProperty* EnduranceProperty = FindFieldChecked<FProperty>(UOWSAttributeSet::StaticClass(), GET_MEMBER_NAME_CHECKED(UOWSAttributeSet, Endurance));
	FProperty* ModifiedProperty = Data.EvaluatedData.Attribute.GetUProperty();

	//Data.EffectSpec.CapturedTargetTags
	

	// What property was modified?
	if (DamageProperty == ModifiedProperty)
	{
		// Treat damage as minus health
		SetHealth(FMath::Clamp(GetHealth() - Damage.GetCurrentValue(), 0.f, GetMaxHealth()));
		Damage = 0.f;
	}

	if (HealingProperty == ModifiedProperty)
	{
		SetHealth(FMath::Clamp(GetHealth() + Healing.GetCurrentValue(), 0.f, GetMaxHealth()));
		Healing = 0.f;
	}

	if (EnergyProperty == ModifiedProperty)
	{
		SetEnergy(FMath::Clamp(GetEnergy(), 0.f, GetMaxEnergy()));
		//Energy.SetBaseValue(FMath::Clamp(Energy.GetCurrentValue(), 0.f, MaxEnergy.GetCurrentValue()));
		//Energy.SetCurrentValue(FMath::Clamp(Energy.GetCurrentValue(), 0.f, MaxEnergy.GetCurrentValue()));
		//Energy = FMath::Clamp(Energy, 0.f, MaxEnergy);
	}

	if (ManaProperty == ModifiedProperty)
	{
		SetMana(FMath::Clamp(GetMana(), 0.f, GetMaxMana()));
		//Mana.SetBaseValue(FMath::Clamp(Mana.GetCurrentValue(), 0.f, Mana.GetCurrentValue()));
		//Mana.SetCurrentValue(FMath::Clamp(Mana.GetCurrentValue(), 0.f, MaxMana.GetCurrentValue()));
		//Mana = FMath::Clamp(Mana, 0.f, MaxMana);
	}

	if (FatigueProperty == ModifiedProperty)
	{
		SetFatigue(FMath::Clamp(GetFatigue(), 0.f, GetMaxFatigue()));
		//Fatigue.SetCurrentValue(FMath::Clamp(Fatigue.GetCurrentValue(), 0.f, MaxFatigue.GetCurrentValue()));
		//Fatigue = FMath::Clamp(Fatigue, 0.f, MaxFatigue);
	}

	if (StaminaProperty == ModifiedProperty)
	{
		SetStamina(FMath::Clamp(GetStamina(), 0.f, GetMaxStamina()));
		//Stamina.SetCurrentValue(FMath::Clamp(Stamina.GetCurrentValue(), 0.f, MaxStamina.GetCurrentValue()));
		//Stamina = FMath::Clamp(Stamina, 0.f, MaxStamina);
	}

	if (EnduranceProperty == ModifiedProperty)
	{
		SetEndurance(FMath::Clamp(GetEndurance(), 0.f, GetMaxEndurance()));
		//Endurance.SetCurrentValue(FMath::Clamp(Endurance.GetCurrentValue(), 0.f, MaxEndurance.GetCurrentValue()));
		//Endurance = FMath::Clamp(Endurance, 0.f, MaxEndurance);
	}

	//Dead
	if (GetHealth() <= 0.f)
	{
		FName DeadTagName = FName(TEXT("Combat.State.Dead"));
		FGameplayTag DeadTag = UGameplayTagsManager::Get().RequestGameplayTag(DeadTagName);

		AOWSCharacterWithAbilities* MyCharacter = Cast<AOWSCharacterWithAbilities>(GetOwningActor());
		if (MyCharacter && !MyCharacter->GetAbilitySystemComponent()->HasAnyMatchingGameplayTags(FGameplayTagContainer(DeadTag)))
		{
			UE_LOG(LogTemp, Warning, TEXT("DEAD!"));
			MyCharacter->OnDeath(WhoAttackedUsLast);
		}
	}

	/*
	static UProperty* HealthProperty = FindFieldChecked<UProperty>(UOWSAttributeSet::StaticClass(), GET_MEMBER_NAME_CHECKED(UOWSAttributeSet, Health));
	static UProperty* ManaProperty = FindFieldChecked<UProperty>(UOWSAttributeSet::StaticClass(), GET_MEMBER_NAME_CHECKED(UOWSAttributeSet, Mana));

	UProperty* ModifiedProperty = Data.EvaluatedData.Attribute.GetUProperty();

	
	if (HealthProperty == ModifiedProperty)
	{
		if (GEngine)
			GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Green, FString::Printf(TEXT("Health: %f"), Health));
	}

	if (ManaProperty == ModifiedProperty)
	{
		if (GEngine)
			GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Blue, FString::Printf(TEXT("Mana: %f"), Mana));
	}	
	*/

	/*	static UProperty* HealthProperty = FindFieldChecked<UProperty>(UOWSAttributeSet::StaticClass(), GET_MEMBER_NAME_CHECKED(UOWSAttributeSet, Health));
	static UProperty* ManaProperty = FindFieldChecked<UProperty>(UOWSAttributeSet::StaticClass(), GET_MEMBER_NAME_CHECKED(UOWSAttributeSet, Mana));
	static UProperty* FatigueProperty = FindFieldChecked<UProperty>(UOWSAttributeSet::StaticClass(), GET_MEMBER_NAME_CHECKED(UOWSAttributeSet, Fatigue));
	//static UProperty* DamageProperty = FindFieldChecked<UProperty>(UOWSAttributeSet::StaticClass(), GET_MEMBER_NAME_CHECKED(UOWSAttributeSet, Damage));

	UProperty* ModifiedProperty = Data.EvaluatedData.Attribute.GetUProperty();

	UAbilitySystemComponent *Source = Data.EffectSpec.GetContext().GetOriginalInstigatorAbilitySystemComponent();

	if (ManaProperty == ModifiedProperty && (Mana < 0 || Mana > MaxMana) )
	{
	UGameplayEffect * LocalManaRestore = RestoreManaGameplayEffect;
	LocalHealthRestore = NewObject<UGameplayEffect>(GetTransientPackage(), FName(TEXT("ManaRestore")));
	LocalHealthRestore->Modifiers.SetNum(1);
	LocalHealthRestore->Modifiers[0].ModifierOp = EGameplayModOp::Additive;
	LocalHealthRestore->Modifiers[0].Attribute.SetUProperty(HealthProperty);
	LocalHealthRestore->DurationPolicy = EGameplayEffectDurationType::Instant;
	LocalHealthRestore->Period.Value = UGameplayEffect::NO_PERIOD;

	if (!LocalManaRestore)
	{
	if (Mana < 0)
	{
	LocalHealthRestore->Modifiers[0].Magnitude.SetValue(0);
	}
	if (Mana > MaxMana)
	{
	LocalHealthRestore->Modifiers[0].Magnitude.SetValue(MaxMana);
	}
	}
	}

	if (FatigueProperty == ModifiedProperty)
	{
	UE_LOG(LogTemp, Error, TEXT("%f"), Fatigue);
	if (Fatigue < 0)
	{
	Fatigue = 0;
	}
	if (Fatigue > MaxFatigue)
	{
	Fatigue = MaxFatigue;
	}
	}*/

	/*UProperty* ModifiedProperty = Data.EvaluatedData.Attribute.GetUProperty();

	// What property was modified?
	if (DamageProperty == ModifiedProperty)
	{
	// Anytime Damage is applied with 'Damage.Fire' tag, there is a chance to apply a burning DOT
	if (Data.EffectSpec.CapturedSourceTags.GetAggregatedTags()->HasTag(FGameplayTag::RequestGameplayTag(FName(TEXT("FireDamage")))))
	{
	// Logic to rand() a burning DOT, if successful, apply DOT GameplayEffect to the target
	}

	// Treat damage as minus health
	Health -= Damage;
	Damage = 0.f;

	// Check for Death?
	//  -This could be defined here or at the actor level.
	//  -Doing it here makes a lot of sense to me, but we have legacy code in ::TakeDamage function, so some games may just want to punt to that pipeline from here.
	}*/
}


void UOWSAttributeSet::GetLifetimeReplicatedProps(TArray< FLifetimeProperty > & OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);


	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, HitDie, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Wounds, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Thirst, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Hunger, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, MaxHealth, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Health, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, HealthRegenRate, COND_None, REPNOTIFY_Always);	
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, MaxMana, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Mana, COND_None, REPNOTIFY_Always);	
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, ManaRegenRate, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, MaxEnergy, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Energy, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, EnergyRegenRate, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, MaxFatigue, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Fatigue, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, FatigueRegenRate, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, MaxStamina, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Stamina, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, StaminaRegenRate, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, MaxEndurance, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Endurance, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, EnduranceRegenRate, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Strength, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Dexterity, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Constitution, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Intellect, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Wisdom, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Charisma, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Agility, COND_None, REPNOTIFY_Always);	
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Spirit, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Magic, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Fortitude, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Reflex, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Willpower, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, BaseAttack, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, BaseAttackBonus, COND_None, REPNOTIFY_Always);
	
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, AttackPower, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, AttackSpeed, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, CritChance, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, CritMultiplier, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Haste, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, SpellPower, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, SpellPenetration, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Defense, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Dodge, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Parry, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Avoidance, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Versatility, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Multishot, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Initiative, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, NaturalArmor, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, PhysicalArmor, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, BonusArmor, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, ForceArmor, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, MagicArmor, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Resistance, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, ReloadSpeed, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Range, COND_None, REPNOTIFY_Always);
	DOREPLIFETIME_CONDITION_NOTIFY(UOWSAttributeSet, Speed, COND_None, REPNOTIFY_Always);
	
}
