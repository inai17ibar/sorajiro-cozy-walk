#include "SorajiroPlayerController.h"
#include "SorajiroCharacter.h"
#include "Logic/CameraPolicy.h"
#include "Components/InputComponent.h"
#include "Engine/World.h"
#include "GameFramework/GameModeBase.h"
#include "GameFramework/PlayerStart.h"
#include "GameFramework/PlayerInput.h"
#include "Kismet/GameplayStatics.h"

void ASorajiroPlayerController::BeginPlay()
{
    Super::BeginPlay();
    SetInputMode(FInputModeGameOnly());
    bShowMouseCursor = false;
}

void ASorajiroPlayerController::OnPossess(APawn* InPawn)
{
    Super::OnPossess(InPawn);
    if (InPawn) SetControlRotation(FRotator(Sorajiro::CameraPolicy::DefaultPitch,
                                           InPawn->GetActorRotation().Yaw, 0));
}

void ASorajiroPlayerController::SetupInputComponent()
{
    Super::SetupInputComponent();
    // Key bindings on the controller keep pause available when pawn actions stop.
    FInputKeyBinding& Pause = InputComponent->BindKey(EKeys::Escape, IE_Pressed, this,
                                                   &ASorajiroPlayerController::TogglePause);
    Pause.bExecuteWhenPaused = true;
    InputComponent->BindKey(EKeys::R, IE_Pressed, this, &ASorajiroPlayerController::ReturnToStart);
}

void ASorajiroPlayerController::TogglePause()
{
    if (ASorajiroCharacter* Character = Cast<ASorajiroCharacter>(GetPawn()))
        Character->ResetTransientInput();
    if (PlayerInput) PlayerInput->FlushPressedKeys();
    SetPause(!UGameplayStatics::IsGamePaused(this));
    SetInputMode(FInputModeGameOnly());
}

void ASorajiroPlayerController::ReturnToStart()
{
    ASorajiroCharacter* Character = Cast<ASorajiroCharacter>(GetPawn());
    AGameModeBase* Mode = GetWorld()->GetAuthGameMode();
    AActor* Start = Mode ? Mode->FindPlayerStart(this) : nullptr;
    if (!Character || !Start) return;
    Character->ResetTransientInput();
    if (PlayerInput) PlayerInput->FlushPressedKeys();
    Character->TeleportTo(Start->GetActorLocation(), Start->GetActorRotation());
}
