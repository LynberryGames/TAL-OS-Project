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
    public float rotateSpeed = 120f;        // degrees/sec when holding A/D/W/S

    [Header("Game Loop")]
    [SerializeField] private DeskGameLoopController loop;

    // Internal state
    private Interactable hover;
    private Interactable held;
    private bool inspecting;

    private Rigidbody heldRb;
    private bool heldUsedGravity;
    private RigidbodyConstraints heldOldConstraints;

    private Vector3 holdTargetPos;
    private bool hasHoldTarget;

    private float deskY;

    // Inspect rotation + distance
    private Quaternion inspectRot;
    private float inspectDistance;
    private float minInspectDistance;
    private float maxInspectDistance;

    void Start()
    {
        if (cam == null) cam = Camera.main;
        deskY = deskTop != null ? deskTop.position.y : 0f;
    }

    void Update()
    {
        if (cam == null || Mouse.current == null) return;

        if (held == null)
        {
            UpdateHover();

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Buttons get priority over grabbing.
                if (TryClickDecisionButton())
                    return;

                if (hover != null)
                    Grab(hover);
            }

            return;
        }

        // Toggle inspect with R
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (!inspecting) EnterInspect();
            else ExitInspect();
        }

        if (!inspecting) UpdateHoldOnDesk();
        else UpdateInspect();

        // While holding (not inspecting): left click drops
        if (!inspecting && Mouse.current.leftButton.wasPressedThisFrame)
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
            CursorManager.Instance?.SetHover();
        }
        else
        {
            hover = null;
            CursorManager.Instance?.SetDefault();
        }
    }

    // ---------------- GRAB / DROP ----------------
    void Grab(Interactable it)
    {
        held = it;
        hover = null;

        held.Visual.rotation = held.DefaultRotation;

        // --- PHYSICS OFF WHILE HELD ---
        heldRb = held.GetComponentInParent<Rigidbody>();
        if (heldRb != null)
        {
            heldUsedGravity = heldRb.useGravity;
            heldOldConstraints = heldRb.constraints;

            heldRb.useGravity = false;
            heldRb.isKinematic = false;
            heldRb.linearVelocity = Vector3.zero;
            heldRb.angularVelocity = Vector3.zero;

            heldRb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        CursorManager.Instance?.SetGrab();
    }

    void Drop()
    {
        // --- PHYSICS BACK ON ---
        if (heldRb != null)
        {
            heldRb.constraints = heldOldConstraints;
            heldRb.isKinematic = false;
            heldRb.useGravity = heldUsedGravity;
            heldRb = null;
        }

        held = null;
        inspecting = false;

        CursorManager.Instance?.SetDefault();
    }

    // ---------------- HOLD ON DESK ----------------
    void UpdateHoldOnDesk()
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane deskPlane = new Plane(Vector3.up, new Vector3(0f, deskY, 0f));

        if (deskPlane.Raycast(ray, out float t))
        {
            Vector3 hit = ray.GetPoint(t);
            holdTargetPos = new Vector3(hit.x, deskY + floatHeight, hit.z);
            hasHoldTarget = true;
        }
        else
        {
            hasHoldTarget = false;
        }
    }

    // ---------------- INSPECT ----------------
    void EnterInspect()
    {
        inspecting = true;

        float size = Mathf.Max(0.0001f, held.GetApproxSize());

        minInspectDistance = held.minZoomK * size;
        maxInspectDistance = held.maxZoomK * size;

        inspectDistance = Mathf.Clamp(held.defaultDistanceK * size, minInspectDistance, maxInspectDistance);
        inspectRot = held.Visual.rotation;
    }

    void ExitInspect()
    {
        inspecting = false;
        held.Visual.rotation = held.DefaultRotation;
    }

    void UpdateInspect()
    {
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

        Quaternion yaw = Quaternion.AngleAxis(yawSpin * rotateSpeed * Time.deltaTime, Vector3.up);
        Quaternion pitch = Quaternion.AngleAxis(pitchSpin * rotateSpeed * Time.deltaTime, Vector3.right);

        inspectRot = yaw * pitch * inspectRot;

        Vector3 targetPos = cam.transform.position + cam.transform.forward * inspectDistance;

        held.Visual.position = Vector3.Lerp(held.Visual.position, targetPos, Time.deltaTime * followSpeed);
        held.Visual.rotation = Quaternion.Slerp(held.Visual.rotation, inspectRot, Time.deltaTime * followSpeed);
    }

    void FixedUpdate()
    {
        if (heldRb == null) return;
        if (inspecting) return;
        if (!hasHoldTarget) return;

        Vector3 newPos = Vector3.Lerp(heldRb.position, holdTargetPos, Time.fixedDeltaTime * followSpeed);
        heldRb.MovePosition(newPos);
        heldRb.angularVelocity = Vector3.zero;
    }

    // ---------------- BUTTON CLICK ----------------
    bool TryClickDecisionButton()
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        // No mask: hit anything, then see if it's a button.
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            return false;

        var btn = hit.collider.GetComponentInParent<DeskButtons>();
        if (btn == null)
            return false;

        // Visual press
        btn.PressVisual();

        // Action
        if (btn.ButtonType == DeskButtons.Type.Green)
            loop?.Accept();
        else
            loop?.Reject();

        return true;
    }
}
