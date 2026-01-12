using UnityEngine;
using UnityEngine.InputSystem;

public class StaticLookCamera : MonoBehaviour
{
    public float sensitivity = 0.08f;

    public float minPitch = -30f;
    public float maxPitch = 30f;
    public float yawLimit = 60f;

    float pitch;
    float yaw;
    float yawCenter;

    void Start()
    {
        Vector3 startRot = transform.eulerAngles;

        yawCenter = NormalizeAngle(startRot.y);
        yaw = yawCenter;
        pitch = NormalizeAngle(startRot.x);

        // Start unlocked (hand mode)
        Cursor.lockState = CursorLockMode.None;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        bool looking = Mouse.current.rightButton.isPressed;

        Cursor.lockState = looking ? CursorLockMode.Locked : CursorLockMode.None;

        if (!looking) return;

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
