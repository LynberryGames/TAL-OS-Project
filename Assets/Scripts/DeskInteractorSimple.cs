using UnityEngine;
using UnityEngine.InputSystem;

public class DeskInteractorSimple : MonoBehaviour
{
    public Camera cam;
    public LayerMask interactMask;

    public float maxDistance = 3f;
    public float floatHeight = 0.08f;

    public float inspectDistance = 0.5f;
    public float rotateSpeed = 120f;

    public DeskGameLoopController loop;

    Interactable hover;
    Interactable held;

    bool inspecting;
    Vector3 inspect;   // rotation values

    void Start()
    {
        if (cam == null)
            cam = Camera.main;
    }

    void Update()
    {
        if (Mouse.current == null)
            return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        // --------------------
        // NOT HOLDING ANYTHING
        // --------------------
        if (held == null)
        {
            hover = null;

            // Raycast to see what the mouse is pointing at
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                // FIRST: check if we hit a desk button
                DeskButtons button = hit.collider.GetComponentInParent<DeskButtons>();
                if (button != null)
                {
                    hover = null;

                    if (Mouse.current.leftButton.wasPressedThisFrame)
                    {
                        button.PressVisual();

                        if (button.ButtonType == DeskButtons.Type.Green)
                            loop.Accept();
                        else
                            loop.Reject();
                    }

                    return;
                }

                // If not a button, check for interactable object
                hover = hit.collider.GetComponentInParent<Interactable>();
            }

            // Click to grab object
            if (Mouse.current.leftButton.wasPressedThisFrame && hover != null)
            {
                held = hover;
                inspecting = false;

                // Reset rotation so it looks tidy
                held.Visual.rotation = held.DefaultRotation;
            }

            return;
        }

        // --------------------
        // HOLDING SOMETHING
        // --------------------

        // Toggle inspect with R
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            inspecting = !inspecting;

            if (inspecting)
                inspect = held.Visual.eulerAngles;
            else
                held.Visual.rotation = held.DefaultRotation;
        }

        // Drop with left click (only when not inspecting)
        if (!inspecting && Mouse.current.leftButton.wasPressedThisFrame)
        {
            held = null;
            return;
        }

        // --------------------
        // MOVE HELD OBJECT
        // --------------------
        if (!inspecting)
        {
            // Follow mouse on surface
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                held.Visual.position =
                    hit.point + Vector3.up * floatHeight;
            }
        }
        else
        {
            // Inspect mode: move in front of camera
            held.Visual.position =
                cam.transform.position + cam.transform.forward * inspectDistance;

            // Rotate with WASD
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed)
                    inspect.y -= rotateSpeed * Time.deltaTime;

                if (Keyboard.current.dKey.isPressed)
                    inspect.y += rotateSpeed * Time.deltaTime;

                if (Keyboard.current.wKey.isPressed)
                    inspect.x -= rotateSpeed * Time.deltaTime;

                if (Keyboard.current.sKey.isPressed)
                    inspect.x += rotateSpeed * Time.deltaTime;
            }

            held.Visual.eulerAngles = inspect;
        }
    }
}
