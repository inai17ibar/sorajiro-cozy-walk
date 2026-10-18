#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Character.h"
#include "SorajiroCharacter.generated.h"

class UCameraComponent;
class USpringArmComponent;
class UStaticMeshComponent;
class UMaterialInterface;
class UInputAction;
class UInputMappingContext;
class UEnhancedInputLocalPlayerSubsystem;
struct FInputActionValue;

UCLASS(Config=Game)
class SORAJIRO_API ASorajiroCharacter : public ACharacter
{
    GENERATED_BODY()

public:
    ASorajiroCharacter();
    virtual void SetupPlayerInputComponent(UInputComponent* PlayerInputComponent) override;
    virtual void BeginPlay() override;
    virtual void EndPlay(const EEndPlayReason::Type EndPlayReason) override;
    void ResetTransientInput();

protected:
    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Camera")
    TObjectPtr<USpringArmComponent> CameraBoom;
    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Camera")
    TObjectPtr<UCameraComponent> FollowCamera;
    UPROPERTY(VisibleAnywhere, Category="M1 Placeholder")
    TObjectPtr<UStaticMeshComponent> PlaceholderBody;
    UPROPERTY(Config, EditDefaultsOnly, Category="M1 Placeholder")
    TSoftObjectPtr<UMaterialInterface> PlaceholderMaterial;
    UPROPERTY(EditDefaultsOnly, Category="Movement", meta=(ClampMin="1"))
    float WalkSpeed = 70.0f;
    UPROPERTY(EditDefaultsOnly, Category="Movement", meta=(ClampMin="1"))
    float RunSpeed = 120.0f;
    UPROPERTY(EditDefaultsOnly, Category="Camera", meta=(ClampMin="0.01", ClampMax="5"))
    float CameraSensitivity = 0.2f;

private:
    UPROPERTY(Transient) TObjectPtr<UInputMappingContext> Mapping;
    UPROPERTY(Transient) TArray<TObjectPtr<UInputAction>> Actions;
    UPROPERTY(Transient) TObjectPtr<UInputAction> ForwardAction;
    UPROPERTY(Transient) TObjectPtr<UInputAction> RightAction;
    UPROPERTY(Transient) TObjectPtr<UInputAction> LookXAction;
    UPROPERTY(Transient) TObjectPtr<UInputAction> LookYAction;
    UPROPERTY(Transient) TObjectPtr<UInputAction> ZoomAction;
    UPROPERTY(Transient) TObjectPtr<UInputAction> JumpAction;
    UPROPERTY(Transient) TObjectPtr<UInputAction> RunAction;
    UPROPERTY(Transient) TObjectPtr<UInputAction> DragAction;
    UPROPERTY(Transient) TWeakObjectPtr<UEnhancedInputLocalPlayerSubsystem> InputSubsystem;
    bool bDragging = false;

    void CreateMappings();
    void MoveForward(const FInputActionValue& Value);
    void MoveRight(const FInputActionValue& Value);
    void LookX(const FInputActionValue& Value);
    void LookY(const FInputActionValue& Value);
    void Zoom(const FInputActionValue& Value);
    void StartJump();
    void EndJump();
    void StartRun();
    void EndRun();
    void StartDrag();
    void EndDrag();
};
