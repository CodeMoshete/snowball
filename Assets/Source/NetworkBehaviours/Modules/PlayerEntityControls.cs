using UnityEngine;
using Utils;

public class PlayerEntityControls
{
    private const float MAX_PITCH = 45f;
    private const float JUMP_FORCE = 150f;
    private float fullPitchRange = 2f * MAX_PITCH;

    public float CameraPitchPct
    {
        get
        {
            float pitch = cameraArmature.eulerAngles.x;
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
    private Transform cameraArmature;
    private IControlScheme currentControlScheme;

    public PlayerEntityControls(PlayerEntity player)
    {
        this.player = player;
        cameraArmature = UnityUtils.FindGameObject(player.gameObject, "CameraOrigin").transform;
    }

    public void Initialize(IControlScheme controlScheme)
    {
        currentControlScheme = controlScheme;
        currentControlScheme.Initialize(UpdateLook, UpdateMovement, OnThrow, OnJump, OnSpawnWall, OnEscape);
    }

    private void UpdateLook(Vector2 value)
    {
        if (player.IsControlDisabled)
            return;

        player.transform.Rotate(new Vector3(0f, value.x, 0f));
        Vector3 armatureRotation = cameraArmature.localEulerAngles;
        float minPitch = 360f - MAX_PITCH;
        armatureRotation.x -= value.y;
        armatureRotation.x = (armatureRotation.x > MAX_PITCH && armatureRotation.x < 180f) ? MAX_PITCH : armatureRotation.x;
        armatureRotation.x = (armatureRotation.x < minPitch && armatureRotation.x > 180f) ? minPitch : armatureRotation.x;
        cameraArmature.localEulerAngles = armatureRotation;
    }

    private void UpdateMovement(Vector2 value)
    {
        if (player.IsControlDisabled)
            return;

        Vector3 newPos = player.transform.position;
        newPos += value.x * player.transform.right;
        newPos += value.y * player.transform.forward;
        player.transform.position = newPos;
    }

    private void OnThrow()
    {
        if (player.IsControlDisabled)
            return;

        player.OnThrowPressed();
    }

    private void OnJump()
    {
        if (player.IsControlDisabled)
            return;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        rb.AddForce(new Vector3(0f, JUMP_FORCE, 0f));
    }

    private void OnSpawnWall()
    {
        if (player.IsControlDisabled)
            return;

        player.OnPlaceWallPressed();
    }

    private void OnEscape()
    {
        player.OnEscapePressed();
    }

    public void Dispose()
    {
        currentControlScheme.Dispose();
        player = null;
        cameraArmature = null;
        currentControlScheme = null;
    }
}
