// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "OWSCharacter.h"
#include "AbilitySystemInterface.h" 
#include "OWSAttributeSet.h"
//#include "OWSGameplayAbility.h"
#include "GameplayEffectTypes.h"
#include "OWSCharacterWithAbilities.generated.h"

class AOWSAdvancedProjectile;

UENUM(BlueprintType)
enum class AbilityInput : uint8
{
	UseAbility1 UMETA(DisplayName = "Use Ability 1"), //This maps the first ability(input ID should be 0 in int) to the action mapping(which you define in the project settings) by the name of "UseAbility1". "Use Spell 1" is the blueprint name of the element.
	UseAbility2 UMETA(DisplayName = "Use Ability 2"), //Maps ability 2(input ID 1) to action mapping UseAbility2. "Use Spell 2" is mostly used for when the enum is a blueprint variable.
	UseAbility3 UMETA(DisplayName = "Use Ability 3"),
	UseAbility4 UMETA(DisplayName = "Use Ability 4"),
	UseAbility5 UMETA(DisplayName = "Use Ability 5"),
	UseAbility6 UMETA(DisplayName = "Use Ability 6"),
	UseAbility7 UMETA(DisplayName = "Use Ability 7"),
	UseAbility8 UMETA(DisplayName = "Use Ability 8"),
	UseAbility9 UMETA(DisplayName = "Use Ability 9"),
	UseAbility10 UMETA(DisplayName = "Use Ability 10"),
	UseAbility11 UMETA(DisplayName = "Use Ability 11"),
	UseAbility12 UMETA(DisplayName = "Use Ability 12"),
	UseAbility13 UMETA(DisplayName = "Use Ability 13"),
	UseAbility14 UMETA(DisplayName = "Use Ability 14"),
	UseAbility15 UMETA(DisplayName = "Use Ability 15"),
	UseAbility16 UMETA(DisplayName = "Use Ability 16"),
	UseAbility17 UMETA(DisplayName = "Use Ability 17"),
	UseAbility18 UMETA(DisplayName = "Use Ability 18"),
	UseAbility19 UMETA(DisplayName = "Use Ability 19"),
	UseAbility20 UMETA(DisplayName = "Use Ability 20"),
	UseAbility21 UMETA(DisplayName = "Use Ability 21"),
	UseAbility22 UMETA(DisplayName = "Use Ability 22"),
	UseWeapon1 UMETA(DisplayName = "Use Weapon 1"),
	UseWeapon2 UMETA(DisplayName = "Use Weapon 2")
};


/**
 * 
 */
UCLASS(abstract, BlueprintType, hidecategories = (CharacterStats, Health))
class OWSPLUGIN_API AOWSCharacterWithAbilities : public AOWSCharacter, public IAbilitySystemInterface
{
	GENERATED_BODY()

public:
	// Sets default values for this character's properties
	AOWSCharacterWithAbilities(const class FObjectInitializer& ObjectInitializer);

	static FName AbilitySystemComponentName;

	// Called when the game starts or when spawned
	virtual void BeginPlay() override;

	virtual void PostInitializeComponents() override;

	virtual void SetupPlayerInputComponent(class UInputComponent* PlayerInputComponent) override;

	virtual void PossessedBy(AController* NewController) override;

	virtual void OnRep_Controller() override;

	void AddDefaultGameplayAbilities();
	void SetupAttributeChangeDelegates();

	//Health Change
	void OnHealthChange(const FOnAttributeChangeData& Data);
	UFUNCTION(BlueprintImplementableEvent)
		void HealthChanged(float OldValue, float NewValue);
	//Energy Change
	void OnEnergyChange(const FOnAttributeChangeData& Data);
	UFUNCTION(BlueprintImplementableEvent)
		void EnergyChanged(float OldValue, float NewValue);
	//Mana Change
	void OnManaChange(const FOnAttributeChangeData& Data);
	UFUNCTION(BlueprintImplementableEvent)
		void ManaChanged(float OldValue, float NewValue);

	UFUNCTION()
		void OnGameplayEffectTagCountChanged(const FGameplayTag GameplayTag, int32 TagCount);

	UPROPERTY()
		AOWSAdvancedProjectile* LastFiredProjectile;
	
	//void HandleProjectileEffectApplicationPrediction(AOWSAdvancedProjectile* FakeProjectile, AActor* TargetActor);

	//UFUNCTION(Server, Reliable, WithValidation)
	//	void ReplicateProjectilePredictionKeyToServer(FPredictionKey PredictionKey);

	void HandleProjectileDamage(AOWSAdvancedProjectile* Projectile, bool UseExplosionEffect);

	UFUNCTION(BlueprintImplementableEvent, Category = "Init")
		void OnOWSAttributeInitalizationComplete();

	virtual void LoadAttributesFromJSON(TSharedPtr<FJsonObject> JsonObject);
	virtual void LoadCharacterStats() override;

	FTimerHandle OnGetCharacterStatsTimerHandle;

	UFUNCTION(BlueprintImplementableEvent, Category = "Damage")
		void CalculateUpdatedDamage(float OriginalDamage, const UOWSAttributeSet* SourceAttributes, FGameplayTagContainer EffectTags, float& NewDamage, bool& IsCritical);

	//Get Character Stats
	UFUNCTION(BlueprintCallable, Category = "Stats")
		void GetCharacterStats();

	//void OnGetCharacterStatsResponseReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bWasSuccessful);

	//Update Character Stats
	UFUNCTION(BlueprintCallable, Category = "Stats")
		void UpdateCharacterStats();

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = Abilities, meta = (AllowPrivateAccess = "true"))
		TObjectPtr<UAbilitySystemComponent> AbilitySystem;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = Abilities, meta = (AllowPrivateAccess = "true"))
		UOWSAttributeSet* OWSAttributes;	

	UAbilitySystemComponent* GetAbilitySystemComponent() const override
	{
		return AbilitySystem;
	};

	UFUNCTION(BlueprintCallable, Category = Combat)
		void ChangeSpell(int SpellSlot, TSubclassOf<class UGameplayAbility> NewSpell);
	UFUNCTION(BlueprintCallable, Category = Combat)
		void ChangeWeapon(int WeaponSlot, TSubclassOf<class UGameplayAbility> NewWeapon);
	UFUNCTION(BlueprintCallable, Category = Combat)
		void ClearAbility(int SpellSlot);
	UFUNCTION(BlueprintCallable, Category = Combat)
		void GrantAbility(TSubclassOf<class UGameplayAbility> NewAbility);
	UFUNCTION(BlueprintCallable, Category = Combat)
		void GrantAbilityKeyBind(TSubclassOf<class UGameplayAbility> NewAbility, int AbilityLevel, int InputID);
	UFUNCTION(BlueprintImplementableEvent, Category = Combat)
		void OnDeath(AOWSCharacter* WhoKilledMe);
	UFUNCTION(BlueprintImplementableEvent, Category = Combat)
		void OnTakeDamage(AOWSCharacter* WhoAttackedMe, float DamageAmount, bool IsCritical);
	UFUNCTION(BlueprintImplementableEvent, Category = Combat)
		void OnInflictDamage(AOWSCharacter* WhoWasDamaged, float DamageAmount, bool IsCritical);

	//Spells
	UPROPERTY()
		TArray<FGameplayAbilitySpecHandle> SpellAbilityHandles;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Ability1;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Ability2;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Ability3;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Ability4;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Ability5;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Ability6;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Ability7;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Ability8;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Ability9;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Ability0;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Ability11;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Ability12;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Ability13;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Ability14;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Ability15;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Ability16;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Ability17;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Ability18;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Ability19;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Ability20;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Ability21;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Ability22;

	//Weapons

	UPROPERTY()
		TArray<FGameplayAbilitySpecHandle> WeaponAbilityHandles;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Weapon1;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		TSubclassOf<UGameplayAbility> Weapon2;
	
	/*UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Abilities)
		UOWSGameplayAbility* Ability9Ptr;*/
	
	/*
	UFUNCTION(BlueprintCallable, Category = "Abilities")
		TArray<FGameplayTag> GetCombatHasIconTags();
	*/



};
