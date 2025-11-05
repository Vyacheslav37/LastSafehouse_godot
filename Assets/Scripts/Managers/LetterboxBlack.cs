using UnityEngine;

[ExecuteInEditMode]
public class LetterboxBlack : MonoBehaviour
{
    private void Awake()
    {
        FixCamera();
    }

    private void OnEnable()
    {
        FixCamera();
    }

    private void FixCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;

        cam.backgroundColor = Color.black;
        cam.clearFlags = CameraClearFlags.SolidColor;
    }
}