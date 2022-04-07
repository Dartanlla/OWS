// Copyright 2017 Sabre Dart Studios

#pragma once

#include "GameFramework/Character.h"
#include "OWSCharacterMovementComponent.h"
#include "OWSCharacterBase.generated.h"

UCLASS()
class OWSPLUGIN_API AOWSCharacterBase : public ACharacter
{
	GENERATED_BODY()

public:
	// Sets default values for this character's properties
	AOWSCharacterBase(const class FObjectInitializer& ObjectInitializer);

protected:
	// Called when the game starts or when spawned
	virtual void BeginPlay() override;

private:
	UOWSCharacterMovementComponent* OWSCharacterMovementComponent;

public:	
	// Called every frame
	virtual void Tick(float DeltaTime) override;

	// Called to bind functionality to input
	virtual void SetupPlayerInputComponent(class UInputComponent* PlayerInputComponent) override;
	virtual void PostInitializeComponents() override;

	UFUNCTION(BlueprintCallable, Category = "Movement")
		FORCEINLINE class UOWSCharacterMovementComponent* GetOWSMovementComponent() const { return OWSCharacterMovementComponent; }

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Internal")
		bool IsTransferringBetweenMaps;	
};
