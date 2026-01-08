using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ScrollTexture : MonoBehaviour
{
    public Vector2 scrollSpeed = new Vector2(0f, -0.2f);

    Renderer rend;
    Material mat;
    Vector2 offset;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        // material = unique instance (so you don't scroll every object using the same material)
        mat = rend.material;
        offset = mat.mainTextureOffset;
    }

    void Update()
    {
        offset += scrollSpeed * Time.deltaTime;
        offset.x = Mathf.Repeat(offset.x, 1f);
        offset.y = Mathf.Repeat(offset.y, 1f);
        mat.mainTextureOffset = offset;
    }
}
