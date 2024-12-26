using System;
using UnityEngine;

public class KeyboardMouseControlScheme : IControlScheme
{
    private const float MOVE_SPEED = 5f;
    private const float LOOK_MULT = 250f;

    private Action<Vector2> updateLook;
    private Action<Vector2> updateMovement;
    private Action onThrow;
    private Action onJump;
    private Action onSpawnWall;

    public void Initialize(
        Action<Vector2> onUpdateLook, 
        Action<Vector2> onUpdateMovement, 
        Action onThrow, 
        Action onJump, 
        Action onSpawnWall)
    {
        updateLook = onUpdateLook;
        updateMovement = onUpdateMovement;
        this.onThrow = onThrow;
        this.onJump = onJump;
        this.onSpawnWall = onSpawnWall;

        Cursor.lockState = CursorLockMode.Locked;
        Service.UpdateManager.AddObserver(OnUpdate);
    }

    private void OnUpdate(float dt)
    {
        Vector2 lookDelta = Vector2.zero;
        Vector2 moveDelta = Vector2.zero;
        float speedMult = MOVE_SPEED * dt;

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
            lookDelta *= LOOK_MULT;
            updateLook(new Vector2(lookDelta.x, lookDelta.y));
        }

        if (Input.GetMouseButtonDown(0))
        {
            onThrow();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            onJump();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            onSpawnWall();
        }
    }
}
