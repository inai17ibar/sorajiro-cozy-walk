#include "SorajiroHUD.h"
#include "Kismet/GameplayStatics.h"

void ASorajiroHUD::DrawHUD()
{
    Super::DrawHUD();
    // Temporary diagnostic label; Japanese product UI and font are later work.
    DrawText(TEXT("M1 PLACEHOLDER | NO FUR / NO BEAR RIG"), FLinearColor::Yellow, 16, 16);
    DrawText(TEXT("WASD / Arrows: Move | Shift: Run | Space: Jump"), FLinearColor::White, 16, 40);
    DrawText(TEXT("RMB Drag: Orbit | Wheel: Zoom | R: Return | Esc: Pause"), FLinearColor::White, 16, 64);
    if (UGameplayStatics::IsGamePaused(this))
        DrawText(TEXT("PAUSED | Esc: Resume"), FLinearColor::Yellow, 16, 88);
}
