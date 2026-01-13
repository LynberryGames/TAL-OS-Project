using UnityEngine;
using UnityEngine.InputSystem;

public class StaticLook : MonoBehaviour
{
    public float sensitivity = 0.08f;

    // Limits from your starting rotation
    public float yawLimit = 60f;
    public float minPitch = -30f;
    public float maxPitch = 30f;

    float startYaw;
    float startPitch;

    float yaw;
    float pitch;

    void Start()
    {
        Vector3 start = transform.eulerAngles;

        startYaw = NormalizeAngle(start.y);
        startPitch = NormalizeAngle(start.x);

        yaw = startYaw;
        pitch = startPitch;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        // Only rotate while holding Right Mouse Button
        if (!Mouse.current.rightButton.isPressed)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue();

        yaw += delta.x * sensitivity;
        pitch -= delta.y * sensitivity;

        yaw = Mathf.Clamp(yaw, startYaw - yawLimit, startYaw + yawLimit);
        pitch = Mathf.Clamp(pitch, startPitch + minPitch, startPitch + maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    float NormalizeAngle(float a)
    {
        while (a > 180f) a -= 360f;
        while (a < -180f) a += 360f;
        return a;
    }
}
