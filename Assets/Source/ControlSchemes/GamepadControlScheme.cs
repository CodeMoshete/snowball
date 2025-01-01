using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GamepadControlScheme : IControlScheme
{
    private const float LOOK_MULT_MIN = 50f;
    private const float LOOK_MULT_MAX = 450f;

    private Action<Vector2> updateLook;
    private Action<Vector2> updateMovement;
    private Action onThrow;
    private Action onJump;
    private Action onSpawnWall;
    private Action onEscape;
    private float lookMultiplier = 250f;
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

        // moveDelta.x -= Input.GetKey(KeyCode.A) ? speedMult : 0f;
        // moveDelta.x += Input.GetKey(KeyCode.D) ? speedMult : 0f;
        // moveDelta.y += Input.GetKey(KeyCode.W) ? speedMult : 0f;
        // moveDelta.y -= Input.GetKey(KeyCode.S) ? speedMult : 0f;
        // if (moveDelta.x != 0f || moveDelta.y != 0f)
        // {
        //     moveDelta = moveDelta.normalized * speedMult;
        //     updateMovement(moveDelta);
        // }

        Vector2 leftStickVal = currentGamepad.leftStick.value;
        Debug.Log($"Left stick: {leftStickVal}");

        // lookDelta = Input.mousePositionDelta;
        // lookDelta.x = lookDelta.x / Screen.width;
        // lookDelta.y = lookDelta.y / Screen.height;
        // if (lookDelta.x != 0f || lookDelta.y != 0f)
        // {
        //     lookDelta *= lookMultiplier;
        //     updateLook(new Vector2(lookDelta.x, lookDelta.y));
        // }

        Vector2 rightStickVal = currentGamepad.rightStick.value;
        Debug.Log($"Right stick: {rightStickVal}");

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
