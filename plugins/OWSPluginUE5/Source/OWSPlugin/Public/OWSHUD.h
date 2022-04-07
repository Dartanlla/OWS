// Copyright 2018 Sabre Dart Studios

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/HUD.h"
#include "OWSCharacter.h"
#include "OWSInventoryItemStack.h"
#include "OWSInventory.h"
#include "Engine/Canvas.h"
#include "Engine/Font.h"
#include "OWSHUD.generated.h"


UENUM(BlueprintType)
enum EAnchorPoint
{
	TopLeft	UMETA(DisplayName = "Top Left"),
	TopCenter	UMETA(DisplayName = "Top Center"),
	TopRight	UMETA(DisplayName = "Top Right"),
	MiddleLeft	UMETA(DisplayName = "Middle Left"),
	MiddleCenter	UMETA(DisplayName = "Middle Center"),
	MiddleRight	UMETA(DisplayName = "Middle Right"),
	BottomLeft	UMETA(DisplayName = "Bottom Left"),
	BottomCenter	UMETA(DisplayName = "Bottom Center"),
	BottomRight	UMETA(DisplayName = "Bottom Right")
};

UENUM(BlueprintType)
enum ESpeechBalloonStyle
{
	Speech	UMETA(DisplayName = "Speech"),
	Thought	UMETA(DisplayName = "Thought")
};

USTRUCT(BlueprintType)
struct FFloatingDamage
{
	GENERATED_BODY()

	FFloatingDamage() {
		DamageText = "";
		DamageTextLength = 0.f;
		TimeLeft = 0.f;
		Alpha = 0.f;
		DisplayLocation = FVector2D(0);
		DamagedActor = nullptr;
		DamagedActorOffset = FVector(0);
		IsHealing = true;
		IsCritical = true;
		MarkedForDeletion = true;
		ShowDropShadow = true;
		ShowOutline = true;
	}

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Text")
		FString DamageText;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Text")
		float DamageTextLength;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Text")
		float TimeLeft;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Text")
		float Alpha;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Text")
		FVector2D DisplayLocation;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Text")
		AActor* DamagedActor;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Text")
		FVector DamagedActorOffset;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Text")
		bool IsHealing;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Text")
		bool IsCritical;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Text")
		bool MarkedForDeletion;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Text")
		bool ShowDropShadow;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Text")
		bool ShowOutline;
};

USTRUCT(BlueprintType)
struct FDialogueChoice
{
	GENERATED_BODY()

	FDialogueChoice() {
		ChoiceKeyword = "";
		ChoiceText = "";
		Weight = 0.f;
	}

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dialogue")
		FString ChoiceKeyword;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dialogue")
		FString ChoiceText;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dialogue")
		float Weight;
};

USTRUCT(BlueprintType)
struct FCharacterSpeaking
{
	GENERATED_BODY()

	FCharacterSpeaking() {
		Speaker = nullptr;
		SpokenText = "";
		SpokenStartTime = 0.f;
		SpokenDuration = 0.f;
	}

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dialogue")
		AOWSCharacter* Speaker;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dialogue")
		FString SpokenText;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dialogue")
		float SpokenStartTime;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dialogue")
		float SpokenDuration;
};


/**
 * 
 */
UCLASS()
class OWSPLUGIN_API AOWSHUD : public AHUD
{
	GENERATED_BODY()

public:
	AOWSHUD();
		
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Damage")
		UFont* FloatingDamageFont;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Damage")
		FVector2D FloatingDamageSpeed;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Damage")
		FLinearColor FloatingDamageFontColor;	
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Damage")
		FLinearColor FloatingDamageDropShadowColor;	
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Damage")
		float FloatingDamageDropShadowOffsetX;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Damage")
		float FloatingDamageDropShadowOffsetY;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Damage")
		FLinearColor FloatingDamageOutlineColor;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Damage")
		UFont* FloatingDamageOutlineFont;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Damage")
		float FloatingDamageMinimumDisplayTime;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Damage")
		float FloatingDamageFadeOutSpeed;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Critical Damage")
		UFont* FloatingCriticalDamageFont;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Critical Damage")
		FVector2D FloatingCriticalDamageSpeed;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Critical Damage")
		FLinearColor FloatingCriticalDamageFontColor;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Critical Damage")
		FLinearColor FloatingCriticalDamageDropShadowColor;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Critical Damage")
		float FloatingCriticalDamageDropShadowOffsetX;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Critical Damage")
		float FloatingCriticalDamageDropShadowOffsetY;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Critical Damage")
		FLinearColor FloatingCriticalDamageOutlineColor;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Critical Damage")
		UFont* FloatingCriticalDamageOutlineFont;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Critical Damage")
		float FloatingCriticalDamageMinimumDisplayTime;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Critical Damage")
		float FloatingCriticalDamageFadeOutSpeed;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Healing")
		UFont* FloatingHealingFont;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Healing")
		FVector2D FloatingHealingSpeed;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Healing")
		FLinearColor FloatingHealingFontColor;	
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Healing")
		FLinearColor FloatingHealingDropShadowColor;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Healing")
		float FloatingHealingDropShadowOffsetX;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Healing")
		float FloatingHealingDropShadowOffsetY;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Healing")
		FLinearColor FloatingHealingOutlineColor;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Healing")
		UFont* FloatingHealingOutlineFont;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Healing")
		float FloatingHealingMinimumDisplayTime;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Healing")
		float FloatingHealingFadeOutSpeed;
	
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Critical Healing")
		UFont* FloatingCriticalHealingFont;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Critical Healing")
		FVector2D FloatingCriticalHealingSpeed;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Critical Healing")
		FLinearColor FloatingCriticalHealingFontColor;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Critical Healing")
		FLinearColor FloatingCriticalHealingDropShadowColor;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Critical Healing")
		float FloatingCriticalHealingDropShadowOffsetX;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Critical Healing")
		float FloatingCriticalHealingDropShadowOffsetY;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Critical Healing")
		FLinearColor FloatingCriticalHealingOutlineColor;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Critical Healing")
		UFont* FloatingCriticalHealingOutlineFont;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Critical Healing")
		float FloatingCriticalHealingMinimumDisplayTime;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Floating Critical Healing")
		float FloatingCriticalHealingFadeOutSpeed;

	UFUNCTION(BlueprintCallable, Category = "Damage")
		void AddFloatingDamageItem(FString DamageText, AActor* DamagedActor, FVector DamagedActorOffset, bool IsHealing = false, bool IsCritical = false, bool ShowDropShadow = false, bool ShowOutline = false);

	UFUNCTION(BlueprintCallable, Category = "Damage")
		void CleanUpFloatingDamageItems();

	UFUNCTION(BlueprintCallable, Category = "Dialogue")
		void ClearDialogueChoices();

	UFUNCTION(BlueprintCallable, Category = "Dialogue")
		void AddDialogueChoice(FString ChoiceKeyword, FString ChoiceText);

	UFUNCTION(BlueprintCallable, Category = "Dialogue")
		int32 GetEstimatedDialogueNumberOfLines(const FString DialogueText, const int32 TextWrapPoint, const float AverageLetterWidth);

	
protected:
	APlayerController* PC;
	AOWSCharacter* OWSChar;
	FVector2D MouseLocation;
	int32 SlotBeingDraggedFrom;
	FName InventoryBeingDraggedFrom;
	bool SplitDialogOpen;
	int32 SplitNumber;
	int32 StackToSplitSize;
	UOWSInventoryItemStack* InventoryItemStackToSplit;
	UTexture* SplitDialogTexture;
	//int32 ScreenCenterX;
	//int32 ScreenCenterY;
	int32 ScreenWidth;
	int32 ScreenHeight;

	UPROPERTY(VisibleAnywhere, BlueprintReadWrite, Category = "Damage")
		TArray<FFloatingDamage> FloatingDamageItems;

	UPROPERTY()
		UOWSInventoryItemStack* ItemStackBeingDragged;

	UFUNCTION(BlueprintCallable, Category = "Damage")
		void RenderFloatingDamage(float DeltaTime);

	UFUNCTION(BlueprintCallable, Category = "Screen")
		FVector2D CalculateScreenPosition(const enum EAnchorPoint ScreenAnchorPoint, const enum EAnchorPoint UIAnchorPoint, const int32 OffsetX, const int32 OffsetY, const int32 UIWidth,
			const int32 UIHeight);

	//Windows
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "UI")
		UTexture* WindowBorderTop;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "UI")
		UTexture* WindowBorderLeft;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "UI")
		UTexture* WindowBorderRight;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "UI")
		UTexture* WindowBorderBottom;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "UI")
		UTexture* WindowBorderTopLeftCorner;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "UI")
		UTexture* WindowBorderTopRightCorner;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "UI")
		UTexture* WindowBorderBottomLeftCorner;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "UI")
		UTexture* WindowBorderBottomRightCorner;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "UI")
		UTexture* WindowBackground;

	//Thought Bubble Tail
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "UI")
		UTexture* ThoughtBubbleTail;

	//Speech Baloon Tail
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "UI")
		UTexture* SpeechBalloonTail;

	//Speakers
	UPROPERTY(VisibleAnywhere, BlueprintReadWrite, Category = "Dialogue")
		TArray<FCharacterSpeaking> Speakers;

	void RemoveExpiredSpeakers();

	UFUNCTION(BlueprintCallable, Category = "UI")
		void DrawWindow(const enum EAnchorPoint ScreenAnchorPoint, const enum EAnchorPoint UIAnchorPoint, int32 X, int32 Y, int32 Width, int32 Height, int32& DrawnAtX, int32& DrawnAtY);

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dialogue")
		TArray<FDialogueChoice> DialogueChoices;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dialogue")
		UFont* DialogueFont;

	UFUNCTION(BlueprintCallable, Category = "Dialogue")
		void RenderInteractiveDialogueChoicesInternal(const TArray<FDialogueChoice>& ChoicesToRender, AOWSCharacter* NPC, UTexture* BackgroundTexture, int32 MinSizeX, int32 MinSizeY, float FontSize);

	UFUNCTION(BlueprintCallable, Category = "Dialogue")
		void RenderInteractiveDialogueChoices(AOWSCharacter* NPC, UTexture* BackgroundTexture, int32 MinSizeX, int32 MinSizeY, float FontSize);

	UFUNCTION(BlueprintImplementableEvent, Category = "Dialogue")
		void OnDialogueChoiceHovered(const FDialogueChoice DialogueChoiceHovered);

	UFUNCTION(BlueprintImplementableEvent, Category = "Dialogue")
		void OnDialogueChoiceBlur(const FDialogueChoice DialogueChoiceHovered);

	UFUNCTION(BlueprintImplementableEvent, Category = "Dialogue")
		void OnDialogueChoiceSelected(const FDialogueChoice DialogueChoiceSelecte);

	UFUNCTION(BlueprintCallable, Category = "Dialogue")
		void RenderDialogueBox(AOWSCharacter* Speaker, const enum EAnchorPoint UIAnchorPoint, const enum ESpeechBalloonStyle BalloonStyle, FString DialogueText, const float FontScale,
			const float OffsetX, const float OffsetY, const int32 TextWrapPoint, const bool DrawTail, const float BorderWidth);

	FVector2D GetDialogueBoxSize(const FString DialogueText, UFont* Font, const float FontScale, const int32 TextWrapPoint, const int32 PaddingX, const int32 PaddingY, const int32 NumberOfLines);

	/* Should we draw a second icon with an offset when there is a stack. */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		bool bDrawSecondIconForStack;

	/* The offset to draw the second icon in a stack. */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		float StackDrawOffset;

	/* The offset to draw the number of items in a stack (entered as positive, but draws negative). */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Inventory")
		float StackDrawTextOffset;

	UFUNCTION(BlueprintCallable, Category = "Inventory")
		void RenderInteractiveInventoryGrid(UOWSInventory* Inventory, UTexture* EmptySlotTexture, enum EAnchorPoint UIAnchorPoint, int32 X, int32 Y, int32 XSpacing, int32 YSpacing, int32 iconWidth, int32 iconHeight, int32 NumberOfRows, int32 NumberOfCols);

	UFUNCTION(BlueprintCallable, Category = "Inventory")
		void RenderInteractiveInventoryGridUsingLockedSlotGroups(UOWSInventory* Inventory, UTexture* EmptySlotTexture, UTexture* LockedRowTexture, int32 X, int32 Y,
			int32 XSpacing, int32 YSpacing, int32 iconWidth, int32 iconHeight, int32 NumberOfRows, int32 NumberOfCols, int32 SlotGroupRows, int32 SlotGroupCols, int32 SlotGroupXSpacing,
			int32 SlotGroupYSpacing);

	UFUNCTION(BlueprintCallable, Category = "Inventory")
		void SetSplitDialogTexture(UTexture* inSplitDialogTexture);
	
	void AddSlotsToSkip(TSet<int32> &SlotsToSkip, const int32 SlotNumber, const int32 ItemSlotWidth, const int32 ItemSlotHeight, const int32 NumberOfColumns);
	void GetInventoryNameAndSlot(const FName BoxName, FName &InventoryName, int32 &Slot);
	void GetInput();
	void DrawSplitDialog();

	TSet<int32> SlotsToShowWhileDragging;

	virtual void PostInitializeComponents() override;
	void NotifyHitBoxClick(const FName BoxName) override;
	void NotifyHitBoxBeginCursorOver(FName BoxName) override;
	void NotifyHitBoxEndCursorOver(FName BoxName) override;
	virtual void DrawHUD() override;
};
