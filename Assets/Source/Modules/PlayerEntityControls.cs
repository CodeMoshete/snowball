using System.Collections.Generic;
using UnityEngine;
using Utils;

public class PlayerEntityControls
{
    private const float MIN_SLOWED_SPEED = 0.5f;
    private string FEET_OBJECT_NAME = "FeetCollision";
    private const string CAMERA_NAME = "Main Camera";
    private string CAMERA_ORIGIN = "CameraOrigin";
    private string CAMERA_CENTERPOINT = "CameraCenterpoint";
    private readonly LayerMask CAMERA_COLLISION_LAYERS = LayerMask.GetMask("Default", "Floor", "Buildable");
    private const float MAX_PITCH = 45f;
    private const float JUMP_FORCE = 250f;
    private float fullPitchRange = 2f * MAX_PITCH;

    public float CameraPitchPct
    {
        get
        {
            float pitch = cameraTransform.eulerAngles.x;
            if (pitch <= MAX_PITCH)
            {
                pitch = (MAX_PITCH - pitch) / fullPitchRange;
            }
            else
            {
                pitch = 1f - ((pitch - (360f - MAX_PITCH)) / fullPitchRange);
            }
            return pitch;
        }
    }

    private PlayerEntity player;
    private Transform cameraOrigin;
    private Transform cameraCenterpoint;
    private Transform cameraTransform;
    private float armatureMagnitude;
    private IControlScheme currentControlScheme;
    private bool isGrounded;
    
    // A persistent list of all objects colliding with the feet collider.
    private List<GameObject> feetColliders = new List<GameObject>();
    private bool isLookInverted;

    public PlayerEntityControls(PlayerEntity player)
    {
        this.player = player;
        cameraCenterpoint = UnityUtils.FindGameObject(player.gameObject, CAMERA_CENTERPOINT).transform;
        cameraOrigin = UnityUtils.FindGameObject(player.gameObject, CAMERA_ORIGIN).transform;
        cameraTransform = GameObject.Find(CAMERA_NAME).transform;

        // This value does not change, so we cache it.
        armatureMagnitude = (cameraOrigin.position - cameraCenterpoint.position).magnitude;
    }

    public void Initialize(IControlScheme controlScheme)
    {
        currentControlScheme = controlScheme;
        currentControlScheme.Initialize(UpdateLook, UpdateMovement, OnThrow, OnCycleSnowballAmmo, OnJump, OnSpawnWall, OnNextWall, OnPrevWall, OnEscape);
        CollisionEventDispatcher feet = UnityUtils.FindGameObject(player.gameObject, FEET_OBJECT_NAME).GetComponent<CollisionEventDispatcher>();
        feet.gameObject.SetActive(true);
        feet.AddListenerCollisionStart(OnFeetCollisionStart);
        feet.AddListenerCollisionEnd(OnFeetCollisionEnd);
        Service.EventManager.AddListener(EventId.OnLookInvertToggled, OnLookInverted);
        Service.UpdateManager.AddObserver(OnUpdate);
    }

    private void OnUpdate(float dt)
    {
        UpdateCameraCollision();
    }

    private bool OnLookInverted(object cookie)
    {
        isLookInverted = (bool)cookie;
        return false;
    }

    private void OnFeetCollisionStart(GameObject collidedObject)
    {
        isGrounded = true;
        feetColliders.Add(collidedObject);
        // Debug.Log($"Collision start: {feetColliders.Count}");
    }

    private void OnFeetCollisionEnd(GameObject collidedObject)
    {
        feetColliders.Remove(collidedObject);
        isGrounded = feetColliders.Count > 0;
        // Debug.Log($"Collision start: {feetColliders.Count}");
    }

    private void UpdateLook(Vector2 value)
    {
        if (player.IsControlDisabled)
            return;

        value.y = isLookInverted ? -value.y : value.y;
        player.transform.Rotate(new Vector3(0f, value.x, 0f));
        Vector3 armatureRotation = cameraTransform.localEulerAngles;
        float minPitch = 360f - MAX_PITCH;
        armatureRotation.x -= value.y;
        armatureRotation.x = (armatureRotation.x > MAX_PITCH && armatureRotation.x < 180f) ? MAX_PITCH : armatureRotation.x;
        armatureRotation.x = (armatureRotation.x < minPitch && armatureRotation.x > 180f) ? minPitch : armatureRotation.x;
        cameraTransform.localEulerAngles = armatureRotation;
    }

    private void UpdateCameraCollision()
    {
        Vector3 direction = cameraOrigin.position - cameraCenterpoint.position;

        if (Physics.Raycast(
            cameraCenterpoint.position, 
            direction.normalized, 
            out RaycastHit hit, 
            armatureMagnitude, CAMERA_COLLISION_LAYERS))
        {
            float pct = hit.distance / armatureMagnitude;
            pct = Mathf.Max(pct - 0.1f, 0f);
            cameraTransform.localPosition =
                Vector3.Lerp(cameraCenterpoint.localPosition, cameraOrigin.localPosition, pct);
        }
        else
        {
            cameraTransform.localPosition = cameraOrigin.localPosition;
        }
    }

    private void UpdateMovement(Vector2 value)
    {
        if (player.IsControlDisabled)
            return;

        // Debug.Log($"Player movement: {value}");

        Vector3 newPos = player.transform.position;
        float healthMovementModifier = Mathf.Lerp(MIN_SLOWED_SPEED, 1f, player.Health / Constants.MAX_HEALTH);
        float xComponent = value.x * healthMovementModifier;
        float yComponent = value.y * healthMovementModifier;
        newPos += xComponent * player.transform.right;
        newPos += yComponent * player.transform.forward;
        player.transform.position = newPos;
    }

    private void OnThrow()
    {
        if (player.IsControlDisabled)
            return;

        player.OnThrowPressed();

        if (player.PlayerAnimator != null)
        {
            player.PlayerAnimator.Throw();
        }
    }

    private void OnJump()
    {
        if (!isGrounded || player.IsControlDisabled)
            return;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        rb.AddForce(new Vector3(0f, JUMP_FORCE, 0f));

        if (player.PlayerAnimator != null)
        {
            player.PlayerAnimator.Jump();
        }
    }

    private void OnSpawnWall()
    {
        if (player.IsControlDisabled)
            return;

        player.OnPlaceWallPressed();
    }

    private void OnNextWall()
    {
        player.OnNextWallPressed();
    }

    private void OnPrevWall()
    {
        player.OnPrevWallPressed();
    }

    private void OnCycleSnowballAmmo()
    {
        player.OnCycleSnowballAmmoPressed();
    }

    private void OnEscape()
    {
        player.OnEscapePressed();
    }

    public void Dispose()
    {
        Service.UpdateManager.RemoveObserver(OnUpdate);
        Service.EventManager.RemoveListener(EventId.OnLookInvertToggled, OnLookInverted);
        currentControlScheme.Dispose();
        player = null;
        currentControlScheme = null;
        feetColliders = null;
    }
}
