#include "SorajiroCharacter.h"
#include "Logic/CameraPolicy.h"
#include "Camera/CameraComponent.h"
#include "Components/CapsuleComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Engine/LocalPlayer.h"
#include "Engine/StaticMesh.h"
#include "EnhancedInputComponent.h"
#include "EnhancedInputSubsystems.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "GameFramework/PlayerController.h"
#include "GameFramework/SpringArmComponent.h"
#include "InputAction.h"
#include "InputActionValue.h"
#include "InputMappingContext.h"
#include "InputModifiers.h"
#include "Materials/MaterialInterface.h"
#include "UObject/ConstructorHelpers.h"

ASorajiroCharacter::ASorajiroCharacter()
{
    // Keep the inherited Character tick; no additional gameplay Tick is introduced.
    GetCapsuleComponent()->InitCapsuleSize(12.0f, 20.0f);
    bUseControllerRotationYaw = false;
    GetCharacterMovement()->bOrientRotationToMovement = true;
    GetCharacterMovement()->RotationRate = FRotator(0.0f, 540.0f, 0.0f);
    GetCharacterMovement()->MaxWalkSpeed = WalkSpeed;
    GetCharacterMovement()->JumpZVelocity = 180.0f;
    GetCharacterMovement()->AirControl = 0.25f;
    GetCharacterMovement()->MaxStepHeight = 6.0f;
    GetCharacterMovement()->BrakingDecelerationWalking = 500.0f;
    JumpMaxCount = 1;

    // Collision-sized visual only. Full-body bear, rig and pile are M2/M3 work.
    PlaceholderBody = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("M1_NoFurPlaceholder"));
    PlaceholderBody->SetupAttachment(GetRootComponent());
    PlaceholderBody->SetCollisionEnabled(ECollisionEnabled::NoCollision);
    PlaceholderBody->SetRelativeScale3D(FVector(0.24f, 0.24f, 0.40f));
    static ConstructorHelpers::FObjectFinder<UStaticMesh> Sphere(TEXT("/Engine/BasicShapes/Sphere.Sphere"));
    if (Sphere.Succeeded()) PlaceholderBody->SetStaticMesh(Sphere.Object);

    CameraBoom = CreateDefaultSubobject<USpringArmComponent>(TEXT("CameraBoom"));
    CameraBoom->SetupAttachment(GetRootComponent());
    CameraBoom->TargetArmLength = Sorajiro::CameraPolicy::DefaultDistance;
    CameraBoom->TargetOffset = FVector(0, 0, 12);
    CameraBoom->bUsePawnControlRotation = true;
    CameraBoom->bDoCollisionTest = true;
    CameraBoom->ProbeSize = 4.0f;
    FollowCamera = CreateDefaultSubobject<UCameraComponent>(TEXT("FollowCamera"));
    FollowCamera->SetupAttachment(CameraBoom, USpringArmComponent::SocketName);
    FollowCamera->bUsePawnControlRotation = false;
    FollowCamera->FieldOfView = 60.0f;
}

void ASorajiroCharacter::BeginPlay()
{
    Super::BeginPlay();
    if (UMaterialInterface* Material = PlaceholderMaterial.LoadSynchronous())
        PlaceholderBody->SetMaterial(0, Material);
    else
        UE_LOG(LogTemp, Warning, TEXT("M1 preview material missing. Run editor asset generation; visual remains placeholder."));
    if (Controller)
        Controller->SetControlRotation(FRotator(Sorajiro::CameraPolicy::DefaultPitch, GetActorRotation().Yaw, 0));
}

void ASorajiroCharacter::CreateMappings()
{
    Mapping = NewObject<UInputMappingContext>(this);
    auto MakeAction = [this](EInputActionValueType Type)
    {
        UInputAction* Action = NewObject<UInputAction>(this);
        Action->ValueType = Type;
        Actions.Add(Action);
        return Action;
    };
    ForwardAction = MakeAction(EInputActionValueType::Axis1D);
    RightAction = MakeAction(EInputActionValueType::Axis1D);
    // Opposite keys cancel instead of choosing whichever mapping was evaluated last.
    ForwardAction->AccumulationBehavior = EInputActionAccumulationBehavior::Cumulative;
    RightAction->AccumulationBehavior = EInputActionAccumulationBehavior::Cumulative;
    LookXAction = MakeAction(EInputActionValueType::Axis1D);
    LookYAction = MakeAction(EInputActionValueType::Axis1D);
    ZoomAction = MakeAction(EInputActionValueType::Axis1D);
    JumpAction = MakeAction(EInputActionValueType::Boolean);
    RunAction = MakeAction(EInputActionValueType::Boolean);
    DragAction = MakeAction(EInputActionValueType::Boolean);
    auto Map = [this](UInputAction* Action, FKey Key, bool bNegative = false)
    {
        FEnhancedActionKeyMapping& KeyMapping = Mapping->MapKey(Action, Key);
        if (bNegative) KeyMapping.Modifiers.Add(NewObject<UInputModifierNegate>(Mapping));
    };
    Map(ForwardAction, EKeys::W); Map(ForwardAction, EKeys::Up);
    Map(ForwardAction, EKeys::S, true); Map(ForwardAction, EKeys::Down, true);
    Map(RightAction, EKeys::D); Map(RightAction, EKeys::Right);
    Map(RightAction, EKeys::A, true); Map(RightAction, EKeys::Left, true);
    Map(LookXAction, EKeys::MouseX); Map(LookYAction, EKeys::MouseY);
    Map(ZoomAction, EKeys::MouseWheelAxis);
    Map(JumpAction, EKeys::SpaceBar);
    Map(RunAction, EKeys::LeftShift); Map(RunAction, EKeys::RightShift);
    Map(DragAction, EKeys::RightMouseButton);
}

void ASorajiroCharacter::SetupPlayerInputComponent(UInputComponent* PlayerInputComponent)
{
    Super::SetupPlayerInputComponent(PlayerInputComponent);
    UEnhancedInputComponent* Input = Cast<UEnhancedInputComponent>(PlayerInputComponent);
    APlayerController* PC = Cast<APlayerController>(Controller);
    if (!ensure(Input && PC && PC->GetLocalPlayer())) return;
    if (!Mapping) CreateMappings();
    InputSubsystem = PC->GetLocalPlayer()->GetSubsystem<UEnhancedInputLocalPlayerSubsystem>();
    if (!ensure(InputSubsystem.IsValid())) return;
    InputSubsystem->RemoveMappingContext(Mapping);
    InputSubsystem->AddMappingContext(Mapping, 0);
    Input->BindAction(ForwardAction, ETriggerEvent::Triggered, this, &ASorajiroCharacter::MoveForward);
    Input->BindAction(RightAction, ETriggerEvent::Triggered, this, &ASorajiroCharacter::MoveRight);
    Input->BindAction(LookXAction, ETriggerEvent::Triggered, this, &ASorajiroCharacter::LookX);
    Input->BindAction(LookYAction, ETriggerEvent::Triggered, this, &ASorajiroCharacter::LookY);
    Input->BindAction(ZoomAction, ETriggerEvent::Triggered, this, &ASorajiroCharacter::Zoom);
    Input->BindAction(JumpAction, ETriggerEvent::Started, this, &ASorajiroCharacter::StartJump);
    Input->BindAction(RunAction, ETriggerEvent::Started, this, &ASorajiroCharacter::StartRun);
    Input->BindAction(DragAction, ETriggerEvent::Started, this, &ASorajiroCharacter::StartDrag);
    for (ETriggerEvent EndEvent : {ETriggerEvent::Completed, ETriggerEvent::Canceled})
    {
        Input->BindAction(JumpAction, EndEvent, this, &ASorajiroCharacter::EndJump);
        Input->BindAction(RunAction, EndEvent, this, &ASorajiroCharacter::EndRun);
        Input->BindAction(DragAction, EndEvent, this, &ASorajiroCharacter::EndDrag);
    }
}

void ASorajiroCharacter::EndPlay(const EEndPlayReason::Type EndPlayReason)
{
    if (InputSubsystem.IsValid() && Mapping) InputSubsystem->RemoveMappingContext(Mapping);
    Super::EndPlay(EndPlayReason);
}

void ASorajiroCharacter::MoveForward(const FInputActionValue& Value)
{
    if (Controller) AddMovementInput(FRotationMatrix(FRotator(0, Controller->GetControlRotation().Yaw, 0))
                                    .GetUnitAxis(EAxis::X), FMath::Clamp(Value.Get<float>(), -1.0f, 1.0f));
}

void ASorajiroCharacter::MoveRight(const FInputActionValue& Value)
{
    if (Controller) AddMovementInput(FRotationMatrix(FRotator(0, Controller->GetControlRotation().Yaw, 0))
                                    .GetUnitAxis(EAxis::Y), FMath::Clamp(Value.Get<float>(), -1.0f, 1.0f));
}

void ASorajiroCharacter::LookX(const FInputActionValue& Value)
{
    if (!bDragging || !Controller) return;
    FRotator Rotation = Controller->GetControlRotation();
    Rotation.Yaw = FRotator::NormalizeAxis(Rotation.Yaw + Value.Get<float>() * CameraSensitivity);
    Controller->SetControlRotation(Rotation);
}

void ASorajiroCharacter::LookY(const FInputActionValue& Value)
{
    if (!bDragging || !Controller) return;
    FRotator Rotation = Controller->GetControlRotation();
    Rotation.Pitch = Sorajiro::CameraPolicy::PitchAfterInput(
        FRotator::NormalizeAxis(Rotation.Pitch), Value.Get<float>(), CameraSensitivity);
    Controller->SetControlRotation(Rotation);
}

void ASorajiroCharacter::Zoom(const FInputActionValue& Value)
{
    CameraBoom->TargetArmLength = Sorajiro::CameraPolicy::DistanceAfterScroll(
        CameraBoom->TargetArmLength, Value.Get<float>());
}
void ASorajiroCharacter::StartJump() { Jump(); }
void ASorajiroCharacter::EndJump() { StopJumping(); }
void ASorajiroCharacter::StartRun() { GetCharacterMovement()->MaxWalkSpeed = RunSpeed; }
void ASorajiroCharacter::EndRun() { GetCharacterMovement()->MaxWalkSpeed = WalkSpeed; }
void ASorajiroCharacter::StartDrag() { bDragging = true; }
void ASorajiroCharacter::EndDrag() { bDragging = false; }
void ASorajiroCharacter::ResetTransientInput()
{
    EndRun(); EndDrag(); EndJump();
    ConsumeMovementInputVector();
    GetCharacterMovement()->StopMovementImmediately();
}
