// Copyright 2018 Sabre Dart Studios

#pragma once

#include "CoreMinimal.h"
#include "AttributeSet.h"
#include "UObject/ObjectMacros.h"
#include "AbilitySystemComponent.h"
#include "OWSBasicAttributeSet.generated.h"

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
UCLASS()
class OWSPLUGIN_API UOWSBasicAttributeSet : public UAttributeSet
{
	GENERATED_UCLASS_BODY()

public:

	//Health
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_Health, Category = RPGAttributes, meta = (HideFromModifiers))
		FGameplayAttributeData Health;
	UFUNCTION()
		void OnRep_Health(const FGameplayAttributeData& OldHealth) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSBasicAttributeSet, Health, OldHealth); }
	ATTRIBUTE_ACCESSORS(UOWSBasicAttributeSet, Health)
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
		void OnRep_MaxHealth(const FGameplayAttributeData& OldMaxHealth) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSBasicAttributeSet, MaxHealth, OldMaxHealth); }
	ATTRIBUTE_ACCESSORS(UOWSBasicAttributeSet, MaxHealth)
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
		void OnRep_HealthRegenRate(const FGameplayAttributeData& OldHealthRegenRate) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSBasicAttributeSet, HealthRegenRate, OldHealthRegenRate); }
	ATTRIBUTE_ACCESSORS(UOWSBasicAttributeSet, HealthRegenRate)
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
	
	/*
	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_NaturalArmor, Category = "RPGAttributes")
		FGameplayAttributeData NaturalArmor;
	UFUNCTION()
		void OnRep_NaturalArmor(const FGameplayAttributeData& OldNaturalArmor) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSBasicAttributeSet, NaturalArmor, OldNaturalArmor); }

	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_PhysicalArmor, Category = "RPGAttributes")
		FGameplayAttributeData PhysicalArmor;
	UFUNCTION()
		void OnRep_PhysicalArmor(const FGameplayAttributeData& OldPhysicalArmor) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSBasicAttributeSet, PhysicalArmor, OldPhysicalArmor); }

	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_BonusArmor, Category = "RPGAttributes")
		FGameplayAttributeData BonusArmor;
	UFUNCTION()
		void OnRep_BonusArmor(const FGameplayAttributeData& OldBonusArmor) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSBasicAttributeSet, BonusArmor, OldBonusArmor); }

	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_ForceArmor, Category = "RPGAttributes")
		FGameplayAttributeData ForceArmor;
	UFUNCTION()
		void OnRep_ForceArmor(const FGameplayAttributeData& OldForceArmor) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSBasicAttributeSet, OldForceArmor); }

	UPROPERTY(EditAnywhere, BlueprintReadWrite, ReplicatedUsing = OnRep_MagicArmor, Category = "RPGAttributes")
		FGameplayAttributeData MagicArmor;
	UFUNCTION()
		void OnRep_MagicArmor(const FGameplayAttributeData& OldMagicArmor) { GAMEPLAYATTRIBUTE_REPNOTIFY(UOWSBasicAttributeSet, OldMagicArmor); }
	*/
	
	/** This Damage is just used for applying negative health mods. Its not a 'persistent' attribute. */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "AttributeTest", meta = (HideFromLevelInfos))		// You can't make a GameplayEffect 'powered' by Damage (Its transient)
		FGameplayAttributeData	Damage;

	/** This Healing is just used for applying positive health mods. Its not a 'persistent' attribute. */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "AttributeTest", meta = (HideFromLevelInfos))		// You can't make a GameplayEffect 'powered' by Healing (Its transient)
		FGameplayAttributeData	Healing;

	virtual bool PreGameplayEffectExecute(struct FGameplayEffectModCallbackData &Data) override;
	virtual void PostGameplayEffectExecute(const struct FGameplayEffectModCallbackData &Data) override;
};
