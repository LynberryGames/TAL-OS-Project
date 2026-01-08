using UnityEngine;
using UnityEngine.InputSystem;

public class StaticLookCamera : MonoBehaviour
{
    public float sensitivity = 0.08f;

    // Vertical limits
    public float minPitch = -30f;
    public float maxPitch = 30f;

    // How far left/right from the starting direction
    public float yawLimit = 60f;

    float pitch;
    float yaw;
    float yawCenter;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Vector3 startRot = transform.eulerAngles;

        yawCenter = NormalizeAngle(startRot.y);
        yaw = yawCenter;
        pitch = NormalizeAngle(startRot.x);
    }

    void Update()
    {
        if (Mouse.current == null)
            return;

        // ONLY look when Right Mouse Button is held
        if (!Mouse.current.rightButton.isPressed)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue();

        yaw += delta.x * sensitivity;
        pitch -= delta.y * sensitivity;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        yaw = Mathf.Clamp(yaw, yawCenter - yawLimit, yawCenter + yawLimit);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }


    float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}
