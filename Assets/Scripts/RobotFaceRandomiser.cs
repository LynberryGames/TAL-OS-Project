using UnityEngine;

public class RobotFaceRandomiser : MonoBehaviour
{
    [Header("What to change")]
    [SerializeField] private Renderer faceRenderer;

    [Header("Possible faces")]
    [SerializeField] private Texture2D[] faces;

    private MaterialPropertyBlock block;

    public void RandomiseFace()
    {
        if (faceRenderer == null) return;
        if (faces == null || faces.Length == 0) return;

        int i = Random.Range(0, faces.Length);

        if (block == null) block = new MaterialPropertyBlock();

        faceRenderer.GetPropertyBlock(block);

        // URP uses _BaseMap. _MainTex is a fallback for other shaders.
        block.SetTexture("_BaseMap", faces[i]);
        block.SetTexture("_MainTex", faces[i]);

        faceRenderer.SetPropertyBlock(block);
    }

    public int FaceCount
    {
        get { return (faces == null) ? 0 : faces.Length; }
    }

    public void SetFaceIndex(int index)
    {
        if (faceRenderer == null) return;
        if (faces == null || faces.Length == 0) return;

        index = Mathf.Clamp(index, 0, faces.Length - 1);

        if (block == null) block = new MaterialPropertyBlock();

        faceRenderer.GetPropertyBlock(block);
        block.SetTexture("_BaseMap", faces[index]);
        block.SetTexture("_MainTex", faces[index]);
        faceRenderer.SetPropertyBlock(block);
    }



}
