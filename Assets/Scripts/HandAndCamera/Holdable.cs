using UnityEngine;

[DisallowMultipleComponent]
public class Holdable : MonoBehaviour
{
    [Header("Hover")]
    [SerializeField] private Color hoverColor = new Color(1f, 0.9f, 0.2f);

    [Header("Auto-find from children if empty")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Collider targetCollider;

    private Color _origColor;
    private bool _hasColor;
    private int _origLayer;

    public Rigidbody Rb { get; private set; }
    public bool IsHeld { get; private set; }

    void Awake()
    {
        // Find renderer/collider in children if not assigned
        if (!targetRenderer) targetRenderer = GetComponentInChildren<Renderer>(true);
        if (!targetCollider) targetCollider = GetComponentInChildren<Collider>(true);

        // Rigidbody should be on ROOT
        Rb = GetComponent<Rigidbody>();
        if (!Rb) Rb = gameObject.AddComponent<Rigidbody>();

        _origLayer = gameObject.layer;

        // Cache original colour if material supports it
        if (targetRenderer && targetRenderer.material.HasProperty("_Color"))
        {
            _origColor = targetRenderer.material.color;
            _hasColor = true;
        }
    }

    public void HoverEnter()
    {
        if (IsHeld) return;
        if (!targetRenderer) return;

        if (_hasColor)
            targetRenderer.material.color = hoverColor;
    }

    public void HoverExit()
    {
        if (!targetRenderer) return;

        if (_hasColor)
            targetRenderer.material.color = _origColor;
    }

    public void BeginHold()
    {
        IsHeld = true;

        // Freeze physics while held
        if (Rb)
        {
            Rb.linearVelocity = Vector3.zero;
            Rb.angularVelocity = Vector3.zero;
            Rb.isKinematic = true;
        }

        // Optional: put on a "Held" layer if you want
        // gameObject.layer = LayerMask.NameToLayer("Held");
    }

    public void DropWithPhysics()
    {
        IsHeld = false;

        if (Rb)
        {
            Rb.isKinematic = false;
        }

        // Restore layer if you changed it
        gameObject.layer = _origLayer;
    }
}
