using UnityEngine;
using UnityEngine.InputSystem;

public class HandMouseControl : MonoBehaviour
{
    [Header("Control")]
    public bool requireRMBReleased = true;   // hand only moves when NOT holding RMB
    public float sensitivity = 0.0025f;      // how much mouse moves the hand
    public float smooth = 18f;               // higher = snappier

    [Header("Limits (local space)")]
    public Vector3 minLocalPos = new Vector3(-0.45f, -0.45f, 0.45f);
    public Vector3 maxLocalPos = new Vector3(0.45f, 0.25f, 0.95f);

    Vector3 targetLocalPos;

    void Start()
    {
        targetLocalPos = transform.localPosition;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        bool rmbHeld = Mouse.current.rightButton.isPressed;
        if (requireRMBReleased && rmbHeld)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue();

        // Screen-like controls: mouse X moves hand right, mouse Y moves hand up
        targetLocalPos.x += delta.x * sensitivity;
        targetLocalPos.y += delta.y * sensitivity;

        // Clamp so it stays in a “hand box”
        targetLocalPos.x = Mathf.Clamp(targetLocalPos.x, minLocalPos.x, maxLocalPos.x);
        targetLocalPos.y = Mathf.Clamp(targetLocalPos.y, minLocalPos.y, maxLocalPos.y);
        targetLocalPos.z = Mathf.Clamp(targetLocalPos.z, minLocalPos.z, maxLocalPos.z);

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, 1f - Mathf.Exp(-smooth * Time.deltaTime));
    }
}
