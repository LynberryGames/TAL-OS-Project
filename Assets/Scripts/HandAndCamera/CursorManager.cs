using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [Header("Textures")]
    [SerializeField] private Texture2D defaultCursor; // assign PNG
    [SerializeField] private Texture2D hoverCursor;   // assign PNG
    [SerializeField] private Texture2D holdCursor;    // optional: reuse hover or a 2nd PNG
    [SerializeField] private Texture2D inspectCursor; // optional: reuse hold if you only have 2

    [Header("Hotspots (pixels from top-left)")]
    [SerializeField] private Vector2 defaultHotspot = Vector2.zero;
    [SerializeField] private Vector2 hoverHotspot = Vector2.zero;
    [SerializeField] private Vector2 holdHotspot = Vector2.zero;
    [SerializeField] private Vector2 inspectHotspot = Vector2.zero;

    public static CursorManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Keep cursor visible & unlocked for this style of game
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetDefault();
    }

    public void SetDefault() => Apply(defaultCursor, defaultHotspot);
    public void SetHover() => Apply(hoverCursor ? hoverCursor : defaultCursor, hoverHotspot);
    public void SetHold() => Apply(holdCursor ? holdCursor : hoverCursor, holdHotspot);
    public void SetInspect() => Apply(inspectCursor ? inspectCursor : holdCursor, inspectHotspot);

    private void Apply(Texture2D tex, Vector2 hotspot)
    {
        if (tex == null) return;
        // Note: hotspot is in pixels from top-left of the texture
        Cursor.SetCursor(tex, hotspot, CursorMode.Auto);
    }

    void LateUpdate()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

}
