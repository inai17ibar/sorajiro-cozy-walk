#include "SorajiroGameMode.h"
#include "SorajiroCharacter.h"
#include "SorajiroPlayerController.h"
#include "SorajiroHUD.h"

ASorajiroGameMode::ASorajiroGameMode()
{
    DefaultPawnClass = ASorajiroCharacter::StaticClass();
    PlayerControllerClass = ASorajiroPlayerController::StaticClass();
    HUDClass = ASorajiroHUD::StaticClass();
}
