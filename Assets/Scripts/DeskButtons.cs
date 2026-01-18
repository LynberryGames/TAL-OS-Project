using UnityEngine;
using System.Collections;

public class DeskButtons : MonoBehaviour
{
    public enum Type { Green, Red }
    [SerializeField] private Type type;

    [Header("Press Visual")]
    [SerializeField] private float pressDepth = 0.01f;
    [SerializeField] private float pressTime = 0.08f;

    private Vector3 startLocalPos;
    private bool isPressed;

    public Type ButtonType => type;

    void Awake()
    {
        startLocalPos = transform.localPosition;
    }

    public void PressVisual()
    {
        if (isPressed) return;
        StartCoroutine(PressRoutine());
    }

    private IEnumerator PressRoutine()
    {
        isPressed = true;

        transform.localPosition = startLocalPos - Vector3.up * pressDepth;
        yield return new WaitForSeconds(pressTime);

        transform.localPosition = startLocalPos;
        isPressed = false;
    }
}
