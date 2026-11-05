using UnityEngine;
using UnityEngine.InputSystem;

public class TrackerToggle : MonoBehaviour
{
    public GameObject trackerUI;
    private bool isTracking = false;

    void Update()
    {
        // Using Unity 6 New Input System checks
        bool togglePressed = (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame) ||
                             (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);

        if (togglePressed)
        {
            isTracking = !isTracking;
            if (trackerUI != null)
            {
                trackerUI.SetActive(isTracking);
            }
        }
    }
}