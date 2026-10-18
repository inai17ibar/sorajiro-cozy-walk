#pragma once
#include "CoreMinimal.h"
#include "GameFramework/PlayerController.h"
#include "SorajiroPlayerController.generated.h"

UCLASS()
class SORAJIRO_API ASorajiroPlayerController : public APlayerController
{
    GENERATED_BODY()
public:
    virtual void BeginPlay() override;
    virtual void OnPossess(APawn* InPawn) override;
    virtual void SetupInputComponent() override;
private:
    void TogglePause();
    void ReturnToStart();
};
