using UnityEngine;

/// Put this on any item you want to pick up/inspect.
public class Interactable : MonoBehaviour
{
    [Header("Optional: if your mesh is nested")]
    [Tooltip("If null, we'll rotate the GameObject this script is on.")]
    public Transform visualRoot;

    [Header("Inspect – feel")]
    [Tooltip("How fast the item rotates when you drag in Inspect.")]
    public float rotationSensitivity = 1.0f;

    [Tooltip("How fast scroll changes zoom distance.")]
    public float zoomStep = 0.5f;

    [Header("Inspect – zoom (computed from size on enter)")]
    [Tooltip("Multiplier for default distance = k * item size.")]
    public float defaultDistanceK = 2.0f;

    [Tooltip("Min zoom distance multiplier × item size.")]
    public float minZoomK = 0.5f;

    [Tooltip("Max zoom distance multiplier × item size.")]
    public float maxZoomK = 5.0f;

    /// The transform we actually rotate/position during Inspect.
    public Transform Visual => visualRoot != null ? visualRoot : transform;

    /// Helper to get a sensible size for zoom distances.
    public float GetApproxSize()
    {
        // Use the renderer bounds if available; otherwise fall back to 1
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return 1f;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        // Magnitude of extents is a decent “size” proxy.
        return b.extents.magnitude;
    }

    /// Helper to get the center for a natural spin pivot.
    public Vector3 GetBoundsCenter()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return transform.position;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        return b.center;
    }
}
