// Copyright 2018 Sabre Dart Studios

#include "OWSHUD.h"


AOWSHUD::AOWSHUD()
{
	SplitNumber = 1;
	StackToSplitSize = 2;
	bDrawSecondIconForStack = true;
	StackDrawOffset = 3.f;
	StackDrawTextOffset = 15.f;
}

void AOWSHUD::AddFloatingDamageItem(FString DamageText, AActor* DamagedActor, FVector DamagedActorOffset, bool IsHealing, bool IsCritical, bool ShowDropShadow, bool ShowOutline)
{
	FFloatingDamage TempFloatingDamageItem;

	TempFloatingDamageItem.DamageText = DamageText;
	TempFloatingDamageItem.IsHealing = IsHealing;
	TempFloatingDamageItem.IsCritical = IsCritical;
	TempFloatingDamageItem.DamageTextLength = 0.f;
	TempFloatingDamageItem.DamagedActor = DamagedActor;	
	TempFloatingDamageItem.DamagedActorOffset = DamagedActorOffset;
	TempFloatingDamageItem.Alpha = 1.f;
	TempFloatingDamageItem.MarkedForDeletion = false;
	TempFloatingDamageItem.ShowDropShadow = ShowDropShadow;
	TempFloatingDamageItem.ShowOutline = ShowOutline;

	if (!IsHealing) //Damage
	{
		TempFloatingDamageItem.TimeLeft = FloatingDamageMinimumDisplayTime;
	}
	else //Healing
	{
		TempFloatingDamageItem.TimeLeft = FloatingHealingMinimumDisplayTime;
	}

	FloatingDamageItems.Add(TempFloatingDamageItem);
}

void AOWSHUD::CleanUpFloatingDamageItems()
{
	FloatingDamageItems.RemoveAll([](const FFloatingDamage item) {
		return item.MarkedForDeletion == true;
	});
}

void AOWSHUD::RenderFloatingDamage(float DeltaTime)
{
	for (FFloatingDamage& FloatingDamageItem : FloatingDamageItems)
	{
		if (FloatingDamageItem.DamagedActor)
		{
			//Calculate text width the first time
			if (FloatingDamageItem.DamageTextLength <= 0.f)
			{
				float TextWidth = 0.f, TextHeight = 0.f;
				GetTextSize(FloatingDamageItem.DamageText, TextWidth, TextHeight, FloatingDamageFont, 1.f);
				FloatingDamageItem.DamageTextLength = TextWidth;
			}

			//Move text up
			FloatingDamageItem.DamagedActorOffset.Z += FloatingDamageSpeed.Y * DeltaTime;

			//Calcualte DamagedActor location on screen
			FVector DamageLocation = FloatingDamageItem.DamagedActor->GetActorLocation() + FloatingDamageItem.DamagedActorOffset;
			FVector ProjectedScreenLocation = Project(DamageLocation);
			FloatingDamageItem.DisplayLocation.X = ProjectedScreenLocation.X - (FloatingDamageItem.DamageTextLength / 2.f);
			FloatingDamageItem.DisplayLocation.Y = ProjectedScreenLocation.Y;

			//Remove DeltaTime from TimeLeft
			FloatingDamageItem.TimeLeft -= DeltaTime;

			//Has time expired?
			if (FloatingDamageItem.TimeLeft < 0.1f)
			{
				//Mark for deletion that will happen in CleanUpFloatingDamageItems()
				FloatingDamageItem.MarkedForDeletion = true;
			}
			else
			{
				UFont* FloatingTextFont;
				UFont* FloatingTextOutlineFont;
				FLinearColor TextColorToRender;
				FLinearColor DropShadowColorToRender;
				FLinearColor OutlineColorToRender;
				float DropShadowOffsetX;
				float DropShadowOffsetY;

				//Is this damage or healing?
				if (!FloatingDamageItem.IsHealing) //Damage
				{
					if (!FloatingDamageItem.IsCritical)
					{
						FloatingTextFont = FloatingDamageFont;
						FloatingTextOutlineFont = FloatingDamageOutlineFont;

						TextColorToRender = FloatingDamageFontColor;
						DropShadowColorToRender = FloatingDamageDropShadowColor;
						OutlineColorToRender = FloatingDamageOutlineColor;

						DropShadowOffsetX = FloatingDamageDropShadowOffsetX;
						DropShadowOffsetY = FloatingDamageDropShadowOffsetY;

						//Fade out if FloatingDamageFadeOutSpeed > 0.f
						if (FloatingDamageFadeOutSpeed > 0.f)
						{
							FloatingDamageItem.Alpha = FMath::Clamp(FloatingDamageItem.Alpha - ((FloatingDamageFadeOutSpeed / FloatingDamageMinimumDisplayTime) * DeltaTime), 0.f, 1.f);
						}
					}
					else
					{
						FloatingTextFont = FloatingCriticalDamageFont;
						FloatingTextOutlineFont = FloatingCriticalDamageOutlineFont;

						TextColorToRender = FloatingCriticalDamageFontColor;
						DropShadowColorToRender = FloatingCriticalDamageDropShadowColor;
						OutlineColorToRender = FloatingCriticalDamageOutlineColor;

						DropShadowOffsetX = FloatingCriticalDamageDropShadowOffsetX;
						DropShadowOffsetY = FloatingCriticalDamageDropShadowOffsetY;

						//Fade out if FloatingCriticalDamageFadeOutSpeed > 0.f
						if (FloatingCriticalDamageFadeOutSpeed > 0.f)
						{
							FloatingDamageItem.Alpha = FMath::Clamp(FloatingDamageItem.Alpha - ((FloatingCriticalDamageFadeOutSpeed / FloatingCriticalDamageMinimumDisplayTime) * DeltaTime), 0.f, 1.f);
						}
					}
				}
				else //Healing
				{
					if (!FloatingDamageItem.IsCritical)
					{
						FloatingTextFont = FloatingHealingFont;
						FloatingTextOutlineFont = FloatingHealingOutlineFont;

						TextColorToRender = FloatingHealingFontColor;
						DropShadowColorToRender = FloatingHealingDropShadowColor;
						OutlineColorToRender = FloatingHealingOutlineColor;

						DropShadowOffsetX = FloatingHealingDropShadowOffsetX;
						DropShadowOffsetY = FloatingHealingDropShadowOffsetY;

						//Fade out if FloatingHealingFadeOutSpeed > 0.f
						if (FloatingHealingFadeOutSpeed > 0.f)
						{
							FloatingDamageItem.Alpha = FMath::Clamp(FloatingDamageItem.Alpha - ((FloatingHealingFadeOutSpeed / FloatingHealingMinimumDisplayTime) * DeltaTime), 0.f, 1.f);
						}
					}
					else
					{
						FloatingTextFont = FloatingCriticalHealingFont;
						FloatingTextOutlineFont = FloatingCriticalHealingOutlineFont;

						TextColorToRender = FloatingCriticalHealingFontColor;
						DropShadowColorToRender = FloatingCriticalHealingDropShadowColor;
						OutlineColorToRender = FloatingCriticalHealingOutlineColor;

						DropShadowOffsetX = FloatingCriticalHealingDropShadowOffsetX;
						DropShadowOffsetY = FloatingCriticalHealingDropShadowOffsetY;

						//Fade out if FloatingCriticalHealingFadeOutSpeed > 0.f
						if (FloatingCriticalHealingFadeOutSpeed > 0.f)
						{
							FloatingDamageItem.Alpha = FMath::Clamp(FloatingDamageItem.Alpha - ((FloatingCriticalHealingFadeOutSpeed / FloatingCriticalHealingMinimumDisplayTime) * DeltaTime), 0.f, 1.f);
						}
					}
				}

				TextColorToRender.A = FloatingDamageItem.Alpha;
				DropShadowColorToRender.A = FloatingDamageItem.Alpha;
				OutlineColorToRender.A = FloatingDamageItem.Alpha;

				if (FloatingDamageItem.ShowDropShadow)
				{
					DrawText(FloatingDamageItem.DamageText, DropShadowColorToRender, FloatingDamageItem.DisplayLocation.X + DropShadowOffsetX,
						FloatingDamageItem.DisplayLocation.Y + DropShadowOffsetY, FloatingTextFont, 1.f);
				}
				if (FloatingDamageItem.ShowOutline)
				{
					DrawText(FloatingDamageItem.DamageText, OutlineColorToRender, FloatingDamageItem.DisplayLocation.X, FloatingDamageItem.DisplayLocation.Y, FloatingTextOutlineFont, 1.f);
				}
				DrawText(FloatingDamageItem.DamageText, TextColorToRender, FloatingDamageItem.DisplayLocation.X, FloatingDamageItem.DisplayLocation.Y, FloatingTextFont, 1.f);
			}
		}
	}
}

void AOWSHUD::SetSplitDialogTexture(UTexture* inSplitDialogTexture)
{
	SplitDialogTexture = inSplitDialogTexture;
}

FVector2D AOWSHUD::CalculateScreenPosition(const enum EAnchorPoint ScreenAnchorPoint, const enum EAnchorPoint UIAnchorPoint, const int32 OffsetX, const int32 OffsetY, const int32 UIWidth,
	const int32 UIHeight)
{
	FVector2D OutScreenPosition;

	if (!Canvas)
	{
		OutScreenPosition.X = 0.f;
		OutScreenPosition.Y = 0.f;
		return OutScreenPosition;
	}

	int32 ScreenCenterX = Canvas->SizeX / 2;
	int32 ScreenCenterY = Canvas->SizeY / 2;

	switch (ScreenAnchorPoint)
	{
	case EAnchorPoint::TopLeft:
		switch (UIAnchorPoint)
		{
		case EAnchorPoint::TopLeft:
			OutScreenPosition.X = OffsetX;
			OutScreenPosition.Y = OffsetY;
			break;
		case EAnchorPoint::TopCenter:
			OutScreenPosition.X = FMath::FloorToInt(OffsetX - (UIWidth / 2));
			OutScreenPosition.Y = OffsetY;
			break;
		case EAnchorPoint::TopRight:
			OutScreenPosition.X = OffsetX - UIWidth;
			OutScreenPosition.Y = OffsetY;
			break;
		case EAnchorPoint::MiddleLeft:
			OutScreenPosition.X = OffsetX;
			OutScreenPosition.Y = FMath::FloorToInt(OffsetY - (UIHeight / 2));
			break;
		case EAnchorPoint::MiddleCenter:
			OutScreenPosition.X = FMath::FloorToInt(OffsetX - (UIWidth / 2));
			OutScreenPosition.Y = FMath::FloorToInt(OffsetY - (UIHeight / 2));
			break;
		case EAnchorPoint::MiddleRight:
			OutScreenPosition.X = OffsetX - UIWidth;
			OutScreenPosition.Y = FMath::FloorToInt(OffsetY - (UIHeight / 2));
			break;
		case EAnchorPoint::BottomLeft:
			OutScreenPosition.X = OffsetX;
			OutScreenPosition.Y = OffsetY - UIHeight;
			break;
		case EAnchorPoint::BottomCenter:
			OutScreenPosition.X = FMath::FloorToInt(OffsetX - (UIWidth / 2));
			OutScreenPosition.Y = OffsetY - UIHeight;
			break;
		case EAnchorPoint::BottomRight:
			OutScreenPosition.X = OffsetX - UIWidth;
			OutScreenPosition.Y = OffsetY - UIHeight;
			break;
		}

		break;

	case EAnchorPoint::TopCenter:
		switch (UIAnchorPoint)
		{
		case EAnchorPoint::TopLeft:
			OutScreenPosition.X = ScreenCenterX + OffsetX;
			OutScreenPosition.Y = OffsetY;
			break;
		case EAnchorPoint::TopCenter:
			OutScreenPosition.X = FMath::FloorToInt(ScreenCenterX + OffsetX - (UIWidth / 2));
			OutScreenPosition.Y = OffsetY;
			break;
		case EAnchorPoint::TopRight:
			OutScreenPosition.X = ScreenCenterX + OffsetX - UIWidth;
			OutScreenPosition.Y = OffsetY;
			break;
		}

		break;

	case EAnchorPoint::TopRight:
		switch (UIAnchorPoint)
		{
		case EAnchorPoint::TopRight:
			OutScreenPosition.X = Canvas->SizeX + OffsetX;
			OutScreenPosition.Y = OffsetY;
			break;
		}

		break;

	case EAnchorPoint::MiddleLeft:
		switch (UIAnchorPoint)
		{
		case EAnchorPoint::TopLeft:
			OutScreenPosition.X = OffsetX;
			OutScreenPosition.Y = ScreenCenterY + OffsetY;
			break;
		case EAnchorPoint::MiddleLeft:
			OutScreenPosition.X = OffsetX;
			OutScreenPosition.Y = FMath::FloorToInt(ScreenCenterY + OffsetY - (UIHeight / 2));
			break;
		case EAnchorPoint::BottomLeft:
			OutScreenPosition.X = OffsetX;
			OutScreenPosition.Y = ScreenCenterY + OffsetY - UIHeight;
			break;
		}

		break;

	case EAnchorPoint::MiddleCenter:
		switch (UIAnchorPoint)
		{
		case EAnchorPoint::TopLeft:
			OutScreenPosition.X = ScreenCenterX + OffsetX;
			OutScreenPosition.Y = ScreenCenterY + OffsetY;
			break;
		case EAnchorPoint::TopCenter:
			OutScreenPosition.X = FMath::FloorToInt(ScreenCenterX + OffsetX - (UIWidth / 2));
			OutScreenPosition.Y = ScreenCenterY + OffsetY;
			break;
		case EAnchorPoint::TopRight:
			OutScreenPosition.X = ScreenCenterX + OffsetX - UIWidth;
			OutScreenPosition.Y = ScreenCenterY + OffsetY;
			break;
		case EAnchorPoint::MiddleLeft:
			OutScreenPosition.X = ScreenCenterX + OffsetX;
			OutScreenPosition.Y = FMath::FloorToInt(ScreenCenterY + OffsetY - (UIHeight / 2));
			break;
		case EAnchorPoint::MiddleCenter:
			OutScreenPosition.X = FMath::FloorToInt(ScreenCenterX + OffsetX - (UIWidth / 2));
			OutScreenPosition.Y = FMath::FloorToInt(ScreenCenterY + OffsetY - (UIHeight / 2));
			break;
		case EAnchorPoint::MiddleRight:
			OutScreenPosition.X = ScreenCenterX + OffsetX - UIWidth;
			OutScreenPosition.Y = FMath::FloorToInt(ScreenCenterY + OffsetY - (UIHeight / 2));
			break;
		case EAnchorPoint::BottomLeft:
			OutScreenPosition.X = ScreenCenterX + OffsetX;
			OutScreenPosition.Y = ScreenCenterY + OffsetY - UIHeight;
			break;
		case EAnchorPoint::BottomCenter:
			OutScreenPosition.X = FMath::FloorToInt(ScreenCenterX + OffsetX - (UIWidth / 2));
			OutScreenPosition.Y = ScreenCenterY + OffsetY - UIHeight;
			break;
		case EAnchorPoint::BottomRight:
			OutScreenPosition.X = ScreenCenterX + OffsetX - UIWidth;
			OutScreenPosition.Y = ScreenCenterY + OffsetY - UIHeight;
			break;
		}

		break;

	case EAnchorPoint::MiddleRight:
		switch (UIAnchorPoint)
		{
		case EAnchorPoint::TopLeft:
			OutScreenPosition.X = Canvas->SizeX + OffsetX;
			OutScreenPosition.Y = ScreenCenterY + OffsetY;
			break;
		case EAnchorPoint::TopCenter:
			OutScreenPosition.X = FMath::FloorToInt(Canvas->SizeX + OffsetX - (UIWidth / 2));
			OutScreenPosition.Y = ScreenCenterY + OffsetY;
			break;
		case EAnchorPoint::TopRight:
			OutScreenPosition.X = Canvas->SizeX + OffsetX - UIWidth;
			OutScreenPosition.Y = ScreenCenterY + OffsetY;
			break;
		case EAnchorPoint::MiddleLeft:
			OutScreenPosition.X = Canvas->SizeX + OffsetX;
			OutScreenPosition.Y = FMath::FloorToInt(ScreenCenterY + OffsetY - (UIHeight / 2));
			break;
		case EAnchorPoint::MiddleCenter:
			OutScreenPosition.X = FMath::FloorToInt(Canvas->SizeX + OffsetX - (UIWidth / 2));
			OutScreenPosition.Y = FMath::FloorToInt(ScreenCenterY + OffsetY - (UIHeight / 2));
			break;
		case EAnchorPoint::MiddleRight:
			OutScreenPosition.X = Canvas->SizeX + OffsetX - UIWidth;
			OutScreenPosition.Y = FMath::FloorToInt(ScreenCenterY + OffsetY - (UIHeight / 2));
			break;
		case EAnchorPoint::BottomLeft:
			OutScreenPosition.X = Canvas->SizeX + OffsetX;
			OutScreenPosition.Y = ScreenCenterY + OffsetY - UIHeight;
			break;
		case EAnchorPoint::BottomCenter:
			OutScreenPosition.X = FMath::FloorToInt(Canvas->SizeX + OffsetX - (UIWidth / 2));
			OutScreenPosition.Y = ScreenCenterY + OffsetY - UIHeight;
			break;
		case EAnchorPoint::BottomRight:
			OutScreenPosition.X = Canvas->SizeX + OffsetX - UIWidth;
			OutScreenPosition.Y = ScreenCenterY + OffsetY - UIHeight;
			break;
		}

		break;

	case EAnchorPoint::BottomLeft:
		switch (UIAnchorPoint)
		{
		case EAnchorPoint::TopLeft:
			OutScreenPosition.X = OffsetX;
			OutScreenPosition.Y = Canvas->SizeY + OffsetY;
			break;
		case EAnchorPoint::TopCenter:
			OutScreenPosition.X = FMath::FloorToInt(OffsetX - (UIWidth / 2));
			OutScreenPosition.Y = Canvas->SizeY + OffsetY;
			break;
		case EAnchorPoint::TopRight:
			OutScreenPosition.X = OffsetX - UIWidth;
			OutScreenPosition.Y = Canvas->SizeY + OffsetY;
			break;
		case EAnchorPoint::MiddleLeft:
			OutScreenPosition.X = OffsetX;
			OutScreenPosition.Y = FMath::FloorToInt(Canvas->SizeY + OffsetY - (UIHeight / 2));
			break;
		case EAnchorPoint::MiddleCenter:
			OutScreenPosition.X = FMath::FloorToInt(OffsetX - (UIWidth / 2));
			OutScreenPosition.Y = FMath::FloorToInt(Canvas->SizeY + OffsetY - (UIHeight / 2));
			break;
		case EAnchorPoint::MiddleRight:
			OutScreenPosition.X = OffsetX - UIWidth;
			OutScreenPosition.Y = FMath::FloorToInt(Canvas->SizeY + OffsetY - (UIHeight / 2));
			break;
		case EAnchorPoint::BottomLeft:
			OutScreenPosition.X = OffsetX;
			OutScreenPosition.Y = Canvas->SizeY + OffsetY - UIHeight;
			break;
		case EAnchorPoint::BottomCenter:
			OutScreenPosition.X = FMath::FloorToInt(OffsetX - (UIWidth / 2));
			OutScreenPosition.Y = Canvas->SizeY + OffsetY - UIHeight;
			break;
		case EAnchorPoint::BottomRight:
			OutScreenPosition.X = OffsetX - UIWidth;
			OutScreenPosition.Y = Canvas->SizeY + OffsetY - UIHeight;
			break;
		}

		break;

	case EAnchorPoint::BottomCenter:
		switch (UIAnchorPoint)
		{
		case EAnchorPoint::TopLeft:
			OutScreenPosition.X = ScreenCenterX + OffsetX;
			OutScreenPosition.Y = Canvas->SizeY + OffsetY;
			break;
		case EAnchorPoint::TopCenter:
			OutScreenPosition.X = ScreenCenterX + FMath::FloorToInt(OffsetX - (UIWidth / 2));
			OutScreenPosition.Y = Canvas->SizeY + OffsetY;
			break;
		case EAnchorPoint::TopRight:
			OutScreenPosition.X = ScreenCenterX + OffsetX - UIWidth;
			OutScreenPosition.Y = Canvas->SizeY + OffsetY;
			break;
		case EAnchorPoint::MiddleLeft:
			OutScreenPosition.X = ScreenCenterX + OffsetX;
			OutScreenPosition.Y = FMath::FloorToInt(Canvas->SizeY + OffsetY - (UIHeight / 2));
			break;
		case EAnchorPoint::MiddleCenter:
			OutScreenPosition.X = ScreenCenterX + FMath::FloorToInt(OffsetX - (UIWidth / 2));
			OutScreenPosition.Y = FMath::FloorToInt(Canvas->SizeY + OffsetY - (UIHeight / 2));
			break;
		case EAnchorPoint::MiddleRight:
			OutScreenPosition.X = ScreenCenterX + OffsetX - UIWidth;
			OutScreenPosition.Y = FMath::FloorToInt(Canvas->SizeY + OffsetY - (UIHeight / 2));
			break;
		case EAnchorPoint::BottomLeft:
			OutScreenPosition.X = ScreenCenterX + OffsetX;
			OutScreenPosition.Y = Canvas->SizeY + OffsetY - UIHeight;
			break;
		case EAnchorPoint::BottomCenter:
			OutScreenPosition.X = ScreenCenterX + FMath::FloorToInt(OffsetX - (UIWidth / 2));
			OutScreenPosition.Y = Canvas->SizeY + OffsetY - UIHeight;
			break;
		case EAnchorPoint::BottomRight:
			OutScreenPosition.X = ScreenCenterX + OffsetX - UIWidth;
			OutScreenPosition.Y = Canvas->SizeY + OffsetY - UIHeight;
			break;
		}

		break;

	case EAnchorPoint::BottomRight:
		switch (UIAnchorPoint)
		{
		case EAnchorPoint::TopLeft:
			OutScreenPosition.X = Canvas->SizeX + OffsetX;
			OutScreenPosition.Y = Canvas->SizeY + OffsetY;
			break;
		case EAnchorPoint::TopCenter:
			OutScreenPosition.X = FMath::FloorToInt(Canvas->SizeX + OffsetX - (UIWidth / 2));
			OutScreenPosition.Y = Canvas->SizeY + OffsetY;
			break;
		case EAnchorPoint::TopRight:
			OutScreenPosition.X = Canvas->SizeX + OffsetX - UIWidth;
			OutScreenPosition.Y = Canvas->SizeY + OffsetY;
			break;
		case EAnchorPoint::MiddleLeft:
			OutScreenPosition.X = Canvas->SizeX + OffsetX;
			OutScreenPosition.Y = FMath::FloorToInt(Canvas->SizeY + OffsetY - (UIHeight / 2));
			break;
		case EAnchorPoint::MiddleCenter:
			OutScreenPosition.X = FMath::FloorToInt(Canvas->SizeX + OffsetX - (UIWidth / 2));
			OutScreenPosition.Y = FMath::FloorToInt(Canvas->SizeY + OffsetY - (UIHeight / 2));
			break;
		case EAnchorPoint::MiddleRight:
			OutScreenPosition.X = Canvas->SizeX + OffsetX - UIWidth;
			OutScreenPosition.Y = FMath::FloorToInt(Canvas->SizeY + OffsetY - (UIHeight / 2));
			break;
		case EAnchorPoint::BottomLeft:
			OutScreenPosition.X = Canvas->SizeX + OffsetX;
			OutScreenPosition.Y = Canvas->SizeY + OffsetY - UIHeight;
			break;
		case EAnchorPoint::BottomCenter:
			OutScreenPosition.X = FMath::FloorToInt(Canvas->SizeX + OffsetX - (UIWidth / 2));
			OutScreenPosition.Y = Canvas->SizeY + OffsetY - UIHeight;
			break;
		case EAnchorPoint::BottomRight:
			OutScreenPosition.X = Canvas->SizeX + OffsetX - UIWidth;
			OutScreenPosition.Y = Canvas->SizeY + OffsetY - UIHeight;
			break;
		}

		break;

	}

	//Clamp Window Position
	OutScreenPosition.X = FMath::Clamp<int32>(OutScreenPosition.X, 0, Canvas->SizeX);
	OutScreenPosition.Y = FMath::Clamp<int32>(OutScreenPosition.Y, 0, Canvas->SizeY);

	return OutScreenPosition;
}

void AOWSHUD::DrawWindow(const enum EAnchorPoint ScreenAnchorPoint, const enum EAnchorPoint UIAnchorPoint, int32 X, int32 Y, int32 Width, int32 Height, int32& DrawnAtX, int32& DrawnAtY)
{
	FVector2D WindowPosition;
	WindowPosition = CalculateScreenPosition(ScreenAnchorPoint, UIAnchorPoint, X, Y, Width, Height);

	//Output top left draw location for other BP nodes to use (i.e. RenderInteractiveInventoryGrid).
	DrawnAtX = WindowPosition.X;
	DrawnAtY = WindowPosition.Y;

	int32 BorderWidthHeight = WindowBorderTopLeftCorner->GetSurfaceHeight();

	DrawTexture(WindowBorderTopLeftCorner, WindowPosition.X, WindowPosition.Y, BorderWidthHeight, BorderWidthHeight, 0.0f, 0.0f, 1.0f, 1.0f);
	DrawTexture(WindowBorderTopRightCorner, WindowPosition.X + Width - BorderWidthHeight, WindowPosition.Y, BorderWidthHeight, BorderWidthHeight, 0.0f, 0.0f, 1.0f, 1.0f);
	DrawTexture(WindowBorderBottomLeftCorner, WindowPosition.X, WindowPosition.Y + Height - BorderWidthHeight, BorderWidthHeight, BorderWidthHeight, 0.0f, 0.0f, 1.0f, 1.0f);
	DrawTexture(WindowBorderBottomRightCorner, WindowPosition.X + Width - BorderWidthHeight, WindowPosition.Y + Height - BorderWidthHeight, BorderWidthHeight, BorderWidthHeight, 0.0f, 0.0f, 1.0f, 1.0f);

	if (Width > BorderWidthHeight * 2)
	{
		DrawTexture(WindowBorderTop, WindowPosition.X + BorderWidthHeight, WindowPosition.Y, Width - BorderWidthHeight * 2, BorderWidthHeight, 0.0f, 0.0f, 1.0f, 1.0f);
		DrawTexture(WindowBorderBottom, WindowPosition.X + BorderWidthHeight, WindowPosition.Y + Height - BorderWidthHeight, Width - BorderWidthHeight * 2, BorderWidthHeight, 0.0f, 0.0f, 1.0f, 1.0f);
	}
	if (Height > BorderWidthHeight * 2)
	{
		DrawTexture(WindowBorderLeft, WindowPosition.X, WindowPosition.Y + BorderWidthHeight, BorderWidthHeight, Height - BorderWidthHeight * 2, 0.0f, 0.0f, 1.0f, 1.0f);
		DrawTexture(WindowBorderRight, WindowPosition.X + Width - BorderWidthHeight, WindowPosition.Y + BorderWidthHeight, BorderWidthHeight, Height - BorderWidthHeight * 2, 0.0f, 0.0f, 1.0f, 1.0f);
	}
	if (Width > BorderWidthHeight * 2 && Height > BorderWidthHeight * 2)
	{
		DrawTexture(WindowBackground, WindowPosition.X + BorderWidthHeight, WindowPosition.Y + BorderWidthHeight, Width - BorderWidthHeight * 2, Height - BorderWidthHeight * 2, 0.0f, 0.0f, 1.0f, 1.0f);
	}
}

void AOWSHUD::RenderInteractiveDialogueChoices(AOWSCharacter* NPC, UTexture* BackgroundTexture, int32 MinSizeX, int32 MinSizeY, float FontSize)
{
	RenderInteractiveDialogueChoicesInternal(DialogueChoices, NPC, BackgroundTexture, MinSizeX, MinSizeY, FontSize);
}

void AOWSHUD::RenderInteractiveDialogueChoicesInternal(const TArray<FDialogueChoice>& ChoicesToRender, AOWSCharacter* NPC, UTexture* BackgroundTexture, int32 MinSizeX, int32 MinSizeY, float FontSize)
{
	/*
	ScreenCenterX = Canvas->SizeX / 2;
	ScreenCenterY = Canvas->SizeY / 2;
	*/

	//This stops the code from crashing when the NPC despawns (will also be fixed)
	if (!NPC)
		return;

	float fScreenCenterX = 0.0f;
	float fScreenCenterY = 0.0f;

	if (Canvas)
	{
		//Canvas->GetCenter(fScreenCenterX, fScreenCenterY);

		//float X = fScreenCenterX - MinSizeX / 2;
		//float Y = fScreenCenterY - MinSizeY / 2;
		if (!PC)
			return;

		FVector WorldPosition;
		FName HeadSocket = FName("head");

		WorldPosition = NPC->GetMesh()->GetSocketLocation(HeadSocket);

		//WorldPosition.Z += 130.f;

		FVector2D ScreenPosition;

		UGameplayStatics::ProjectWorldToScreen(PC, WorldPosition, ScreenPosition, true);

		TArray<FDialogueChoice> SortedChoices;

		for (FDialogueChoice Choice : ChoicesToRender)
		{
			SortedChoices.Add(Choice);
		}

		SortedChoices.Sort([](const FDialogueChoice& A, const FDialogueChoice& B)
		{
			float WeightA = A.Weight;
			float WeightB = B.Weight;

			return WeightA > WeightB;
		});

		float Xoffset = 0;
		float Yoffset = 0;
		int32 CurChoice = 0;

		FVector2D Position;
		FVector2D Size;

		for (FDialogueChoice Choice : SortedChoices)
		{
			//Only show up to 5 choices at once
			if (CurChoice >= 5)
				continue;

			Position.X = ScreenPosition.X + Xoffset;
			Position.Y = ScreenPosition.Y + Yoffset - 120.f;

			Size.X = (float)MinSizeX;
			Size.Y = (float)MinSizeY;

			FVector2D BoxSize = GetDialogueBoxSize(Choice.ChoiceKeyword, DialogueFont, FontSize, 300, 20, 10, 1);

			Position.X = Position.X - (BoxSize.X / 2.f);

			DrawTexture(BackgroundTexture, Position.X, Position.Y, BoxSize.X, BoxSize.Y, 0.0f, 0.0f, 1.0f, 1.0f);
			DrawText(Choice.ChoiceKeyword, FLinearColor::White, Position.X + 19.0f, Position.Y + 9.0f, DialogueFont, FontSize);

			this->AddHitBox(Position, BoxSize, FName(*Choice.ChoiceKeyword), true);

			if (CurChoice == 0)
			{
				Xoffset -= 200.f;
				Yoffset += 100.f;
			}
			else if (CurChoice == 1)
			{
				Xoffset += 400.f;
			}
			else if (CurChoice == 2)
			{
				Yoffset -= 50.f;
				Xoffset -= 340.f;
			}
			else if (CurChoice == 3)
			{
				Xoffset += 280.f;
			}

			/*
			if (CurChoice == 0)
			{
				Xoffset += 130;
			}
			else if (CurChoice == 1)
			{
				Yoffset += 70;
			}
			else if (CurChoice == 2 || CurChoice == 3)
			{
				Xoffset -= 130;
			}
			else if (CurChoice == 4 || CurChoice == 5)
			{
				Yoffset -= 70;
			}
			else if (CurChoice == 6 || CurChoice == 7 || CurChoice == 8)
			{
				Xoffset += 130;
			}
			else if (CurChoice == 11 || CurChoice == 9 || CurChoice == 10)
			{
				Yoffset += 70;
			}
			else if (CurChoice == 14 || CurChoice == 12 || CurChoice == 13 || CurChoice == 15)
			{
				Xoffset -= 130;
			}
			else if (CurChoice == 16 || CurChoice == 17 || CurChoice == 18 || CurChoice == 19)
			{
				Yoffset -= 70;
			}
			*/

			//Xoffset += FMath::RandRange(-15.0f, 15.0f);
			//Yoffset += FMath::RandRange(-10.0f, 10.0f);

			CurChoice++;
		}
	}
}

void AOWSHUD::AddDialogueChoice(FString ChoiceKeyword, FString ChoiceText)
{
	FDialogueChoice DialogueChoice;

	DialogueChoice.ChoiceKeyword = ChoiceKeyword;
	DialogueChoice.ChoiceText = ChoiceText;

	DialogueChoices.Add(DialogueChoice);
}

void AOWSHUD::ClearDialogueChoices()
{
	DialogueChoices.Empty();
}

void AOWSHUD::RenderDialogueBox(AOWSCharacter* Speaker, const enum EAnchorPoint UIAnchorPoint, const enum ESpeechBalloonStyle SpeechBalloonStyle, FString DialogueText, const float FontScale, const float OffsetX, const float OffsetY, const int32 TextWrapPoint, const bool DrawTail, const float BorderWidth)
{
	if (!Speaker)
		return;

	float TextWidth = 0.f, TextHeight = 0.f;
	GetTextSize(DialogueText, TextWidth, TextHeight, DialogueFont, FontScale);

	FVector2D BoxSize;

	FVector WorldPosition;
	FName HeadSocket = FName("head");
	WorldPosition = Speaker->GetMesh()->GetSocketLocation(HeadSocket);
	FVector2D ScreenPosition;
	UGameplayStatics::ProjectWorldToScreen(PC, WorldPosition, ScreenPosition, true);

	float PositionX = ScreenPosition.X + OffsetX;
	float PositionY = ScreenPosition.Y + OffsetY;

	if (TextWidth > (float)TextWrapPoint)
	{
		TArray<FString> DialogueWords;

		DialogueText.ParseIntoArray(DialogueWords, TEXT(" "), false);

		TArray<FString> DialogueLines;
		FString TempString = "";
		FString LastTempString = "";
		int32 CurWord = 0;
		int32 CurLine = 0;

		if (DialogueWords.Num() > 0)
		{
			while (CurWord < DialogueWords.Num())
			{
				TempString += " " + DialogueWords[CurWord];

				GetTextSize(TempString, TextWidth, TextHeight, DialogueFont, FontScale);

				if (TextWidth > TextWrapPoint)
				{
					DialogueLines.Add(LastTempString);

					TempString = "";
					CurLine++;
				}
				else
				{
					CurWord++;
				}

				LastTempString = TempString;
			}
		}
		//Add what is left over as the last line.
		DialogueLines.Add(TempString);

		//Start Rendering
		BoxSize = GetDialogueBoxSize(DialogueText, DialogueFont, FontScale, TextWrapPoint, 20, 20, DialogueLines.Num());

		//PositionY -= BoxSize.Y;

		FVector2D WindowPosition;
		WindowPosition = CalculateScreenPosition(EAnchorPoint::TopLeft, UIAnchorPoint, PositionX, PositionY, BoxSize.X, BoxSize.Y);

		PositionX = WindowPosition.X;
		PositionY = WindowPosition.Y;

		DrawRect(FLinearColor::Black, PositionX, PositionY, BoxSize.X, BoxSize.Y);

		FLinearColor SpeechBalloonBackgroundColor;
		if (SpeechBalloonStyle == ESpeechBalloonStyle::Speech)
		{
			SpeechBalloonBackgroundColor = FLinearColor::White;
		}
		else
		{
			SpeechBalloonBackgroundColor = FLinearColor::White;
		}

		DrawRect(SpeechBalloonBackgroundColor, PositionX + BorderWidth, PositionY + BorderWidth, BoxSize.X - (BorderWidth * 2), BoxSize.Y - (BorderWidth * 2));

		int32 LineNumber = 0;
		for (auto LineToRender : DialogueLines)
		{
			DrawText(LineToRender, FLinearColor::Black, PositionX + 19.f, PositionY + 19.f + (LineNumber * 26.f), DialogueFont, FontScale);

			LineNumber++;
		}
	}
	else
	{
		BoxSize = GetDialogueBoxSize(DialogueText, DialogueFont, FontScale, TextWrapPoint, 20, 20, 1);

		//PositionY -= BoxSize.Y;

		FVector2D WindowPosition;
		WindowPosition = CalculateScreenPosition(EAnchorPoint::TopLeft, UIAnchorPoint, PositionX, PositionY, BoxSize.X, BoxSize.Y);

		PositionX = WindowPosition.X;
		PositionY = WindowPosition.Y;

		DrawRect(FLinearColor::Black, PositionX, PositionY, BoxSize.X, BoxSize.Y);

		FLinearColor SpeechBalloonBackgroundColor;
		if (SpeechBalloonStyle == ESpeechBalloonStyle::Speech)
		{
			SpeechBalloonBackgroundColor = FLinearColor::White;
		}
		else
		{
			SpeechBalloonBackgroundColor = FLinearColor::White;
		}

		DrawRect(SpeechBalloonBackgroundColor, PositionX + BorderWidth, PositionY + BorderWidth, BoxSize.X - (BorderWidth * 2), BoxSize.Y - (BorderWidth * 2));

		DrawText(DialogueText, FLinearColor::Black, PositionX + 19.0f, PositionY + 19.0f, DialogueFont, FontScale);
	}

	if (DrawTail)
	{
		if (SpeechBalloonStyle == ESpeechBalloonStyle::Speech)
		{
			/*DrawLine(PositionX, PositionY + BoxSize.Y - 10.0f, PositionX - 30.0f, PositionY + BoxSize.Y + 20.0f, FLinearColor::Black, 1.0f);
			DrawLine(PositionX + 10.0f, PositionY + BoxSize.Y, PositionX - 30.0f, PositionY + BoxSize.Y + 20.0f, FLinearColor::Black, 1.0f);

			//Head / Tail
			DrawLine(PositionX + 2, PositionY + BoxSize.Y - 2, PositionX - 28.0f, PositionY + BoxSize.Y + 18.0f, FLinearColor::White, 1.0f);
			DrawLine(PositionX + 2, PositionY + BoxSize.Y - 10, PositionX - 18.0f, PositionY + BoxSize.Y + 11.0f, FLinearColor::White, 2.0f);
			DrawLine(PositionX + 8, PositionY + BoxSize.Y - 2, PositionX - 20.0f, PositionY + BoxSize.Y + 11.0f, FLinearColor::White, 2.0f);
			DrawLine(PositionX + 10, PositionY + BoxSize.Y - 10, PositionX - 8.0f, PositionY + BoxSize.Y + 5.0f, FLinearColor::White, 5.0f);*/

			DrawTextureSimple(SpeechBalloonTail, PositionX - 58.f, PositionY + BoxSize.Y - 12.f);
		}
		else
		{
			DrawTextureSimple(ThoughtBubbleTail, PositionX + BoxSize.X, PositionY + BoxSize.Y);
		}
	}

}

FVector2D AOWSHUD::GetDialogueBoxSize(const FString DialogueText, UFont* Font, const float FontScale, const int32 TextWrapPoint, const int32 PaddingX, const int32 PaddingY, const int32 NumberOfLines)
{
	FVector2D outSize;

	float TextWidth = 0.f, TextHeight = 0.f;
	GetTextSize(DialogueText, TextWidth, TextHeight, Font, FontScale);

	if (TextWidth > TextWrapPoint)
	{
		outSize.X = TextWrapPoint;
		TextHeight += 4.0f;
		outSize.Y = TextHeight * NumberOfLines;
	}
	else
	{
		outSize.X = TextWidth;
		outSize.Y = TextHeight;
	}

	outSize.X += PaddingX * 2;
	outSize.Y += PaddingY * 2;

	return outSize;
}

int32 AOWSHUD::GetEstimatedDialogueNumberOfLines(const FString DialogueText, const int32 TextWrapPoint, const float AverageLetterWidth)
{
	float TextWidth = 0.f, TextHeight = 0.f;
	TextWidth = DialogueText.Len() * AverageLetterWidth;

	if (TextWidth > (float)TextWrapPoint)
	{
		TArray<FString> DialogueWords;

		DialogueText.ParseIntoArray(DialogueWords, TEXT(" "), false);

		FString TempString = "";
		FString LastTempString = "";
		int32 CurWord = 0;
		int32 CurLine = 0;

		if (DialogueWords.Num() > 0)
		{
			while (CurWord < DialogueWords.Num())
			{
				TempString += " " + DialogueWords[CurWord];

				TextWidth = TempString.Len() * AverageLetterWidth;

				if (TextWidth > TextWrapPoint)
				{
					TempString = "";
					CurLine++;
				}
				else
				{
					CurWord++;
				}

				LastTempString = TempString;
			}
		}

		CurLine++;

		return CurLine;
	}

	return 1;
}


void AOWSHUD::AddSlotsToSkip(TSet<int32> &SlotsToSkip, const int32 SlotNumber, const int32 ItemSlotWidth, const int32 ItemSlotHeight, const int32 NumberOfColumns)
{
	//There is no need to run this method
	if (ItemSlotWidth < 2 && ItemSlotHeight < 2)
		return;

	int32 StartingRow = FMath::FloorToInt(SlotNumber / NumberOfColumns);
	int32 StartingCol = SlotNumber % NumberOfColumns;

	for (int32 CurRow = StartingRow; CurRow < StartingRow + ItemSlotHeight; CurRow++) {
		for (int32 CurCol = StartingCol; CurCol < StartingCol + ItemSlotWidth; CurCol++) {
			if (CurRow != StartingRow || CurCol != StartingCol)
			{
				SlotsToSkip.Add((CurRow * NumberOfColumns) + CurCol);
			}
		}
	}

}

void AOWSHUD::RenderInteractiveInventoryGrid(UOWSInventory* Inventory, UTexture* EmptySlotTexture, enum EAnchorPoint UIAnchorPoint, int32 X, int32 Y, int32 XSpacing, int32 YSpacing, 
	int32 iconWidth, int32 iconHeight, int32 NumberOfRows, int32 NumberOfCols)
{
	//if (GetOwningPlayerController() != NULL)
	//{
	//	ARPGCharacter* PlayerChar = Cast<ARPGCharacter>(GetOwningPlayerController()->GetPawn());
	//	UInventory* Inventory = PlayerChar->BagInventory;

	//UInventory* const Inventory = (UInventory*)InventoryObject;

	FColor FontColor;
	FVector2D* FontScale = new FVector2D(1.0f, 1.0f);

	for (int32 curRow = 0; curRow < NumberOfRows; curRow++)
	{
		for (int32 curCol = 0; curCol < NumberOfCols; curCol++)
		{
			int32 SlotNumber = (curRow * NumberOfCols) + curCol;
			FVector2D* Position = new FVector2D((curCol * iconWidth) + (curCol * XSpacing) + X, (curRow * iconHeight) + (curRow * YSpacing) + Y);
			FVector2D* Size = new FVector2D(iconWidth, iconHeight);
			FString InventoryNameStr = Inventory->InventoryName.GetPlainNameString();
			FString SplitChar = TEXT("|");
			FString SlotNumberStr = FString::FromInt(SlotNumber);
			FString InventorySlotNameStr = InventoryNameStr + SplitChar + SlotNumberStr;
			FName InventorySlotName = FName(*InventorySlotNameStr);			

			if (Inventory && Inventory->IsValidLowLevel())
			{
				UOWSInventoryItemStack* InventoryItemStack = Inventory->GetStackInSlot(SlotNumber);

				if (!InventoryItemStack)
					continue;

				AOWSInventoryItem* InventoryItem = InventoryItemStack->GetTopItemFromStack();
				if (InventoryItem && InventoryItem->IsValidLowLevel())
				{
					UTexture* Texture = InventoryItem->IconTexture;
					float CalculatedIconWidth = (float)iconWidth * (float)InventoryItem->IconSlotWidth + ((float)XSpacing * (float)(InventoryItem->IconSlotWidth - 1));
					float CalculatedIconHeight = (float)iconHeight * (float)InventoryItem->IconSlotHeight + ((float)YSpacing * (float)(InventoryItem->IconSlotHeight - 1));

					if (InventoryItem->IconSlotWidth > 1)
					{
						Size->X = CalculatedIconWidth;
						//AddSlotsToSkip(SlotsToSkip, SlotNumber, InventoryItem->IconSlotWidth, InventoryItem->IconSlotHeight, NumberOfCols);
					}
					if (InventoryItem->IconSlotHeight > 1)
					{
						Size->Y = CalculatedIconHeight;
					}

					if (InventoryItemStack->IsBeingDragged)
					{
						DrawTexture(EmptySlotTexture, Position->X, Position->Y, (float)iconWidth, (float)iconHeight, 0.0f, 0.0f, 1.0f, 1.0f);
					}
					else
					{
						DrawTexture(Texture, Position->X, Position->Y, CalculatedIconWidth, CalculatedIconHeight, 0.0f, 0.0f, 1.0f, 1.0f);
						if (InventoryItemStack->InventoryItems.Num() > 1)
						{
							if (bDrawSecondIconForStack)
							{
								DrawTexture(Texture, Position->X + StackDrawOffset, Position->Y + StackDrawOffset, CalculatedIconWidth - StackDrawOffset, CalculatedIconHeight - StackDrawOffset, 0.0f, 0.0f, 1.0f, 1.0f);
							}

							const FString StackSizeText = FString::FromInt(InventoryItemStack->InventoryItems.Num());
							DrawText(StackSizeText, FLinearColor::White, Position->X + CalculatedIconWidth - StackDrawTextOffset, Position->Y + CalculatedIconHeight - StackDrawTextOffset);
						}
					}
				}
				else if (!Inventory->IsSlotFilled(SlotNumber) || (ItemStackBeingDragged && ItemStackBeingDragged->IsBeingDragged && SlotsToShowWhileDragging.Contains(SlotNumber)))
				{
					DrawTexture(EmptySlotTexture, Position->X, Position->Y, (float)iconWidth, (float)iconHeight, 0.0f, 0.0f, 1.0f, 1.0f);
				}

				if (InventoryItemStack != ItemStackBeingDragged || (ItemStackBeingDragged && !ItemStackBeingDragged->IsBeingDragged))
				{
					this->AddHitBox(*Position, *Size, InventorySlotName, true, 99);
				}
			}
		}
	}

	if (ItemStackBeingDragged && ItemStackBeingDragged->IsBeingDragged)
	{
		AOWSInventoryItem* InventoryItem = ItemStackBeingDragged->GetTopItemFromStack();
		if (InventoryItem && InventoryItem->IsValidLowLevel())
		{
			UTexture* Texture = InventoryItem->IconTexture;
			float CalculatedIconWidth = (float)iconWidth * (float)InventoryItem->IconSlotWidth + ((float)XSpacing * (float)(InventoryItem->IconSlotWidth - 1));
			float CalculatedIconHeight = (float)iconHeight * (float)InventoryItem->IconSlotHeight + ((float)YSpacing * (float)(InventoryItem->IconSlotHeight - 1));

			DrawTexture(Texture, MouseLocation.X - ((float)iconWidth / 2), MouseLocation.Y - ((float)iconHeight / 2), (float)CalculatedIconWidth, (float)CalculatedIconHeight, 0.0f, 0.0f, 1.0f, 1.0f);
			if (ItemStackBeingDragged->InventoryItems.Num() > 1)
			{
				if (bDrawSecondIconForStack)
				{
					DrawTexture(Texture, MouseLocation.X - ((float)iconWidth / 2) + StackDrawOffset, MouseLocation.Y - ((float)iconHeight / 2) + StackDrawOffset, (float)CalculatedIconWidth - StackDrawOffset, (float)CalculatedIconHeight - StackDrawOffset, 0.0f, 0.0f, 1.0f, 1.0f);
				}

				const FString StackSizeText = FString::FromInt(ItemStackBeingDragged->InventoryItems.Num());
				DrawText(StackSizeText, FLinearColor::White, MouseLocation.X - (CalculatedIconWidth / 2) + CalculatedIconWidth - StackDrawTextOffset, MouseLocation.Y - (CalculatedIconHeight / 2) + CalculatedIconHeight - StackDrawTextOffset);
			}
		}
	}
}

void AOWSHUD::RenderInteractiveInventoryGridUsingLockedSlotGroups(UOWSInventory* Inventory, UTexture* EmptySlotTexture, UTexture* LockedRowTexture, int32 X, int32 Y,
	int32 XSpacing, int32 YSpacing, int32 iconWidth, int32 iconHeight, int32 NumberOfRows, int32 NumberOfCols, int32 SlotGroupRows, int32 SlotGroupCols, int32 SlotGroupXSpacing, int32 SlotGroupYSpacing)
{
	FColor FontColor;
	FVector2D* FontScale = new FVector2D(1.0f, 1.0f);
	int32 SlotGroupWidth = NumberOfCols * (iconWidth + XSpacing) - XSpacing;
	int32 SlotGroupHeight = NumberOfRows * (iconHeight + YSpacing) - YSpacing;

	FString InventoryNameStr = Inventory->InventoryName.GetPlainNameString();
	FString SplitChar = TEXT("|");
	int32 NumberOfGroupsUnlocked = 0;

	if (Inventory && Inventory->IsValidLowLevel())
	{
		NumberOfGroupsUnlocked = Inventory->NumberOfGroupsUnlocked;
	}

	//Draw locked icons
	for (int32 curGroupRow = 0; curGroupRow < SlotGroupRows; curGroupRow++)
	{
		for (int32 curGroupCol = 0; curGroupCol < SlotGroupCols; curGroupCol++)
		{
			int32 curGroup = (curGroupRow*SlotGroupCols) + curGroupCol;
			if (curGroup >= NumberOfGroupsUnlocked)
			{
				FVector2D* Position = new FVector2D((curGroupCol * SlotGroupWidth) + (curGroupCol * SlotGroupXSpacing),
					(curGroupRow * SlotGroupHeight) + (curGroupRow * SlotGroupYSpacing));
				DrawTexture(LockedRowTexture, Position->X + X, Position->Y + Y, (float)SlotGroupWidth, (float)SlotGroupHeight, 0.0f, 0.0f, 1.0f, 1.0f);
			}
		}
	}

	for (int32 curGroupRow = 0; curGroupRow < SlotGroupRows; curGroupRow++)
	{
		for (int32 curGroupCol = 0; curGroupCol < SlotGroupCols; curGroupCol++)
		{
			int32 curGroup = (curGroupRow*SlotGroupCols) + curGroupCol;
			if (curGroup < NumberOfGroupsUnlocked)
			{
				for (int32 curRow = 0; curRow < NumberOfRows; curRow++)
				{
					for (int32 curCol = 0; curCol < NumberOfCols; curCol++)
					{
						int32 Slot = (curGroupRow * SlotGroupCols * NumberOfCols) + (curGroupCol * NumberOfCols) + (curRow * NumberOfCols) + curCol;
						FVector2D* Position = new FVector2D((curGroupCol * SlotGroupWidth) + (curGroupCol * SlotGroupXSpacing) + (curCol * iconWidth) + (curCol * XSpacing) + X,
							(curGroupRow * SlotGroupHeight) + (curGroupRow * SlotGroupYSpacing) + (curRow * iconHeight) + (curRow * YSpacing) + Y);
						FVector2D* Size = new FVector2D(iconWidth, iconHeight);
						FString SlotNumberStr = FString::FromInt(Slot);
						FString InventorySlotNameStr = InventoryNameStr + SplitChar + SlotNumberStr;
						FName InventorySlotName = FName(*InventorySlotNameStr);

						if (Inventory && Inventory->IsValidLowLevel())
						{
							UOWSInventoryItemStack* InventoryItemStack = Inventory->GetStackInSlot(Slot);

							AOWSInventoryItem* InventoryItem = InventoryItemStack->GetTopItemFromStack();
							if (InventoryItem && InventoryItem->IsValidLowLevel())
							{
								UTexture* Texture = InventoryItem->IconTexture;

								if (InventoryItemStack->IsBeingDragged)
								{
									DrawTexture(EmptySlotTexture, Position->X, Position->Y, (float)iconWidth, (float)iconHeight, 0.0f, 0.0f, 1.0f, 1.0f);
								}
								else
								{
									DrawTexture(Texture, Position->X, Position->Y, (float)iconWidth, (float)iconHeight, 0.0f, 0.0f, 1.0f, 1.0f);
									if (InventoryItemStack->InventoryItems.Num() > 1)
									{
										DrawTexture(Texture, Position->X + 5.0f, Position->Y, (float)iconWidth - 5.0f, (float)iconHeight - 5.0f, 0.0f, 0.0f, 1.0f, 1.0f);
										const FString StackSizeText = FString::FromInt(InventoryItemStack->InventoryItems.Num());
										DrawText(StackSizeText, FLinearColor::White, Position->X + iconWidth - 15.0f, Position->Y + iconHeight - 20.0f);
									}
								}
							}
							else
							{
								DrawTexture(EmptySlotTexture, Position->X, Position->Y, (float)iconWidth, (float)iconHeight, 0.0f, 0.0f, 1.0f, 1.0f);
							}
						}

						this->AddHitBox(*Position, *Size, InventorySlotName, true, 99);
					}
				}
			}
		}
	}

	if (ItemStackBeingDragged && ItemStackBeingDragged->IsBeingDragged)
	{
		AOWSInventoryItem* InventoryItem = ItemStackBeingDragged->GetTopItemFromStack();
		if (InventoryItem && InventoryItem->IsValidLowLevel())
		{
			UTexture* Texture = InventoryItem->IconTexture;

			DrawTexture(Texture, MouseLocation.X - (iconWidth / 2), MouseLocation.Y - (iconHeight / 2), (float)iconWidth, (float)iconHeight, 0.0f, 0.0f, 1.0f, 1.0f);
			if (ItemStackBeingDragged->InventoryItems.Num() > 1)
			{
				DrawTexture(Texture, MouseLocation.X - (iconWidth / 2) + 5, MouseLocation.Y - (iconHeight / 2), (float)iconWidth - 5, (float)iconHeight - 5, 0.0f, 0.0f, 1.0f, 1.0f);
				const FString StackSizeText = FString::FromInt(ItemStackBeingDragged->InventoryItems.Num());
				DrawText(StackSizeText, FLinearColor::White, MouseLocation.X - (iconWidth / 2) + iconWidth - 15.0f, MouseLocation.Y - (iconHeight / 2) + iconHeight - 20.0f);
			}
		}
	}
}

void AOWSHUD::PostInitializeComponents()
{
	Super::PostInitializeComponents();

	PC = GetOwningPlayerController();
}


void AOWSHUD::NotifyHitBoxBeginCursorOver(FName BoxName)
{
	FString BoxNameStr = BoxName.ToString();

	auto FoundEntry = DialogueChoices.FindByPredicate([&](FDialogueChoice InItem)
	{
		return InItem.ChoiceKeyword == BoxNameStr;
	});

	if (FoundEntry)
	{
		OnDialogueChoiceHovered(*FoundEntry);
	}
}

void AOWSHUD::NotifyHitBoxEndCursorOver(FName BoxName)
{
	FString BoxNameStr = BoxName.ToString();

	auto FoundEntry = DialogueChoices.FindByPredicate([&](FDialogueChoice InItem)
	{
		return InItem.ChoiceKeyword == BoxNameStr;
	});

	if (FoundEntry)
	{
		OnDialogueChoiceBlur(*FoundEntry);
	}
}

void AOWSHUD::NotifyHitBoxClick(const FName BoxName)
{
	if (BoxName == "StackSizeDownButton")
	{
		if (SplitNumber > 1)
			SplitNumber--;
	}
	else if (BoxName == "StackSizeUpButton")
	{
		if (SplitNumber < StackToSplitSize - 1)
			SplitNumber++;
	}
	else if (BoxName == "SplitStackButton")
	{
		if (InventoryItemStackToSplit)
		{
			UOWSInventory* Inventory = OWSChar->GetHUDInventoryFromName(InventoryBeingDraggedFrom);
			if (Inventory)
			{
				AOWSInventoryItem* SplitItem = InventoryItemStackToSplit->RemoveFromTopOfStack();
				if (SplitItem)
				{
					int32 Slot = Inventory->FindFirstEmptySlotToFitItemOfSize(SplitItem->IconSlotWidth, SplitItem->IconSlotHeight);
					if (Slot != -1)
					{
						Inventory->AddItemToSlot(SplitItem, Slot);
					}
				}
			}
		}
		SplitDialogOpen = false;
	}
	else if (BoxName == "CancelStackButton")
	{
		SplitDialogOpen = false;
	}
	else if (!SplitDialogOpen)
	{
		FName InventoryName = FName("");
		int32 Slot = 0;
		GetInventoryNameAndSlot(BoxName, InventoryName, Slot);

		if (InventoryName.IsValid())
		{

			if (PC)
			{
				APawn* Pawn = PC->GetPawn();
				if (Pawn)
				{
					OWSChar = Cast<AOWSCharacter>(Pawn);
				}
			}

			if (!OWSChar) return;

			UOWSInventory* Inventory = OWSChar->GetHUDInventoryFromName(InventoryName);

			if (Inventory)
			{
				UOWSInventoryItemStack* InventoryItemStack = Inventory->GetStackInSlot(Slot);
				if (InventoryItemStack)
				{
					AOWSInventoryItem* ItemToStartDragging = InventoryItemStack->GetTopItemFromStack();

					//Don't drag an empty slot
					if (ItemToStartDragging)
					{
						InventoryBeingDraggedFrom = InventoryName;
						SlotBeingDraggedFrom = Slot;
						ItemStackBeingDragged = InventoryItemStack;
						ItemStackBeingDragged->IsBeingDragged = true;
						SlotsToShowWhileDragging.Empty();
						if (ItemToStartDragging->IconSlotWidth > 1 || ItemToStartDragging->IconSlotHeight > 1)
						{
							int32 StartingRow = FMath::FloorToInt(Slot / Inventory->NumberOfColumns);
							int32 StartingCol = Slot % Inventory->NumberOfColumns;

							for (int32 CurRow = StartingRow; CurRow < StartingRow + ItemToStartDragging->IconSlotHeight; CurRow++) {
								for (int32 CurCol = StartingCol; CurCol < StartingCol + ItemToStartDragging->IconSlotWidth; CurCol++) {
									if (CurRow != StartingRow || CurCol != StartingCol)
									{
										SlotsToShowWhileDragging.Add((CurRow * Inventory->NumberOfColumns) + CurCol);
									}
								}
							}
						}
					}
				}
			}
		}
	}

	// dispatch BP event
	ReceiveHitBoxClick(BoxName);
}


void AOWSHUD::DrawHUD()
{
	if (!PC)
	{
		PC = GetOwningPlayerController();
		if (!PC) return;
	}

	if (!PC->PlayerInput) return;

	GetInput();
	RemoveExpiredSpeakers();

	Super::DrawHUD();

	if (SplitDialogOpen)
		DrawSplitDialog();
}


void AOWSHUD::GetInput()
{
	PC->GetMousePosition(MouseLocation.X, MouseLocation.Y);

	if (PC->WasInputKeyJustReleased(EKeys::LeftMouseButton))
	{
		if (ItemStackBeingDragged)
		{
			ItemStackBeingDragged->IsBeingDragged = false;
			if (HitBoxesOver.Num() > 0)
			{
				FString BoxNameStr = HitBoxesOver.Array().Top().GetPlainNameString();
				FName BoxName = FName(*BoxNameStr);
				FName InventoryName = FName("");
				int32 Slot = 0;
				GetInventoryNameAndSlot(BoxName, InventoryName, Slot);
				UOWSInventory* Inventory = OWSChar->GetHUDInventoryFromName(InventoryName);
				if (Inventory)
				{
					UOWSInventory* SourceInventory = OWSChar->GetHUDInventoryFromName(InventoryBeingDraggedFrom);
					UOWSInventoryItemStack* InventoryItemStackSource = SourceInventory->GetStackInSlot(SlotBeingDraggedFrom);
					UOWSInventoryItemStack* InventoryItemStackDest = Inventory->GetStackInSlot(Slot);

					if (InventoryBeingDraggedFrom == InventoryName && Slot != SlotBeingDraggedFrom)
					{
						AOWSInventoryItem* SourceItem = InventoryItemStackSource->GetTopItemFromStack();
						AOWSInventoryItem* DestItem = InventoryItemStackDest->GetTopItemFromStack();
						if (DestItem)
						{
							if (SourceItem->UniqueItemGUID == DestItem->UniqueItemGUID
								&& SourceItem->CanStack
								&& InventoryItemStackDest->InventoryItems.Num() < DestItem->StackSize)
							{
								InventoryItemStackDest->AddToStack(InventoryItemStackSource);
								SourceInventory->RemoveStackFromSlot(SlotBeingDraggedFrom);
								OWSChar->SerializeAndSaveInventory(InventoryName);
							}
							else
							{
								Inventory->SwapSlots(Slot, SlotBeingDraggedFrom);
								OWSChar->SerializeAndSaveInventory(InventoryName);
							}
						}
						else
						{
							Inventory->SwapSlots(Slot, SlotBeingDraggedFrom);
							OWSChar->SerializeAndSaveInventory(InventoryName);
						}
					}
					else if (InventoryBeingDraggedFrom != InventoryName)
					{
						SourceInventory->AddStackToSlot(InventoryItemStackDest, SlotBeingDraggedFrom);
						Inventory->AddStackToSlot(InventoryItemStackSource, Slot);
						OWSChar->SerializeAndSaveInventory(InventoryName);
					}

				}
			}
		}
		else
		{
			if (HitBoxesOver.Num() > 0)
			{
				FString BoxNameStr = HitBoxesOver.Array().Top().GetPlainNameString();
				FName BoxName = FName(*BoxNameStr);

				auto FoundEntry = DialogueChoices.FindByPredicate([&](FDialogueChoice InItem)
				{
					return InItem.ChoiceKeyword == BoxNameStr;
				});

				/*auto FoundEntry = DialogResponses.FindByPredicate([&](UOWSDialogueResponse* InItem)
				{
					//return InItem->Choices.Contains(InputString);
					return InItem->Choices.ContainsByPredicate([&](FOWSDialogueChoice InChoice) { return InChoice.Keyword == InputString; });
				});*/

				if (FoundEntry)
				{
					OnDialogueChoiceSelected(*FoundEntry);
				}
			}
		}
	}
	else if (PC->WasInputKeyJustReleased(EKeys::RightMouseButton))
	{
		if (HitBoxesOver.Num() > 0)
		{
			if (PC && !OWSChar)
			{
				APawn* Pawn = PC->GetPawn();
				if (Pawn)
				{
					OWSChar = Cast<AOWSCharacter>(Pawn);
				}
			}

			if (!OWSChar) return;

			FString BoxNameStr = HitBoxesOver.Array().Top().GetPlainNameString();
			FName BoxName = FName(*BoxNameStr);
			FName InventoryName = FName("");
			int32 Slot = 0;
			GetInventoryNameAndSlot(BoxName, InventoryName, Slot);
			UOWSInventory* Inventory = OWSChar->GetHUDInventoryFromName(InventoryName);
			if (Inventory)
			{
				InventoryItemStackToSplit = Inventory->GetStackInSlot(Slot);
				StackToSplitSize = InventoryItemStackToSplit->InventoryItems.Num();
				if (StackToSplitSize > 1)
					SplitDialogOpen = true;
			}
		}
	}
}

void AOWSHUD::RemoveExpiredSpeakers()
{
	if (Speakers.Num() < 1)
		return;

	for (int CurSpeaker = Speakers.Num() - 1; CurSpeaker >= 0; CurSpeaker--)
	{
		if (GetWorld()->GetRealTimeSeconds() > Speakers[CurSpeaker].SpokenStartTime + Speakers[CurSpeaker].SpokenDuration)
		{
			Speakers.RemoveAt(CurSpeaker);
		}
	}
}

void AOWSHUD::GetInventoryNameAndSlot(const FName BoxName, FName &InventoryName, int32 &Slot)
{
	FString SlotName = BoxName.GetPlainNameString();
	//print(SlotName);

	FString InventoryNameString = "";
	FString SlotNumberString = "";
	SlotName.Split("|", &InventoryNameString, &SlotNumberString);
	//print(InventoryNameString);
	//print(SlotNumberString);
	InventoryName = FName(*InventoryNameString);
	if (SlotNumberString.IsNumeric())
	{
		Slot = FCString::Atoi(*SlotNumberString);
	}
}

void AOWSHUD::DrawSplitDialog()
{
	if (SplitDialogTexture)
	{
		int32 ScreenCenterX = Canvas->SizeX / 2;
		int32 ScreenCenterY = Canvas->SizeY / 2;

		FVector2D* Position = new FVector2D(ScreenCenterX - 100.0f, ScreenCenterY - 41.0f);
		FVector2D* Size = new FVector2D(200.0f, 82.0f);

		DrawTexture(SplitDialogTexture, Position->X, Position->Y, Size->X, Size->Y, 0.0f, 0.0f, 1.0f, 1.0f);

		const FString StackSizeText = FString::FromInt(SplitNumber);
		DrawText(StackSizeText, FLinearColor::White, ScreenCenterX - 12.0f, ScreenCenterY - 23.0f, (UFont*)0, 3.0f);

		FVector2D* UpButtonPosition = new FVector2D(ScreenCenterX - 95.0f, ScreenCenterY - 30.0f);
		FVector2D* DownButtonPosition = new FVector2D(ScreenCenterX + 35.0f, ScreenCenterY - 30.0f);
		FVector2D* ButtonSize = new FVector2D(60.0f, 60.0f);

		this->AddHitBox(*UpButtonPosition, *ButtonSize, "StackSizeUpButton", true, 99);
		this->AddHitBox(*DownButtonPosition, *ButtonSize, "StackSizeDownButton", true, 99);

		FVector2D* CancelButtonPosition = new FVector2D(ScreenCenterX - 100.0f, ScreenCenterY + 42.0f);
		FVector2D* SplitButtonPosition = new FVector2D(ScreenCenterX + 1.0f, ScreenCenterY + 42.0f);
		FVector2D* ConfirmButtonSize = new FVector2D(100.0f, 40.0f);

		this->AddHitBox(*SplitButtonPosition, *ConfirmButtonSize, "SplitStackButton", true, 99);
		this->AddHitBox(*CancelButtonPosition, *ConfirmButtonSize, "CancelStackButton", true, 99);
	}

}

