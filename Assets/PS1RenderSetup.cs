using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class PS1RenderSetup : MonoBehaviour
{
    [SerializeField] private RenderTexture renderTexture;

    private void Awake()
    {
        if (renderTexture != null)
            renderTexture.filterMode = FilterMode.Point;

        GetComponent<RawImage>().texture = renderTexture;
    }
}
