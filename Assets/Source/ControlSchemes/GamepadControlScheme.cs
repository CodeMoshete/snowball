using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GamepadControlScheme : IControlScheme
{
    private const float LOOK_MULT_MIN = 0.05f;
    private const float LOOK_MULT_MAX = 4f;

    private Action<Vector2> updateLook;
    private Action<Vector2> updateMovement;
    private Action onThrow;
    private Action onCycleAmmo;
    private Action onJump;
    private Action onSpawnWall;
    private Action onNextWall;
    private Action onPrevWall;
    private Action onEscape;
    private float lookMultiplier = (LOOK_MULT_MIN + LOOK_MULT_MAX) / 2f;
    private Gamepad currentGamepad;
    private bool isMoving;

    public void Initialize(
        Action<Vector2> onUpdateLook, 
        Action<Vector2> onUpdateMovement, 
        Action onThrow,
        Action onCycleAmmo,
        Action onJump, 
        Action onSpawnWall, 
        Action onNextWall,
        Action onPrevWall,
        Action onEscape)
    {
        updateLook = onUpdateLook;
        updateMovement = onUpdateMovement;
        this.onThrow = onThrow;
        this.onCycleAmmo = onCycleAmmo;
        this.onJump = onJump;
        this.onSpawnWall = onSpawnWall;
        this.onNextWall = onNextWall;
        this.onPrevWall = onPrevWall;
        this.onEscape = onEscape;

        currentGamepad = Gamepad.current;

        // foreach (var device in InputSystem.devices)
        // {
        //     Debug.Log($"Device: {device.displayName}, Type: {device.GetType()}");
        //     if (device.displayName.IndexOf("Steam") >= 0 && device is Gamepad)
        //     {
        //         Debug.Log("Found Steam Gamepad!");
        //         currentGamepad = (Gamepad)device;
        //         break;
        //     }
        // }

        Service.UpdateManager.AddObserver(OnUpdate);
        Service.EventManager.AddListener(EventId.GameStateChanged, OnGameStateChanged);
        Service.EventManager.AddListener(EventId.OnLookSpeedUpdated, OnLookSpeedUpdated);
    }

    private bool OnGameStateChanged(object cookie)
    {
        GameState gameState = (GameState)cookie;
        Cursor.lockState = gameState == GameState.Gameplay ? CursorLockMode.Locked : CursorLockMode.None;
        return false;
    }

    private bool OnLookSpeedUpdated(object cookie)
    {
        float lookSpeed = (float)cookie;
        lookMultiplier = Mathf.Lerp(LOOK_MULT_MIN, LOOK_MULT_MAX, lookSpeed);
        return false;
    }

    private void OnUpdate(float dt)
    {
        Vector2 lookDelta = Vector2.zero;
        Vector2 moveDelta = Vector2.zero;
        float speedMult = Constants.MoveSpeed * dt;

        Vector2 leftStickVal = currentGamepad.leftStick.value;
        if (leftStickVal.x != 0f || leftStickVal.y != 0f)
        {
            isMoving = true;
            moveDelta = leftStickVal * speedMult;
            updateMovement(moveDelta);
        }
        else if (isMoving)
        {
            isMoving = false;
            updateMovement(moveDelta);
        }
        Debug.Log($"Left stick: {leftStickVal}");

        Vector2 rightStickVal = currentGamepad.rightStick.value;
        // Debug.Log($"Right stick: {rightStickVal}");
        if (rightStickVal.x != 0f || rightStickVal.y != 0f)
        {
            lookDelta = rightStickVal * lookMultiplier;
            updateLook(new Vector2(lookDelta.x, lookDelta.y));
        }

        if (currentGamepad.rightTrigger.wasPressedThisFrame)
        {
            onThrow();
        }

        if (currentGamepad.aButton.wasPressedThisFrame)
        {
            onJump();
        }

        if (currentGamepad.xButton.wasPressedThisFrame)
        {
            onSpawnWall();
        }

        if (currentGamepad.yButton.wasPressedThisFrame)
        {
            onCycleAmmo();
        }
        
        if (currentGamepad.rightShoulder.wasPressedThisFrame)
        {
            onNextWall();
        }

        if (currentGamepad.leftShoulder.wasPressedThisFrame)
        {
            onPrevWall();
        }

        if (currentGamepad.startButton.wasPressedThisFrame)
        {
            onEscape();
        }
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
