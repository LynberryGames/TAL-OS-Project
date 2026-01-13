using UnityEngine;
using UnityEngine.InputSystem;

public class DeskInteractor : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public Transform deskTop;               // empty transform placed at desk height
    public LayerMask interactableMask;

    [Header("Ray")]
    public float maxDistance = 3f;

    [Header("Hold on desk")]
    public float floatHeight = 0.08f;
    public float followSpeed = 20f;

    [Header("Rotation")]
    public float rotateSpeed = 120f;        // degrees/sec when holding Q/E

    [Header("Inspect")]
    public float zoomStep = 0.05f;          // fallback if Interactable.zoomStep is 0

    // Internal state
    Interactable hover;
    Interactable held;
    bool inspecting = false;

    Rigidbody heldRb;
    bool heldUsedGravity;


    float deskY;

    // Holding rotation
    float heldYaw = 0f;
    Quaternion heldStartRot;

    // Inspect rotation + distance
    Quaternion inspectRot;
    float inspectDistance;
    float minInspectDistance;
    float maxInspectDistance;

    void Start()
    {
        if (cam == null) cam = Camera.main;
        deskY = deskTop != null ? deskTop.position.y : 0f;
    }

    void Update()
    {
        if (cam == null || Mouse.current == null) return;

        // If not holding anything, just hover
        if (held == null)
        {
            UpdateHover();

            // Click to grab
            if (Mouse.current.leftButton.wasPressedThisFrame && hover != null)
                Grab(hover);

            return;
        }

        // Toggle inspect with R
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (!inspecting) EnterInspect();
            else ExitInspect();
        }

        // Update behaviour based on mode
        if (!inspecting)
            UpdateHoldOnDesk();
        else
            UpdateInspect();

        // If holding and NOT inspecting: Left click drops
        if (held != null && !inspecting && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Drop();
            return;
        }

    }

    // ---------------- HOVER ----------------
    void UpdateHover()
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableMask))
        {
            hover = hit.collider.GetComponentInParent<Interactable>();

            if (CursorManager.Instance != null) CursorManager.Instance.SetHover();
        }
        else
        {
            hover = null;

            if (CursorManager.Instance != null) CursorManager.Instance.SetDefault();
        }
    }

    // ---------------- GRAB / DROP ----------------
    void Grab(Interactable it)
    {
        held = it;
        hover = null;

        heldStartRot = held.Visual.rotation;
        heldYaw = 0f;

        // --- PHYSICS OFF WHILE HELD ---
        heldRb = held.GetComponentInParent<Rigidbody>();
        if (heldRb != null)
        {
            heldUsedGravity = heldRb.useGravity;
            heldRb.useGravity = false;
            heldRb.isKinematic = true;
            heldRb.linearVelocity = Vector3.zero;
            heldRb.angularVelocity = Vector3.zero;
        }

        if (CursorManager.Instance != null)
            CursorManager.Instance.SetGrab();
    }



    void Drop()
    {
        // --- PHYSICS BACK ON ---
        if (heldRb != null)
        {
            heldRb.isKinematic = false;
            heldRb.useGravity = heldUsedGravity;
            heldRb = null;
        }

        held = null;
        inspecting = false;

        if (CursorManager.Instance != null)
            CursorManager.Instance.SetDefault();
    }


    // ---------------- HOLD ON DESK ----------------
    void UpdateHoldOnDesk()
    {
        // 1) Move along desk plane where the mouse ray hits
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane deskPlane = new Plane(Vector3.up, new Vector3(0f, deskY, 0f));

        if (deskPlane.Raycast(ray, out float t))
        {
            Vector3 hit = ray.GetPoint(t);
            Vector3 targetPos = new Vector3(hit.x, deskY + floatHeight, hit.z);

            held.Visual.position = Vector3.Lerp(held.Visual.position, targetPos, Time.deltaTime * followSpeed);
        }

        // 2) Rotate with Q/E
        float spin = 0f;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed) spin -= 1f;
            if (kb.dKey.isPressed) spin += 1f;
        }

        heldYaw += spin * rotateSpeed * Time.deltaTime;

        Quaternion yawRot = Quaternion.AngleAxis(heldYaw, Vector3.up);
        Quaternion targetRot = heldStartRot * yawRot;

        held.Visual.rotation = Quaternion.Slerp(held.Visual.rotation, targetRot, Time.deltaTime * followSpeed);
    }

    // ---------------- INSPECT ----------------
    void EnterInspect()
    {
        inspecting = true;

        // Use your Interactable size-based distances
        float size = Mathf.Max(0.0001f, held.GetApproxSize());

        minInspectDistance = held.minZoomK * size;
        maxInspectDistance = held.maxZoomK * size;

        inspectDistance = Mathf.Clamp(held.defaultDistanceK * size, minInspectDistance, maxInspectDistance);

        inspectRot = held.Visual.rotation;
    }

    void ExitInspect()
    {
        inspecting = false;

        // Reset �desk rotation base� so you don't snap weirdly
        heldStartRot = held.Visual.rotation;
        heldYaw = 0f;
    }

    void UpdateInspect()
    {
        // 1) Zoom with mouse wheel
        float wheel = Mouse.current.scroll.ReadValue().y;
        if (wheel != 0f)
        {
            float step = held.zoomStep > 0f ? held.zoomStep : zoomStep;
            inspectDistance -= Mathf.Sign(wheel) * step;
            inspectDistance = Mathf.Clamp(inspectDistance, minInspectDistance, maxInspectDistance);
        }

        // 2) Rotate with Q/E and W/S
        float yawSpin = 0f;
        float pitchSpin = 0f;

        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed) yawSpin -= 1f;
            if (kb.dKey.isPressed) yawSpin += 1f;

            if (kb.wKey.isPressed) pitchSpin += 1f;
            if (kb.sKey.isPressed) pitchSpin -= 1f;
        }

        // Apply small rotation increments
        Quaternion yaw = Quaternion.AngleAxis(yawSpin * rotateSpeed * Time.deltaTime, Vector3.up);
        Quaternion pitch = Quaternion.AngleAxis(pitchSpin * rotateSpeed * Time.deltaTime, Vector3.right);

        inspectRot = yaw * pitch * inspectRot;

        // 3) Move object in front of camera
        Vector3 targetPos = cam.transform.position + cam.transform.forward * inspectDistance;

        held.Visual.position = Vector3.Lerp(held.Visual.position, targetPos, Time.deltaTime * followSpeed);
        held.Visual.rotation = Quaternion.Slerp(held.Visual.rotation, inspectRot, Time.deltaTime * followSpeed);
    }
}
