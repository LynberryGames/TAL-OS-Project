using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [Header("Cursor Textures")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D hoverCursor;
    [SerializeField] private Texture2D holdCursor;      // used for holding/grab
    [SerializeField] private Texture2D inspectCursor;   // used for inspect (optional)

    [Header("Hotspots (pixels)")]
    [SerializeField] private Vector2 defaultHotspot = Vector2.zero;
    [SerializeField] private Vector2 hoverHotspot = Vector2.zero;
    [SerializeField] private Vector2 holdHotspot = Vector2.zero;
    [SerializeField] private Vector2 inspectHotspot = Vector2.zero;

    public static CursorManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Desk-style game: visible, unlocked cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetDefault();
    }

    public void SetDefault() => Apply(defaultCursor, defaultHotspot);

    public void SetHover() =>
        Apply(hoverCursor != null ? hoverCursor : defaultCursor, hoverHotspot);

    // "Hold" is your grab state
    public void SetHold() =>
        Apply(holdCursor != null ? holdCursor : (hoverCursor != null ? hoverCursor : defaultCursor), holdHotspot);

    // Alias so your interactor can call SetGrab()
    public void SetGrab() => SetHold();

    public void SetInspect() =>
        Apply(inspectCursor != null ? inspectCursor : (holdCursor != null ? holdCursor : defaultCursor), inspectHotspot);

    void Apply(Texture2D tex, Vector2 hotspot)
    {
        if (tex == null) return;
        Cursor.SetCursor(tex, hotspot, CursorMode.Auto);
    }
}
