using UnityEngine;
using UnityEngine.InputSystem;

public class DeskInteractionController : MonoBehaviour
{
    public enum State { Idle, Held, Inspect }

    [Header("Scene")]
    [SerializeField] private Transform deskTop;            // Empty transform at desk surface height
    [SerializeField] private Transform holdAnchor;         // Empty child of camera (e.g., 0,0.05,0.2)
    [SerializeField] private LayerMask interactableMask;   // Only your "Interactable" layer

    [Header("Held (on desk)")]
    [SerializeField] private float deskFloatHeight = 0.08f;  // meters above desk
    [SerializeField] private float followSmooth = 18f;
    [SerializeField] private float rotateSpeedDeg = 120f;   // Q/E

    [Header("Inspect (zoom)")]
    [SerializeField] private float inspectDistance = 0.5f;   // meters from camera
    [SerializeField] private float inspectPosSmooth = 18f;
    [SerializeField] private float inspectRotSmooth = 14f;

    [Header("Quality of life")]
    [SerializeField] private float freezeAfterInspect = 0.15f; // pause follow after exiting Inspect

    private State _state = State.Idle;
    private Camera _cam;
    private Holdable _hover;
    private Holdable _held;
    private float _deskY;
    private float _yaw;
    private float _pitch; // up/down rotation when inspecting

    private float _resumeFollowTime;
    private Vector3 _heldDeskPos;     // where it sits on the desk (XZ maintained)

    private Quaternion _heldBaseRot;     // rotation when we first picked it up
    private Quaternion _inspectBaseRot;  // rotation when we entered Inspect
    private Quaternion _inspectRot; // accumulated rotation while in Inspect


    void Awake()
    {
        _cam = Camera.main ?? GetComponent<Camera>();
        if (_cam == null) Debug.LogError("DeskInteractionController: no Camera found.");
    }

    void Start()
    {
        if (deskTop == null) Debug.LogError("Assign 'deskTop' Transform at your desk surface.");
        if (holdAnchor == null) Debug.LogError("Assign 'holdAnchor' (child of camera).");
        _deskY = (deskTop != null) ? deskTop.position.y : 0f;
    }

    void Update()
    {
        var mouse = Mouse.current;
        var kb = Keyboard.current;
        if (_cam == null || mouse == null) return;

        // --- Hover detection (only while not holding/inspecting)
        if (_state == State.Idle)
        {
            Ray ray = _cam.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 10f, interactableMask))
            {
                var h = hit.collider.GetComponent<Holdable>();
                if (h != _hover)
                {
                    if (_hover) _hover.HoverExit();
                    if (h && !h.IsHeld) h.HoverEnter();
                    _hover = h;
                }

                if (mouse.leftButton.wasPressedThisFrame && h != null)
                {
                    BeginHold(h);
                    return;
                }
            }
            else if (_hover)
            {
                _hover.HoverExit();
                _hover = null;
            }
        }

        // --- State machine
        switch (_state)
        {
            case State.Held:
                HeldUpdate(mouse, kb);
                break;

            case State.Inspect:
                InspectUpdate(mouse, kb);
                break;

            case State.Idle:
            default:
                break;
        }
    }

    // ---------- State handlers ----------
    void BeginHold(Holdable h)
    {
        _held = h;
        _held.BeginHold();
        _state = State.Held;

        _heldDeskPos = new Vector3(_held.transform.position.x, _deskY, _held.transform.position.z);

        _heldBaseRot = _held.transform.rotation;  // <— remember current rotation
        _yaw = 0f;                                 // we’ll add yaw on top of the base
        _pitch = 0f;
    }


    void HeldUpdate(Mouse mouse, Keyboard kb)
    {
        // Rotate (Q/E)
        float spin = 0f;
        if (kb != null)
        {
            if (kb.qKey.isPressed) spin -= 1f;
            if (kb.eKey.isPressed) spin += 1f;

            // Toggle Inspect (R)
            if (kb.rKey.wasPressedThisFrame)
            {
                _inspectRot = _held.transform.rotation; // seed accumulated rotation
                _state = State.Inspect;
                _resumeFollowTime = Time.time + freezeAfterInspect; // if you still use it later
                return;
            }






        }
        _yaw += spin * rotateSpeedDeg * Time.deltaTime;

        // Drop with RMB
        if (mouse.rightButton.wasPressedThisFrame)
        {
            _held.DropWithPhysics();
            _held = null;
            _state = State.Idle;
            return;
        }

        // Move along desk plane (XZ from mouse, Y fixed to desk top + float)
        Ray ray = _cam.ScreenPointToRay(mouse.position.ReadValue());
        float t;
        // Plane: y = _deskY
        if (new Plane(Vector3.up, new Vector3(0f, _deskY, 0f)).Raycast(ray, out t))
        {
            Vector3 hit = ray.GetPoint(t);
            _heldDeskPos = new Vector3(hit.x, _deskY, hit.z);
        }

        Vector3 targetPos = new Vector3(_heldDeskPos.x, _deskY + deskFloatHeight, _heldDeskPos.z);
        _held.transform.position = Vector3.Lerp(_held.transform.position, targetPos, Time.deltaTime * followSmooth);

        Quaternion yawRot = Quaternion.AngleAxis(_yaw, Vector3.up);
        Quaternion targetRot = _heldBaseRot * yawRot;              // rotate relative to the pickup rotation
        _held.transform.rotation = Quaternion.Slerp(_held.transform.rotation, targetRot, Time.deltaTime * followSmooth);

    }

    void InspectUpdate(Mouse mouse, Keyboard kb)
    {
        if (_cam == null || _held == null) return;

        // Toggle back to Held (R)
        if (kb != null && kb.rKey.wasPressedThisFrame)
        {
            // carry rotation back to Held so it keeps what player set
            _heldBaseRot = _held.transform.rotation;
            _yaw = 0f;                // held yaw starts fresh, relative to current
            _state = State.Held;
            _resumeFollowTime = Time.time + freezeAfterInspect;
            return;
        }

        // --- rotation while Inspect (unlimited: Q/E yaw, W/S pitch) ---
        float yawSpin = 0f, pitchSpin = 0f;
        if (kb != null)
        {
            if (kb.qKey.isPressed) yawSpin -= 1f;
            if (kb.eKey.isPressed) yawSpin += 1f;
            if (kb.wKey.isPressed) pitchSpin += 1f;
            if (kb.sKey.isPressed) pitchSpin -= 1f;
        }

        float yawDelta = yawSpin * rotateSpeedDeg * Time.deltaTime;
        float pitchDelta = pitchSpin * rotateSpeedDeg * Time.deltaTime;

        // Accumulate rotation (no clamps → full 360s)
        Quaternion delta = Quaternion.Euler(pitchDelta, yawDelta, 0f);
        _inspectRot = delta * _inspectRot;

        // --- position in front of camera (bring it closer) ---
        Vector3 anchorOffset = (holdAnchor != null)
            ? _cam.transform.TransformVector(holdAnchor.localPosition)
            : Vector3.zero;

        Vector3 targetPos = _cam.transform.position
                            + _cam.transform.forward * inspectDistance
                            + anchorOffset;

        _held.transform.position = Vector3.Lerp(
            _held.transform.position, targetPos, Time.deltaTime * inspectPosSmooth
        );

        // --- apply rotation ---
        _held.transform.rotation = Quaternion.Slerp(
            _held.transform.rotation, _inspectRot, Time.deltaTime * inspectRotSmooth
        );
    }


}

