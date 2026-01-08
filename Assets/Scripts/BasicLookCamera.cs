using UnityEngine;

public class BasicLookCamera : MonoBehaviour
{
    public float sensitivity = 2f;
    float pitch = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        // Horizontal look
        transform.Rotate(0f, mouseX, 0f, Space.World);

        // Vertical look
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -80f, 80f);
        transform.localRotation = Quaternion.Euler(pitch, transform.localEulerAngles.y, 0f);
    }
}
