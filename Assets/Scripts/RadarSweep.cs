using UnityEngine;

public class RadarSweep : MonoBehaviour
{
    [Tooltip("Negative speed rotates clockwise")]
    public float rotationSpeed = -120f; 

    void Update()
    {
        // Rotate the RectTransform continuously around the Z axis
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }
}