using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DeskInteractionController : MonoBehaviour
{
    public enum State { Idle, Held, Inspect }

    [Header("PS1 View (RenderTexture on UI RawImage)")]
    [SerializeField] private Camera renderCam;     // camera rendering the 512x288 RT
    [SerializeField] private RawImage rawImage;    // UI that displays the RT

    [Header("Scene")]
    [SerializeField] private Transform deskTop;            // Empty transform at desk surface height
    [SerializeField] private Transform holdAnchor;         // Empty child of camera (e.g., 0,0.05,0.2)
    [SerializeField] private LayerMask interactableMask;   // Only your interactable layer(s)

    [Header("Held (on desk)")]
    [SerializeField] private float deskFloatHeight = 0.08f;
    [SerializeField] private float followSmooth = 18f;
    [SerializeField] private float rotateSpeedDeg = 120f;  // Q/E

    [Header("Inspect (zoom)")]
    [SerializeField] private float inspectDistance = 0.5f;
    [SerializeField] private float inspectPosSmooth = 18f;
    [SerializeField] private float inspectRotSmooth = 14f;

    [Header("Quality of life")]
    [SerializeField] private float freezeAfterInspect = 0.15f;

    private State _state = State.Idle;
    private Holdable _hover;
    private Holdable _held;
    private float _deskY;
    private float _yaw;
    private float _pitch;

    private float _resumeFollowTime;
    private Vector3 _heldDeskPos;

    private Quaternion _heldBaseRot;
    private Quaternion _inspectRot;

    void Start()
    {
        if (renderCam == null) Debug.LogError("Assign RenderCam (camera that renders to the RenderTexture).");
        if (rawImage == null) Debug.LogError("Assign RawImage (shows the RenderTexture).");
        if (deskTop == null) Debug.LogError("Assign deskTop Transform at your desk surface.");
        if (holdAnchor == null) Debug.LogError("Assign holdAnchor (child of RenderCam).");

        _deskY = (deskTop != null) ? deskTop.position.y : 0f;
    }

    void Update()
    {
        var mouse = Mouse.current;
        var kb = Keyboard.current;
        if (mouse == null || renderCam == null || rawImage == null) return;

        // --- Hover detection (only while Idle)
        if (_state == State.Idle)
        {
            if (!TryGetRay(out Ray ray)) return;

            if (Physics.Raycast(ray, out RaycastHit hit, 10f, interactableMask))
            {
                var h = hit.collider.GetComponent<Holdable>();
                if (h != _hover)
                {
                    if (_hover) _hover.HoverExit();
                    if (h && !h.IsHeld) h.HoverEnter();
                    _hover = h;

                    CursorManager.Instance?.SetHover();
                }

                if (mouse.leftButton.wasPressedThisFrame && h != null)
                {
                    BeginHold(h);
                    return;
                }
            }
            else
            {
                if (_hover) _hover.HoverExit();
                _hover = null;
                CursorManager.Instance?.SetDefault();
            }
        }

        switch (_state)
        {
            case State.Held: HeldUpdate(mouse, kb); break;
            case State.Inspect: InspectUpdate(mouse, kb); break;
        }
    }

    bool TryGetRay(out Ray ray)
    {
        ray = default;

        Vector2 screenMouse = Mouse.current.position.ReadValue();
        RectTransform rt = rawImage.rectTransform;

        Canvas canvas = rawImage.canvas;
        Camera uiCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera
            : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenMouse, uiCam, out Vector2 local))
            return false;

        Rect rect = rt.rect;
        float u = (local.x - rect.xMin) / rect.width;
        float v = (local.y - rect.yMin) / rect.height;

        // Only interact when mouse is over the PS1 view
        if (u < 0f || u > 1f || v < 0f || v > 1f) return false;

        float px = u * renderCam.pixelWidth;
        float py = v * renderCam.pixelHeight;

        ray = renderCam.ScreenPointToRay(new Vector3(px, py, 0f));
        return true;
    }

    void BeginHold(Holdable h)
    {
        _held = h;
        _held.BeginHold();
        _state = State.Held;

        CursorManager.Instance?.SetHold();

        _heldDeskPos = new Vector3(_held.transform.position.x, _deskY, _held.transform.position.z);

        _heldBaseRot = _held.transform.rotation;
        _yaw = 0f;
        _pitch = 0f;
    }

    void HeldUpdate(Mouse mouse, Keyboard kb)
    {
        // Rotate Q/E
        float spin = 0f;
        if (kb != null)
        {
            if (kb.qKey.isPressed) spin -= 1f;
            if (kb.eKey.isPressed) spin += 1f;

            // Inspect toggle (R)
            if (kb.rKey.wasPressedThisFrame)
            {
                _inspectRot = _held.transform.rotation;
                _state = State.Inspect;
                CursorManager.Instance?.SetInspect();
                _resumeFollowTime = Time.time + freezeAfterInspect;
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
            CursorManager.Instance?.SetDefault();
            return;
        }

        // Move along desk plane using PS1-view ray
        if (TryGetRay(out Ray ray))
        {
            if (new Plane(Vector3.up, new Vector3(0f, _deskY, 0f)).Raycast(ray, out float t))
            {
                Vector3 hit = ray.GetPoint(t);
                _heldDeskPos = new Vector3(hit.x, _deskY, hit.z);
            }
        }

        Vector3 targetPos = new Vector3(_heldDeskPos.x, _deskY + deskFloatHeight, _heldDeskPos.z);
        _held.transform.position = Vector3.Lerp(_held.transform.position, targetPos, Time.deltaTime * followSmooth);

        Quaternion yawRot = Quaternion.AngleAxis(_yaw, Vector3.up);
        Quaternion targetRot = _heldBaseRot * yawRot;
        _held.transform.rotation = Quaternion.Slerp(_held.transform.rotation, targetRot, Time.deltaTime * followSmooth);
    }

    void InspectUpdate(Mouse mouse, Keyboard kb)
    {
        if (renderCam == null || _held == null) return;

        // Back to Held (R)
        if (kb != null && kb.rKey.wasPressedThisFrame)
        {
            _heldBaseRot = _held.transform.rotation;
            _yaw = 0f;
            _state = State.Held;
            CursorManager.Instance?.SetHold();
            _resumeFollowTime = Time.time + freezeAfterInspect;
            return;
        }

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

        Quaternion delta = Quaternion.Euler(pitchDelta, yawDelta, 0f);
        _inspectRot = delta * _inspectRot;

        Vector3 anchorOffset = (holdAnchor != null)
            ? renderCam.transform.TransformVector(holdAnchor.localPosition)
            : Vector3.zero;

        Vector3 targetPos = renderCam.transform.position
                            + renderCam.transform.forward * inspectDistance
                            + anchorOffset;

        _held.transform.position = Vector3.Lerp(_held.transform.position, targetPos, Time.deltaTime * inspectPosSmooth);

        _held.transform.rotation = Quaternion.Slerp(_held.transform.rotation, _inspectRot, Time.deltaTime * inspectRotSmooth);
    }
}
