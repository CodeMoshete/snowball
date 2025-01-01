using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GamepadControlScheme : IControlScheme
{
    private const float LOOK_MULT_MIN = 0.05f;
    private const float LOOK_MULT_MAX = 1f;

    private Action<Vector2> updateLook;
    private Action<Vector2> updateMovement;
    private Action onThrow;
    private Action onJump;
    private Action onSpawnWall;
    private Action onEscape;
    private float lookMultiplier = (LOOK_MULT_MIN + LOOK_MULT_MAX) / 2f;
    private Gamepad currentGamepad;

    public void Initialize(
        Action<Vector2> onUpdateLook, 
        Action<Vector2> onUpdateMovement, 
        Action onThrow, 
        Action onJump, 
        Action onSpawnWall, 
        Action onEscape)
    {
        updateLook = onUpdateLook;
        updateMovement = onUpdateMovement;
        this.onThrow = onThrow;
        this.onJump = onJump;
        this.onSpawnWall = onSpawnWall;
        this.onEscape = onEscape;

        currentGamepad = Gamepad.current;

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
        float speedMult = Constants.MOVE_SPEED * dt;

        Vector2 leftStickVal = currentGamepad.leftStick.value;
        if (leftStickVal.x != 0f || leftStickVal.y != 0f)
        {
            moveDelta = leftStickVal * speedMult;
            updateMovement(moveDelta);
        }
        Debug.Log($"Left stick: {leftStickVal}");

        Vector2 rightStickVal = currentGamepad.rightStick.value;
        Debug.Log($"Right stick: {rightStickVal}");
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

        if (currentGamepad.yButton.wasPressedThisFrame)
        {
            onSpawnWall();
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
