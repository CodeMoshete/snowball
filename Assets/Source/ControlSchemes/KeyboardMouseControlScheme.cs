using System;
using UnityEngine;

public class KeyboardMouseControlScheme : IControlScheme
{
    // private const float MOVE_SPEED = 5f;
    private const float LOOK_MULT_MIN = 50f;
    private const float LOOK_MULT_MAX = 450f;

    private Action<Vector2> updateLook;
    private Action<Vector2> updateMovement;
    private Action onThrow;
    private Action onCycleAmmo;
    private Action onJump;
    private Action onSpawnWall;
    private Action onNextWall;
    private Action onPrevWall;
    private Action onEscape;
    private float lookMultiplier = 250f;

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

        moveDelta.x -= Input.GetKey(KeyCode.A) ? speedMult : 0f;
        moveDelta.x += Input.GetKey(KeyCode.D) ? speedMult : 0f;
        moveDelta.y += Input.GetKey(KeyCode.W) ? speedMult : 0f;
        moveDelta.y -= Input.GetKey(KeyCode.S) ? speedMult : 0f;
        if (moveDelta.x != 0f || moveDelta.y != 0f)
        {
            moveDelta = moveDelta.normalized * speedMult;
            updateMovement(moveDelta);
        }

        lookDelta = Input.mousePositionDelta;
        lookDelta.x = lookDelta.x / Screen.width;
        lookDelta.y = lookDelta.y / Screen.height;
        if (lookDelta.x != 0f || lookDelta.y != 0f)
        {
            lookDelta *= lookMultiplier;
            updateLook(new Vector2(lookDelta.x, lookDelta.y));
        }

        if (Input.GetMouseButtonDown(0))
        {
            onThrow();
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            onCycleAmmo();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            onJump();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            onSpawnWall();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            onPrevWall();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            onNextWall();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            onEscape();
        }
    }

    public void Dispose()
    {
        Service.UpdateManager.RemoveObserver(OnUpdate);
        Service.EventManager.RemoveListener(EventId.GameStateChanged, OnGameStateChanged);
        Service.EventManager.RemoveListener(EventId.OnLookSpeedUpdated, OnLookSpeedUpdated);
    }
}
