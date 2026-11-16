using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MotionTrackerController : MonoBehaviour
{
    public enum SweepMode
    {
        CircularSweep,
        VerticalFill
    }

    [System.Serializable]
    public class RadarTarget
    {
        [Tooltip("Label for identification in the Inspector (e.g., 'Xeno - Above')")]
        public string targetName;
        
        [Tooltip("Drag the Top, Middle, or Bottom ring/bar transform here")]
        public Transform ringTransform;
        
        [Tooltip("Check this to enable this enemy blip during your prototype run.")]
        public bool isEnabledForTest = true;
    }

    [Header("Mode Selection")]
    [Tooltip("Switch between CircularSweep and VerticalFill in the Inspector.")]
    public SweepMode sweepMode = SweepMode.VerticalFill;

    [Header("References")]
    public GameObject trackerUI;
    public List<RadarTarget> radarTargets = new List<RadarTarget>();
    public TextMeshProUGUI promptText;

    [Header("Circular Sweep Settings (Old Scene)")]
    public Transform scanLineTransform;
    public float angularSpeed = 120f;

    [Header("Vertical Fill Settings (New Scene)")]
    [Tooltip("Drag your UI Image (Type: Filled, Method: Vertical) here.")]
    public Image verticalFillImage;
    [Tooltip("Speed of the vertical fill sweep (cycles per second).")]
    public float verticalSweepSpeed = 0.5f;
    [Tooltip("Match this to your Image's Fill Origin (true = Bottom, false = Top).")]
    public bool sweepBottomToTop = true;

    [Header("Animation Settings")]
    public Transform loweredPosition;
    public Transform raisedPosition;
    public float transitionSpeed = 10f;

    [Header("Radar Properties")]
    public float blipDisplayDuration = 0.4f;

    private bool isTracking = false;
    private float currentSweepAngle = 0f;
    private float previousSweepAngle = 0f;
    private float currentFillAmount = 0f;
    private float previousFillAmount = 0f;

    private Coroutine transitionCoroutine;
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

            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
            transitionCoroutine = StartCoroutine(AnimateTrackerTransition(isTracking));

            if (promptText != null)
            {
                promptText.gameObject.SetActive(!isTracking);
            }
        }

        if (isTracking)
        {
            if (sweepMode == SweepMode.CircularSweep)
            {
                HandleCircularSweepAndDetection();
            }
            else
            {
                HandleVerticalFillAndDetection();
            }
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

        if (targetState)
        {
            trackerUI.SetActive(true);
            currentFillAmount = 0f;
            previousFillAmount = 0f;
            if (verticalFillImage != null) verticalFillImage.fillAmount = 0f;
        }

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

    void HandleCircularSweepAndDetection()
    {
        previousSweepAngle = currentSweepAngle;
        currentSweepAngle -= angularSpeed * Time.deltaTime;
        currentSweepAngle = Mathf.Repeat(currentSweepAngle, 360f);

        if (scanLineTransform != null)
        {
            scanLineTransform.localRotation = Quaternion.Euler(0, 0, currentSweepAngle);
        }

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

    void HandleVerticalFillAndDetection()
    {
        if (verticalFillImage == null) return;

        previousFillAmount = currentFillAmount;
        currentFillAmount = Mathf.Repeat(currentFillAmount + verticalSweepSpeed * Time.deltaTime, 1f);
        verticalFillImage.fillAmount = currentFillAmount;

        bool wrappedAround = currentFillAmount < previousFillAmount;
        RectTransform fillRect = verticalFillImage.rectTransform;

        foreach (RadarTarget target in radarTargets)
        {
            if (!target.isEnabledForTest || target.ringTransform == null) continue;

            // Automatically uses the verticalFillImage's own height
            Vector3 localPos = fillRect.InverseTransformPoint(target.ringTransform.position);
            float normalizedY = Mathf.InverseLerp(fillRect.rect.yMin, fillRect.rect.yMax, localPos.y);
            float targetProgress = sweepBottomToTop ? normalizedY : (1f - normalizedY);

            bool crossed = wrappedAround
                ? (targetProgress >= previousFillAmount || targetProgress <= currentFillAmount)
                : (targetProgress >= previousFillAmount && targetProgress <= currentFillAmount);

            if (crossed)
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