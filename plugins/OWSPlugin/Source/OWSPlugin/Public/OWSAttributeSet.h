// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "UObject/ObjectMacros.h"
#include "AttributeSet.h"
#include "AbilitySystemComponent.h"
#include "OWSCharacter.h"
#include "OWSAttributeSet.generated.h"

#define ATTRIBUTE_ACCESSORS(ClassName, PropertyName) \
    GAMEPLAYATTRIBUTE_PROPERTY_GETTER(ClassName, PropertyName) \
    GAMEPLAYATTRIBUTE_VALUE_GETTER(PropertyName) \
    GAMEPLAYATTRIBUTE_VALUE_SETTER(PropertyName) \
    GAMEPLAYATTRIBUTE_VALUE_INITTER(PropertyName)

#define OWS_GAMEPLAYATTRIBUTE_VALUE_GETTER(PropertyName) \
	float OWSGet##PropertyName() const \
	{ \
		return PropertyName.GetCurrentValue(); \
	}

#define OWS_GAMEPLAYATTRIBUTE_VALUE_SETTER(PropertyName) \
	void OWSSet##PropertyName(float NewVal) \
	{ \
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent(); \
		if (ensure(AbilityComp)) \
		{ \
			AbilityComp->SetNumericAttributeBase(Get##PropertyName##Attribute(), NewVal); \
		}; \
	}

#define OWS_GAMEPLAYATTRIBUTE_VALUE_INITTER(PropertyName) \
	void OWSInit##PropertyName(float NewVal) \
	{ \
		PropertyName.SetBaseValue(NewVal); \
		PropertyName.SetCurrentValue(NewVal); \
	}

/**
 * 
 */
UCLASS(EditInlineNew)
class OWSPLUGIN_API UOWSAttributeSet : public UAttributeSet
{
	GENERATED_UCLASS_BODY()
	
protected:
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Internal")
		AOWSCharacter* WhoAttackedUsLast;
	
public:

	/* BEGIN AUTO GENERATED ATTRIBUTES */

	
	//HitDie
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_HitDie, Category = RPGAttributes)
		FGameplayAttributeData HitDie;
	UFUNCTION()
		void OnRep_HitDie(const FGameplayAttributeData& OldHitDie) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, HitDie, OldHitDie); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, HitDie)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetHitDie() const
	{
		return HitDie.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetHitDie(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetHitDieAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitHitDie(float NewVal)
	{
		HitDie.SetBaseValue(NewVal);
		HitDie.SetCurrentValue(NewVal);
	}

	//Wounds
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Wounds, Category = RPGAttributes)
		FGameplayAttributeData Wounds;
	UFUNCTION()
		void OnRep_Wounds(const FGameplayAttributeData& OldWounds) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Wounds, OldWounds); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Wounds)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetWounds() const
	{
		return Wounds.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetWounds(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetWoundsAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitWounds(float NewVal)
	{
		Wounds.SetBaseValue(NewVal);
		Wounds.SetCurrentValue(NewVal);
	}

	//Thirst
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Thirst, Category = RPGAttributes)
		FGameplayAttributeData Thirst;
	UFUNCTION()
		void OnRep_Thirst(const FGameplayAttributeData& OldThirst) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Thirst, OldThirst); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Thirst)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetThirst() const
	{
		return Thirst.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetThirst(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetThirstAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitThirst(float NewVal)
	{
		Thirst.SetBaseValue(NewVal);
		Thirst.SetCurrentValue(NewVal);
	}

	//Hunger
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Hunger, Category = RPGAttributes)
		FGameplayAttributeData Hunger;
	UFUNCTION()
		void OnRep_Hunger(const FGameplayAttributeData& OldHunger) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Hunger, OldHunger); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Hunger)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetHunger() const
	{
		return Hunger.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetHunger(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetHungerAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitHunger(float NewVal)
	{
		Hunger.SetBaseValue(NewVal);
		Hunger.SetCurrentValue(NewVal);
	}

	//Health
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Health, Category = RPGAttributes, meta = (HideFromModifiers))
		FGameplayAttributeData Health;
	UFUNCTION()
		void OnRep_Health(const FGameplayAttributeData& OldHealth) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Health, OldHealth); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Health)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetHealth() const
	{
		return Health.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetHealth(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetHealthAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitHealth(float NewVal)
	{
		Health.SetBaseValue(NewVal);
		Health.SetCurrentValue(NewVal);
	}

	//MaxHealth
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_MaxHealth, Category = RPGAttributes)
		FGameplayAttributeData MaxHealth;
	UFUNCTION()
		void OnRep_MaxHealth(const FGameplayAttributeData& OldMaxHealth) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, MaxHealth, OldMaxHealth); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, MaxHealth)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetMaxHealth() const
	{
		return MaxHealth.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetMaxHealth(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetMaxHealthAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitMaxHealth(float NewVal)
	{
		MaxHealth.SetBaseValue(NewVal);
		MaxHealth.SetCurrentValue(NewVal);
	}

	//HealthRegenRate
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_HealthRegenRate, Category = RPGAttributes)
		FGameplayAttributeData HealthRegenRate;
	UFUNCTION()
		void OnRep_HealthRegenRate(const FGameplayAttributeData& OldHealthRegenRate) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, HealthRegenRate, OldHealthRegenRate); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, HealthRegenRate)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetHealthRegenRate() const
	{
		return HealthRegenRate.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetHealthRegenRate(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetHealthRegenRateAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitHealthRegenRate(float NewVal)
	{
		HealthRegenRate.SetBaseValue(NewVal);
		HealthRegenRate.SetCurrentValue(NewVal);
	}

	//Mana
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Mana, Category = RPGAttributes)
		FGameplayAttributeData Mana;
	UFUNCTION()
		void OnRep_Mana(const FGameplayAttributeData& OldMana) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Mana, OldMana); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Mana)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetMana() const
	{
		return Mana.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetMana(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetManaAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitMana(float NewVal)
	{
		Mana.SetBaseValue(NewVal);
		Mana.SetCurrentValue(NewVal);
	}

	//MaxMana
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_MaxMana, Category = RPGAttributes)
		FGameplayAttributeData MaxMana;
	UFUNCTION()
		void OnRep_MaxMana(const FGameplayAttributeData& OldMaxMana) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, MaxMana, OldMaxMana); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, MaxMana)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetMaxMana() const
	{
		return MaxMana.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetMaxMana(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetMaxManaAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitMaxMana(float NewVal)
	{
		MaxMana.SetBaseValue(NewVal);
		MaxMana.SetCurrentValue(NewVal);
	}

	//ManaRegenRate
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_ManaRegenRate, Category = RPGAttributes)
		FGameplayAttributeData ManaRegenRate;
	UFUNCTION()
		void OnRep_ManaRegenRate(const FGameplayAttributeData& OldManaRegenRate) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, ManaRegenRate, OldManaRegenRate); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, ManaRegenRate)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetManaRegenRate() const
	{
		return ManaRegenRate.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetManaRegenRate(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetManaRegenRateAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitManaRegenRate(float NewVal)
	{
		ManaRegenRate.SetBaseValue(NewVal);
		ManaRegenRate.SetCurrentValue(NewVal);
	}

	//Energy
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Energy, Category = RPGAttributes)
		FGameplayAttributeData Energy;
	UFUNCTION()
		void OnRep_Energy(const FGameplayAttributeData& OldEnergy) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Energy, OldEnergy); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Energy)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetEnergy() const
	{
		return Energy.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetEnergy(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetEnergyAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitEnergy(float NewVal)
	{
		Energy.SetBaseValue(NewVal);
		Energy.SetCurrentValue(NewVal);
	}

	//MaxEnergy
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_MaxEnergy, Category = RPGAttributes)
		FGameplayAttributeData MaxEnergy;
	UFUNCTION()
		void OnRep_MaxEnergy(const FGameplayAttributeData& OldMaxEnergy) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, MaxEnergy, OldMaxEnergy); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, MaxEnergy)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetMaxEnergy() const
	{
		return MaxEnergy.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetMaxEnergy(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetMaxEnergyAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitMaxEnergy(float NewVal)
	{
		MaxEnergy.SetBaseValue(NewVal);
		MaxEnergy.SetCurrentValue(NewVal);
	}

	//EnergyRegenRate
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_EnergyRegenRate, Category = RPGAttributes)
		FGameplayAttributeData EnergyRegenRate;
	UFUNCTION()
		void OnRep_EnergyRegenRate(const FGameplayAttributeData& OldEnergyRegenRate) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, EnergyRegenRate, OldEnergyRegenRate); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, EnergyRegenRate)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetEnergyRegenRate() const
	{
		return EnergyRegenRate.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetEnergyRegenRate(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetEnergyRegenRateAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitEnergyRegenRate(float NewVal)
	{
		EnergyRegenRate.SetBaseValue(NewVal);
		EnergyRegenRate.SetCurrentValue(NewVal);
	}

	//Fatigue
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Fatigue, Category = RPGAttributes)
		FGameplayAttributeData Fatigue;
	UFUNCTION()
		void OnRep_Fatigue(const FGameplayAttributeData& OldFatigue) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Fatigue, OldFatigue); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Fatigue)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetFatigue() const
	{
		return Fatigue.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetFatigue(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetFatigueAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitFatigue(float NewVal)
	{
		Fatigue.SetBaseValue(NewVal);
		Fatigue.SetCurrentValue(NewVal);
	}

	//MaxFatigue
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_MaxFatigue, Category = RPGAttributes)
		FGameplayAttributeData MaxFatigue;
	UFUNCTION()
		void OnRep_MaxFatigue(const FGameplayAttributeData& OldMaxFatigue) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, MaxFatigue, OldMaxFatigue); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, MaxFatigue)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetMaxFatigue() const
	{
		return MaxFatigue.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetMaxFatigue(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetMaxFatigueAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitMaxFatigue(float NewVal)
	{
		MaxFatigue.SetBaseValue(NewVal);
		MaxFatigue.SetCurrentValue(NewVal);
	}

	//FatigueRegenRate
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_FatigueRegenRate, Category = RPGAttributes)
		FGameplayAttributeData FatigueRegenRate;
	UFUNCTION()
		void OnRep_FatigueRegenRate(const FGameplayAttributeData& OldFatigueRegenRate) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, FatigueRegenRate, OldFatigueRegenRate); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, FatigueRegenRate)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetFatigueRegenRate() const
	{
		return FatigueRegenRate.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetFatigueRegenRate(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetFatigueRegenRateAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitFatigueRegenRate(float NewVal)
	{
		FatigueRegenRate.SetBaseValue(NewVal);
		FatigueRegenRate.SetCurrentValue(NewVal);
	}

	//Stamina
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Stamina, Category = RPGAttributes)
		FGameplayAttributeData Stamina;
	UFUNCTION()
		void OnRep_Stamina(const FGameplayAttributeData& OldStamina) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Stamina, OldStamina); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Stamina)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetStamina() const
	{
		return Stamina.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetStamina(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetStaminaAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitStamina(float NewVal)
	{
		Stamina.SetBaseValue(NewVal);
		Stamina.SetCurrentValue(NewVal);
	}

	//MaxStamina
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_MaxStamina, Category = RPGAttributes)
		FGameplayAttributeData MaxStamina;
	UFUNCTION()
		void OnRep_MaxStamina(const FGameplayAttributeData& OldMaxStamina) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, MaxStamina, OldMaxStamina); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, MaxStamina)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetMaxStamina() const
	{
		return MaxStamina.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetMaxStamina(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetMaxStaminaAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitMaxStamina(float NewVal)
	{
		MaxStamina.SetBaseValue(NewVal);
		MaxStamina.SetCurrentValue(NewVal);
	}

	//StaminaRegenRate
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_StaminaRegenRate, Category = RPGAttributes)
		FGameplayAttributeData StaminaRegenRate;
	UFUNCTION()
		void OnRep_StaminaRegenRate(const FGameplayAttributeData& OldStaminaRegenRate) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, StaminaRegenRate, OldStaminaRegenRate); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, StaminaRegenRate)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetStaminaRegenRate() const
	{
		return StaminaRegenRate.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetStaminaRegenRate(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetStaminaRegenRateAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitStaminaRegenRate(float NewVal)
	{
		StaminaRegenRate.SetBaseValue(NewVal);
		StaminaRegenRate.SetCurrentValue(NewVal);
	}

	//Endurance
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Endurance, Category = RPGAttributes)
		FGameplayAttributeData Endurance;
	UFUNCTION()
		void OnRep_Endurance(const FGameplayAttributeData& OldEndurance) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Endurance, OldEndurance); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Endurance)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetEndurance() const
	{
		return Endurance.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetEndurance(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetEnduranceAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitEndurance(float NewVal)
	{
		Endurance.SetBaseValue(NewVal);
		Endurance.SetCurrentValue(NewVal);
	}

	//MaxEndurance
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_MaxEndurance, Category = RPGAttributes)
		FGameplayAttributeData MaxEndurance;
	UFUNCTION()
		void OnRep_MaxEndurance(const FGameplayAttributeData& OldMaxEndurance) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, MaxEndurance, OldMaxEndurance); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, MaxEndurance)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetMaxEndurance() const
	{
		return MaxEndurance.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetMaxEndurance(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetMaxEnduranceAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitMaxEndurance(float NewVal)
	{
		MaxEndurance.SetBaseValue(NewVal);
		MaxEndurance.SetCurrentValue(NewVal);
	}

	//EnduranceRegenRate
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_EnduranceRegenRate, Category = RPGAttributes)
		FGameplayAttributeData EnduranceRegenRate;
	UFUNCTION()
		void OnRep_EnduranceRegenRate(const FGameplayAttributeData& OldEnduranceRegenRate) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, EnduranceRegenRate, OldEnduranceRegenRate); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, EnduranceRegenRate)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetEnduranceRegenRate() const
	{
		return EnduranceRegenRate.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetEnduranceRegenRate(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetEnduranceRegenRateAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitEnduranceRegenRate(float NewVal)
	{
		EnduranceRegenRate.SetBaseValue(NewVal);
		EnduranceRegenRate.SetCurrentValue(NewVal);
	}

	//Strength
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Strength, Category = RPGAttributes)
		FGameplayAttributeData Strength;
	UFUNCTION()
		void OnRep_Strength(const FGameplayAttributeData& OldStrength) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Strength, OldStrength); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Strength)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetStrength() const
	{
		return Strength.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetStrength(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetStrengthAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitStrength(float NewVal)
	{
		Strength.SetBaseValue(NewVal);
		Strength.SetCurrentValue(NewVal);
	}

	//Dexterity
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Dexterity, Category = RPGAttributes)
		FGameplayAttributeData Dexterity;
	UFUNCTION()
		void OnRep_Dexterity(const FGameplayAttributeData& OldDexterity) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Dexterity, OldDexterity); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Dexterity)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetDexterity() const
	{
		return Dexterity.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetDexterity(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetDexterityAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitDexterity(float NewVal)
	{
		Dexterity.SetBaseValue(NewVal);
		Dexterity.SetCurrentValue(NewVal);
	}

	//Constitution
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Constitution, Category = RPGAttributes)
		FGameplayAttributeData Constitution;
	UFUNCTION()
		void OnRep_Constitution(const FGameplayAttributeData& OldConstitution) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Constitution, OldConstitution); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Constitution)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetConstitution() const
	{
		return Constitution.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetConstitution(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetConstitutionAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitConstitution(float NewVal)
	{
		Constitution.SetBaseValue(NewVal);
		Constitution.SetCurrentValue(NewVal);
	}

	//Intellect
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Intellect, Category = RPGAttributes)
		FGameplayAttributeData Intellect;
	UFUNCTION()
		void OnRep_Intellect(const FGameplayAttributeData& OldIntellect) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Intellect, OldIntellect); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Intellect)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetIntellect() const
	{
		return Intellect.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetIntellect(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetIntellectAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitIntellect(float NewVal)
	{
		Intellect.SetBaseValue(NewVal);
		Intellect.SetCurrentValue(NewVal);
	}

	//Wisdom
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Wisdom, Category = RPGAttributes)
		FGameplayAttributeData Wisdom;
	UFUNCTION()
		void OnRep_Wisdom(const FGameplayAttributeData& OldWisdom) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Wisdom, OldWisdom); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Wisdom)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetWisdom() const
	{
		return Wisdom.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetWisdom(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetWisdomAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitWisdom(float NewVal)
	{
		Wisdom.SetBaseValue(NewVal);
		Wisdom.SetCurrentValue(NewVal);
	}

	//Charisma
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Charisma, Category = RPGAttributes)
		FGameplayAttributeData Charisma;
	UFUNCTION()
		void OnRep_Charisma(const FGameplayAttributeData& OldCharisma) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Charisma, OldCharisma); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Charisma)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetCharisma() const
	{
		return Charisma.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetCharisma(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetCharismaAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitCharisma(float NewVal)
	{
		Charisma.SetBaseValue(NewVal);
		Charisma.SetCurrentValue(NewVal);
	}

	//Agility
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Agility, Category = RPGAttributes)
		FGameplayAttributeData Agility;
	UFUNCTION()
		void OnRep_Agility(const FGameplayAttributeData& OldAgility) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Agility, OldAgility); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Agility)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetAgility() const
	{
		return Agility.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetAgility(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetAgilityAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitAgility(float NewVal)
	{
		Agility.SetBaseValue(NewVal);
		Agility.SetCurrentValue(NewVal);
	}

	//Spirit
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Spirit, Category = RPGAttributes)
		FGameplayAttributeData Spirit;
	UFUNCTION()
		void OnRep_Spirit(const FGameplayAttributeData& OldSpirit) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Spirit, OldSpirit); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Spirit)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetSpirit() const
	{
		return Spirit.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetSpirit(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetSpiritAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitSpirit(float NewVal)
	{
		Spirit.SetBaseValue(NewVal);
		Spirit.SetCurrentValue(NewVal);
	}

	//Magic
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Magic, Category = RPGAttributes)
		FGameplayAttributeData Magic;
	UFUNCTION()
		void OnRep_Magic(const FGameplayAttributeData& OldMagic) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Magic, OldMagic); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Magic)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetMagic() const
	{
		return Magic.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetMagic(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetMagicAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitMagic(float NewVal)
	{
		Magic.SetBaseValue(NewVal);
		Magic.SetCurrentValue(NewVal);
	}

	//Fortitude
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Fortitude, Category = RPGAttributes)
		FGameplayAttributeData Fortitude;
	UFUNCTION()
		void OnRep_Fortitude(const FGameplayAttributeData& OldFortitude) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Fortitude, OldFortitude); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Fortitude)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetFortitude() const
	{
		return Fortitude.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetFortitude(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetFortitudeAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitFortitude(float NewVal)
	{
		Fortitude.SetBaseValue(NewVal);
		Fortitude.SetCurrentValue(NewVal);
	}

	//Reflex
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Reflex, Category = RPGAttributes)
		FGameplayAttributeData Reflex;
	UFUNCTION()
		void OnRep_Reflex(const FGameplayAttributeData& OldReflex) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Reflex, OldReflex); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Reflex)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetReflex() const
	{
		return Reflex.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetReflex(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetReflexAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitReflex(float NewVal)
	{
		Reflex.SetBaseValue(NewVal);
		Reflex.SetCurrentValue(NewVal);
	}

	//Willpower
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Willpower, Category = RPGAttributes)
		FGameplayAttributeData Willpower;
	UFUNCTION()
		void OnRep_Willpower(const FGameplayAttributeData& OldWillpower) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Willpower, OldWillpower); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Willpower)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetWillpower() const
	{
		return Willpower.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetWillpower(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetWillpowerAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitWillpower(float NewVal)
	{
		Willpower.SetBaseValue(NewVal);
		Willpower.SetCurrentValue(NewVal);
	}

	//BaseAttack
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_BaseAttack, Category = RPGAttributes)
		FGameplayAttributeData BaseAttack;
	UFUNCTION()
		void OnRep_BaseAttack(const FGameplayAttributeData& OldBaseAttack) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, BaseAttack, OldBaseAttack); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, BaseAttack)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetBaseAttack() const
	{
		return BaseAttack.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetBaseAttack(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetBaseAttackAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitBaseAttack(float NewVal)
	{
		BaseAttack.SetBaseValue(NewVal);
		BaseAttack.SetCurrentValue(NewVal);
	}

	//BaseAttackBonus
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_BaseAttackBonus, Category = RPGAttributes)
		FGameplayAttributeData BaseAttackBonus;
	UFUNCTION()
		void OnRep_BaseAttackBonus(const FGameplayAttributeData& OldBaseAttackBonus) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, BaseAttackBonus, OldBaseAttackBonus); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, BaseAttackBonus)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetBaseAttackBonus() const
	{
		return BaseAttackBonus.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetBaseAttackBonus(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetBaseAttackBonusAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitBaseAttackBonus(float NewVal)
	{
		BaseAttackBonus.SetBaseValue(NewVal);
		BaseAttackBonus.SetCurrentValue(NewVal);
	}

	//AttackPower
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_AttackPower, Category = RPGAttributes)
		FGameplayAttributeData AttackPower;
	UFUNCTION()
		void OnRep_AttackPower(const FGameplayAttributeData& OldAttackPower) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, AttackPower, OldAttackPower); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, AttackPower)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetAttackPower() const
	{
		return AttackPower.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetAttackPower(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetAttackPowerAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitAttackPower(float NewVal)
	{
		AttackPower.SetBaseValue(NewVal);
		AttackPower.SetCurrentValue(NewVal);
	}

	//AttackSpeed
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_AttackSpeed, Category = RPGAttributes)
		FGameplayAttributeData AttackSpeed;
	UFUNCTION()
		void OnRep_AttackSpeed(const FGameplayAttributeData& OldAttackSpeed) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, AttackSpeed, OldAttackSpeed); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, AttackSpeed)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetAttackSpeed() const
	{
		return AttackSpeed.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetAttackSpeed(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetAttackSpeedAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitAttackSpeed(float NewVal)
	{
		AttackSpeed.SetBaseValue(NewVal);
		AttackSpeed.SetCurrentValue(NewVal);
	}

	//CritChance
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_CritChance, Category = RPGAttributes)
		FGameplayAttributeData CritChance;
	UFUNCTION()
		void OnRep_CritChance(const FGameplayAttributeData& OldCritChance) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, CritChance, OldCritChance); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, CritChance)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetCritChance() const
	{
		return CritChance.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetCritChance(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetCritChanceAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitCritChance(float NewVal)
	{
		CritChance.SetBaseValue(NewVal);
		CritChance.SetCurrentValue(NewVal);
	}

	//CritMultiplier
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_CritMultiplier, Category = RPGAttributes)
		FGameplayAttributeData CritMultiplier;
	UFUNCTION()
		void OnRep_CritMultiplier(const FGameplayAttributeData& OldCritMultiplier) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, CritMultiplier, OldCritMultiplier); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, CritMultiplier)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetCritMultiplier() const
	{
		return CritMultiplier.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetCritMultiplier(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetCritMultiplierAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitCritMultiplier(float NewVal)
	{
		CritMultiplier.SetBaseValue(NewVal);
		CritMultiplier.SetCurrentValue(NewVal);
	}

	//Haste
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Haste, Category = RPGAttributes)
		FGameplayAttributeData Haste;
	UFUNCTION()
		void OnRep_Haste(const FGameplayAttributeData& OldHaste) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Haste, OldHaste); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Haste)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetHaste() const
	{
		return Haste.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetHaste(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetHasteAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitHaste(float NewVal)
	{
		Haste.SetBaseValue(NewVal);
		Haste.SetCurrentValue(NewVal);
	}

	//SpellPower
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_SpellPower, Category = RPGAttributes)
		FGameplayAttributeData SpellPower;
	UFUNCTION()
		void OnRep_SpellPower(const FGameplayAttributeData& OldSpellPower) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, SpellPower, OldSpellPower); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, SpellPower)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetSpellPower() const
	{
		return SpellPower.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetSpellPower(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetSpellPowerAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitSpellPower(float NewVal)
	{
		SpellPower.SetBaseValue(NewVal);
		SpellPower.SetCurrentValue(NewVal);
	}

	//SpellPenetration
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_SpellPenetration, Category = RPGAttributes)
		FGameplayAttributeData SpellPenetration;
	UFUNCTION()
		void OnRep_SpellPenetration(const FGameplayAttributeData& OldSpellPenetration) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, SpellPenetration, OldSpellPenetration); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, SpellPenetration)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetSpellPenetration() const
	{
		return SpellPenetration.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetSpellPenetration(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetSpellPenetrationAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitSpellPenetration(float NewVal)
	{
		SpellPenetration.SetBaseValue(NewVal);
		SpellPenetration.SetCurrentValue(NewVal);
	}

	//Defense
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Defense, Category = RPGAttributes)
		FGameplayAttributeData Defense;
	UFUNCTION()
		void OnRep_Defense(const FGameplayAttributeData& OldDefense) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Defense, OldDefense); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Defense)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetDefense() const
	{
		return Defense.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetDefense(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetDefenseAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitDefense(float NewVal)
	{
		Defense.SetBaseValue(NewVal);
		Defense.SetCurrentValue(NewVal);
	}

	//Dodge
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Dodge, Category = RPGAttributes)
		FGameplayAttributeData Dodge;
	UFUNCTION()
		void OnRep_Dodge(const FGameplayAttributeData& OldDodge) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Dodge, OldDodge); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Dodge)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetDodge() const
	{
		return Dodge.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetDodge(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetDodgeAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitDodge(float NewVal)
	{
		Dodge.SetBaseValue(NewVal);
		Dodge.SetCurrentValue(NewVal);
	}

	//Parry
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Parry, Category = RPGAttributes)
		FGameplayAttributeData Parry;
	UFUNCTION()
		void OnRep_Parry(const FGameplayAttributeData& OldParry) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Parry, OldParry); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Parry)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetParry() const
	{
		return Parry.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetParry(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetParryAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitParry(float NewVal)
	{
		Parry.SetBaseValue(NewVal);
		Parry.SetCurrentValue(NewVal);
	}

	//Avoidance
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Avoidance, Category = RPGAttributes)
		FGameplayAttributeData Avoidance;
	UFUNCTION()
		void OnRep_Avoidance(const FGameplayAttributeData& OldAvoidance) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Avoidance, OldAvoidance); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Avoidance)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetAvoidance() const
	{
		return Avoidance.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetAvoidance(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetAvoidanceAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitAvoidance(float NewVal)
	{
		Avoidance.SetBaseValue(NewVal);
		Avoidance.SetCurrentValue(NewVal);
	}

	//Versatility
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Versatility, Category = RPGAttributes)
		FGameplayAttributeData Versatility;
	UFUNCTION()
		void OnRep_Versatility(const FGameplayAttributeData& OldVersatility) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Versatility, OldVersatility); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Versatility)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetVersatility() const
	{
		return Versatility.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetVersatility(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetVersatilityAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitVersatility(float NewVal)
	{
		Versatility.SetBaseValue(NewVal);
		Versatility.SetCurrentValue(NewVal);
	}

	//Multishot
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Multishot, Category = RPGAttributes)
		FGameplayAttributeData Multishot;
	UFUNCTION()
		void OnRep_Multishot(const FGameplayAttributeData& OldMultishot) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Multishot, OldMultishot); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Multishot)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetMultishot() const
	{
		return Multishot.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetMultishot(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetMultishotAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitMultishot(float NewVal)
	{
		Multishot.SetBaseValue(NewVal);
		Multishot.SetCurrentValue(NewVal);
	}

	//Initiative
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Initiative, Category = RPGAttributes)
		FGameplayAttributeData Initiative;
	UFUNCTION()
		void OnRep_Initiative(const FGameplayAttributeData& OldInitiative) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Initiative, OldInitiative); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Initiative)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetInitiative() const
	{
		return Initiative.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetInitiative(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetInitiativeAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitInitiative(float NewVal)
	{
		Initiative.SetBaseValue(NewVal);
		Initiative.SetCurrentValue(NewVal);
	}

	//NaturalArmor
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_NaturalArmor, Category = RPGAttributes)
		FGameplayAttributeData NaturalArmor;
	UFUNCTION()
		void OnRep_NaturalArmor(const FGameplayAttributeData& OldNaturalArmor) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, NaturalArmor, OldNaturalArmor); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, NaturalArmor)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetNaturalArmor() const
	{
		return NaturalArmor.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetNaturalArmor(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetNaturalArmorAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitNaturalArmor(float NewVal)
	{
		NaturalArmor.SetBaseValue(NewVal);
		NaturalArmor.SetCurrentValue(NewVal);
	}

	//PhysicalArmor
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_PhysicalArmor, Category = RPGAttributes)
		FGameplayAttributeData PhysicalArmor;
	UFUNCTION()
		void OnRep_PhysicalArmor(const FGameplayAttributeData& OldPhysicalArmor) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, PhysicalArmor, OldPhysicalArmor); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, PhysicalArmor)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetPhysicalArmor() const
	{
		return PhysicalArmor.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetPhysicalArmor(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetPhysicalArmorAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitPhysicalArmor(float NewVal)
	{
		PhysicalArmor.SetBaseValue(NewVal);
		PhysicalArmor.SetCurrentValue(NewVal);
	}

	//BonusArmor
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_BonusArmor, Category = RPGAttributes)
		FGameplayAttributeData BonusArmor;
	UFUNCTION()
		void OnRep_BonusArmor(const FGameplayAttributeData& OldBonusArmor) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, BonusArmor, OldBonusArmor); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, BonusArmor)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetBonusArmor() const
	{
		return BonusArmor.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetBonusArmor(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetBonusArmorAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitBonusArmor(float NewVal)
	{
		BonusArmor.SetBaseValue(NewVal);
		BonusArmor.SetCurrentValue(NewVal);
	}

	//ForceArmor
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_ForceArmor, Category = RPGAttributes)
		FGameplayAttributeData ForceArmor;
	UFUNCTION()
		void OnRep_ForceArmor(const FGameplayAttributeData& OldForceArmor) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, ForceArmor, OldForceArmor); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, ForceArmor)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetForceArmor() const
	{
		return ForceArmor.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetForceArmor(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetForceArmorAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitForceArmor(float NewVal)
	{
		ForceArmor.SetBaseValue(NewVal);
		ForceArmor.SetCurrentValue(NewVal);
	}

	//MagicArmor
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_MagicArmor, Category = RPGAttributes)
		FGameplayAttributeData MagicArmor;
	UFUNCTION()
		void OnRep_MagicArmor(const FGameplayAttributeData& OldMagicArmor) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, MagicArmor, OldMagicArmor); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, MagicArmor)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetMagicArmor() const
	{
		return MagicArmor.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetMagicArmor(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetMagicArmorAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitMagicArmor(float NewVal)
	{
		MagicArmor.SetBaseValue(NewVal);
		MagicArmor.SetCurrentValue(NewVal);
	}

	//Resistance
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Resistance, Category = RPGAttributes)
		FGameplayAttributeData Resistance;
	UFUNCTION()
		void OnRep_Resistance(const FGameplayAttributeData& OldResistance) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Resistance, OldResistance); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Resistance)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetResistance() const
	{
		return Resistance.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetResistance(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetResistanceAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitResistance(float NewVal)
	{
		Resistance.SetBaseValue(NewVal);
		Resistance.SetCurrentValue(NewVal);
	}

	//ReloadSpeed
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_ReloadSpeed, Category = RPGAttributes)
		FGameplayAttributeData ReloadSpeed;
	UFUNCTION()
		void OnRep_ReloadSpeed(const FGameplayAttributeData& OldReloadSpeed) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, ReloadSpeed, OldReloadSpeed); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, ReloadSpeed)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetReloadSpeed() const
	{
		return ReloadSpeed.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetReloadSpeed(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetReloadSpeedAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitReloadSpeed(float NewVal)
	{
		ReloadSpeed.SetBaseValue(NewVal);
		ReloadSpeed.SetCurrentValue(NewVal);
	}

	//Range
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Range, Category = RPGAttributes)
		FGameplayAttributeData Range;
	UFUNCTION()
		void OnRep_Range(const FGameplayAttributeData& OldRange) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Range, OldRange); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Range)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetRange() const
	{
		return Range.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetRange(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetRangeAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitRange(float NewVal)
	{
		Range.SetBaseValue(NewVal);
		Range.SetCurrentValue(NewVal);
	}

	//Speed
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Speed, Category = RPGAttributes)
		FGameplayAttributeData Speed;
	UFUNCTION()
		void OnRep_Speed(const FGameplayAttributeData& OldSpeed) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSAttributeSet, Speed, OldSpeed); }
	ATTRIBUTE_ACCESSORS(UOWSAttributeSet, Speed)
		UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		float OWSGetSpeed() const
	{
		return Speed.GetCurrentValue();
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSSetSpeed(float NewVal)
	{
		UAbilitySystemComponent* AbilityComp = GetOwningAbilitySystemComponent();
		if (ensure(AbilityComp))
		{
			AbilityComp->SetNumericAttributeBase(GetSpeedAttribute(), NewVal);
		};
	}
	UFUNCTION(BlueprintCallable, Category = RPGAttributes)
		void OWSInitSpeed(float NewVal)
	{
		Speed.SetBaseValue(NewVal);
		Speed.SetCurrentValue(NewVal);
	}






	/* END AUTO GENERATED ATTRIBUTES */

	/** This Damage is just used for applying negative health mods. Its not a 'persistent' attribute. */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "AttributeTest", meta = (HideFromLevelInfos))		// You can't make a GameplayEffect 'powered' by Damage (Its transient)
		FGameplayAttributeData	Damage;

	/** This Healing is just used for applying positive health mods. Its not a 'persistent' attribute. */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "AttributeTest", meta = (HideFromLevelInfos))		// You can't make a GameplayEffect 'powered' by Healing (Its transient)
		FGameplayAttributeData	Healing;

	virtual bool PreGameplayEffectExecute(struct FGameplayEffectModCallbackData &Data) override;
	virtual void PostGameplayEffectExecute(const struct FGameplayEffectModCallbackData &Data) override;
};
