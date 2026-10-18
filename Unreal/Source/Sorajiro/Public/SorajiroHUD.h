#pragma once
#include "CoreMinimal.h"
#include "GameFramework/HUD.h"
#include "SorajiroHUD.generated.h"

UCLASS()
class SORAJIRO_API ASorajiroHUD : public AHUD
{
    GENERATED_BODY()
public:
    virtual void DrawHUD() override;
};
