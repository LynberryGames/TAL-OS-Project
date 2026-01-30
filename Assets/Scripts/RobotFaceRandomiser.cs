using UnityEngine;

public class RobotFaceRandomiser : MonoBehaviour
{
    [SerializeField] private Renderer faceRenderer;
    [SerializeField] private Texture2D[] faces;

    public void RandomiseFace()
    {
        if (faceRenderer == null)
        {
            Debug.LogWarning("RobotFaceRandomiser: faceRenderer is NULL", this);
            return;
        }

        if (faces == null || faces.Length == 0)
        {
            Debug.LogWarning("RobotFaceRandomiser: faces is empty", this);
            return;
        }

        int i = Random.Range(0, faces.Length);
        Texture2D tex = faces[i];

        // Make/ensure an instance material on THIS renderer
        Material m = faceRenderer.material;

        // Set both (covers URP + older shaders)
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);

        Debug.Log($"Randomised face to index {i} ({tex.name}) on {faceRenderer.name}", this);
    }
}
