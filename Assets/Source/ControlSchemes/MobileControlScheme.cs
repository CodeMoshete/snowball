using System;
using UnityEngine;

public class MobileControlScheme : IControlScheme
{
    private const float MOVE_SPEED = 5f;
    private const float LOOK_MULT_MIN = 0.01f;
    private const float LOOK_MULT_MAX = 0.5f;
    private const float MOVE_THRESHOLD = 17000f;

    private Action<Vector2> updateLook;
    private Action<Vector2> updateMovement;
    private Action onThrow;
    private Action onJump;
    private Action onSpawnWall;
    private Action onNextWall;
    private Action onPrevWall;
    private Action onEscape;
    private float lookMultiplier = (LOOK_MULT_MAX + LOOK_MULT_MIN) / 2f;
    private Vector2 leftTouchStartPos;  // Initial touch position for movement
    private Vector2 rightTouchPos; // Initial touch position for looking

    private Vector2 leftTouchDelta;     // Delta for movement input
    private Vector2 rightTouchDelta;    // Delta for looking input

    public void Initialize(
        Action<Vector2> onUpdateLook, 
        Action<Vector2> onUpdateMovement,
        Action onThrow, 
        Action onJump, 
        Action onSpawnWall, 
        Action onNextWall,
        Action onPrevWall,
        Action onEscape)
    {
        updateLook = onUpdateLook;
        updateMovement = onUpdateMovement;
        this.onThrow = onThrow;
        this.onJump = onJump;
        this.onSpawnWall = onSpawnWall;
        this.onNextWall = onNextWall;
        this.onPrevWall = onPrevWall;
        this.onEscape = onEscape;

        Service.UpdateManager.AddObserver(OnUpdate);
        Service.EventManager.AddListener(EventId.OnThrowUIButtonPressed, OnThrowPressed);
        Service.EventManager.AddListener(EventId.OnJumpUIButtonPressed, OnJumpPressed);
        Service.EventManager.AddListener(EventId.OnWallUIButtonPressed, OnWallPressed);
        Service.EventManager.AddListener(EventId.OnNextWallUIButtonPressed, OnNextWallPressed);
        Service.EventManager.AddListener(EventId.OnPrevWallUIButtonPressed, OnPrevWallPressed);
        Service.EventManager.AddListener(EventId.OnMenuUIButtonPressed, OnMenuPressed);
        Service.EventManager.AddListener(EventId.OnLookSpeedUpdated, OnLookSpeedUpdated);
    }

    private bool OnThrowPressed(object cookie)
    {
        onThrow();
        return true;
    }

    private bool OnJumpPressed(object cookie)
    {
        onJump();
        return true;
    }

    private bool OnWallPressed(object cookie)
    {
        onSpawnWall();
        return true;
    }

    private bool OnNextWallPressed(object cookie)
    {
        onNextWall();
        return true;
    }

    private bool OnPrevWallPressed(object cookie)
    {
        onPrevWall();
        return true;
    }

    private bool OnMenuPressed(object cookie)
    {
        onEscape();
        return true;
    }

    private void OnUpdate(float dt)
    {
        leftTouchDelta = Vector2.zero;
        rightTouchDelta = Vector2.zero;

        foreach (var touch in Input.touches)
        {
            Debug.Log($"Touches {touch.position}");
            // Check if touch is on the left or right half of the screen
            bool isLeft = touch.position.x < Screen.width / 2;

            if (touch.phase == TouchPhase.Began)
            {
                // Save starting position for each touch
                if (isLeft)
                    leftTouchStartPos = touch.position;
                else
                    rightTouchPos = touch.position;
            }
            else if (touch.phase == TouchPhase.Moved || 
                     touch.phase == TouchPhase.Stationary)
            {
                // Calculate delta based on touch movement
                if (isLeft)
                {
                    leftTouchDelta = touch.position - leftTouchStartPos;
                    float dragPct = Vector2.SqrMagnitude(leftTouchDelta);
                    float pct = Mathf.Min(dragPct, MOVE_THRESHOLD) / MOVE_THRESHOLD;
                    float speedMult = Constants.MoveSpeed * dt;
                    Vector2 speedVector = leftTouchDelta.normalized * pct * speedMult;
                    updateMovement(speedVector);
                }
                else
                {
                    rightTouchDelta = touch.position - rightTouchPos;
                    rightTouchPos = touch.position;
                    Debug.Log($"Update look {rightTouchDelta}");
                    updateLook(rightTouchDelta * lookMultiplier);
                }
            }
            else if (touch.phase == TouchPhase.Ended || 
                     touch.phase == TouchPhase.Canceled)
            {
                // Reset deltas when touch ends
                if (isLeft)
                    leftTouchDelta = Vector2.zero;
                else
                    rightTouchDelta = Vector2.zero;
            }
        }
    }

    private bool OnLookSpeedUpdated(object cookie)
    {
        float lookSpeed = (float)cookie;
        lookMultiplier = Mathf.Lerp(LOOK_MULT_MIN, LOOK_MULT_MAX, lookSpeed);
        return false;
    }

    public void Dispose()
    {
        Service.UpdateManager.RemoveObserver(OnUpdate);
        Service.EventManager.RemoveListener(EventId.OnThrowUIButtonPressed, OnThrowPressed);
        Service.EventManager.RemoveListener(EventId.OnJumpUIButtonPressed, OnJumpPressed);
        Service.EventManager.RemoveListener(EventId.OnWallUIButtonPressed, OnWallPressed);
        Service.EventManager.RemoveListener(EventId.OnMenuUIButtonPressed, OnMenuPressed);
        Service.EventManager.RemoveListener(EventId.OnLookSpeedUpdated, OnLookSpeedUpdated);
    }
}
