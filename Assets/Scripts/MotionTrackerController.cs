using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MotionTrackerController : MonoBehaviour
{
    [System.Serializable]
    public class RadarTarget
    {
        [Tooltip("Label for identification in the Inspector (e.g., 'Xeno - Above')")]
        public string targetName;
        
        [Tooltip("Drag the Top, Middle, or Bottom ring transform here")]
        public Transform ringTransform;
        
        [Tooltip("Check this to enable this enemy blip during your prototype run.")]
        public bool isEnabledForTest = true;
    }

    [Header("References")]
    public GameObject trackerUI;
    public Transform scanLineTransform;
    public List<RadarTarget> radarTargets = new List<RadarTarget>();
    public TextMeshProUGUI promptText;

    [Header("Animation Settings")]
    public Transform loweredPosition;
    public Transform raisedPosition;
    public float transitionSpeed = 10f;

    [Header("Radar Properties")]
    public float blipDisplayDuration = 0.4f;

    private bool isTracking = false;
    private float currentSweepAngle = 0f;
    private float previousSweepAngle = 0f;
    private Dictionary<Transform, Coroutine> activeBlipCoroutines = new Dictionary<Transform, Coroutine>();

    void Start()
    {
        if (loweredPosition != null)
        {
            transform.localPosition = loweredPosition.localPosition;
            transform.localRotation = loweredPosition.localRotation;
        }

        if (trackerUI != null)
        {
            trackerUI.SetActive(false);
        }

        if (promptText != null)
        {
            promptText.text = "Hold [SPACE] to raise Motion Tracker";
            promptText.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        bool isHoldingSpace = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;

        if (isHoldingSpace != isTracking)
        {
            isTracking = isHoldingSpace;
            StopAllCoroutines();
            StartCoroutine(AnimateTrackerTransition(isTracking));

            if (promptText != null)
            {
                promptText.gameObject.SetActive(!isTracking);
            }
        }

        if (isTracking)
        {
            HandleRadarSweepAndDetection();
        }
    }

    IEnumerator AnimateTrackerTransition(bool targetState)
    {
        float elapsedTime = 0f;
        float duration = 1f / transitionSpeed;

        Vector3 startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;

        Vector3 endPos = targetState ? raisedPosition.localPosition : loweredPosition.localPosition;
        Quaternion endRot = targetState ? raisedPosition.localRotation : loweredPosition.localRotation;

        if (targetState) trackerUI.SetActive(true);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f);

            transform.localPosition = Vector3.Lerp(startPos, endPos, smoothT);
            transform.localRotation = Quaternion.Slerp(startRot, endRot, smoothT);

            yield return null;
        }

        transform.localPosition = endPos;
        transform.localRotation = endRot;

        if (!targetState) trackerUI.SetActive(false);
    }

    void HandleRadarSweepAndDetection()
    {
        previousSweepAngle = currentSweepAngle;
        currentSweepAngle -= 120f * Time.deltaTime; 
        currentSweepAngle = Mathf.Repeat(currentSweepAngle, 360f);

        if (scanLineTransform != null)
        {
            scanLineTransform.localRotation = Quaternion.Euler(0, 0, currentSweepAngle);
        }

        // Loop through configured targets and check if they are enabled in the Inspector
        foreach (RadarTarget target in radarTargets)
        {
            if (!target.isEnabledForTest || target.ringTransform == null) continue;

            float ringAngle = GetAngleFromCenter(target.ringTransform);

            if (CheckAngleCrossed(previousSweepAngle, currentSweepAngle, ringAngle))
            {
                TriggerBlipOnRing(target.ringTransform);
            }
        }
    }

    float GetAngleFromCenter(Transform target)
    {
        Vector3 direction = target.localPosition;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return Mathf.Repeat(angle - 90f, 360f);
    }

    bool CheckAngleCrossed(float prevAngle, float currAngle, float targetAngle)
    {
        float diff = Mathf.Repeat(prevAngle - targetAngle, 360f);
        float step = Mathf.Repeat(prevAngle - currAngle, 360f);
        return diff <= step && step < 360f;
    }

    void TriggerBlipOnRing(Transform ringObj)
    {
        Transform blipChild = ringObj.Find("BlipEnemy");
        if (blipChild == null) return;

        GameObject blipObject = blipChild.gameObject;

        if (activeBlipCoroutines.ContainsKey(ringObj) && activeBlipCoroutines[ringObj] != null)
        {
            StopCoroutine(activeBlipCoroutines[ringObj]);
        }

        activeBlipCoroutines[ringObj] = StartCoroutine(FlashBlipRoutine(blipObject));
    }

    IEnumerator FlashBlipRoutine(GameObject blip)
    {
        blip.SetActive(true);
        yield return new WaitForSeconds(blipDisplayDuration);
        blip.SetActive(false);
    }
}