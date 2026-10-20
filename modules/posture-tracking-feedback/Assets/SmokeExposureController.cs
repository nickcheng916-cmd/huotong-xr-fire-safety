using UnityEngine;

public class SmokeExposureController : MonoBehaviour
{
    [Header("采样点")]
    public Transform headPoint;
    public Transform chestPoint;

    [Header("头部权重")]
    [Range(0f, 1f)] public float headWeight = 0.8f;

    [Header("平滑速度")]
    public float riseSpeed = 1.5f;
    public float fallSpeed = 1.0f;

    [Header("阈值")]
    [Range(0f, 1f)] public float crouchThreshold = 0.45f;
    [Range(0f, 1f)] public float crouchRunThreshold = 0.75f;

    [Header("Animator")]
    public Animator animator;
    public string crouchParam = "SmokeCrouch";
    public string runParam = "SmokeRun";
    public string densityParam = "SmokeDensity";

    public float CurrentDensity { get; private set; }
    public float HeadDensity { get; private set; }
    public float ChestDensity { get; private set; }

    public bool IsCrouching => CurrentDensity >= crouchThreshold;
    public bool IsCrouchRunning => CurrentDensity >= crouchRunThreshold;

    private void Reset()
    {
        if (Camera.main != null)
            headPoint = Camera.main.transform;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (headPoint == null && Camera.main != null)
            headPoint = Camera.main.transform;

        Vector3 headPos = headPoint != null ? headPoint.position : transform.position;
        Vector3 chestPos = chestPoint != null ? chestPoint.position : headPos + Vector3.down * 0.35f;

        float maxHead = 0f;
        float maxChest = 0f;

        for (int i = 0; i < SmokeZone.ActiveZones.Count; i++)
        {
            SmokeZone zone = SmokeZone.ActiveZones[i];
            if (zone == null) continue;

            float h = zone.SampleDensity(headPos);
            float c = zone.SampleDensity(chestPos);

            if (h > maxHead) maxHead = h;
            if (c > maxChest) maxChest = c;
        }

        HeadDensity = maxHead;
        ChestDensity = maxChest;

        float targetDensity = Mathf.Lerp(ChestDensity, HeadDensity, headWeight);

        float speed = targetDensity > CurrentDensity ? riseSpeed : fallSpeed;
        CurrentDensity = Mathf.MoveTowards(CurrentDensity, targetDensity, speed * Time.deltaTime);

        ApplyAnimator();
    }

    private void ApplyAnimator()
    {
        if (animator == null) return;

        bool crouch = CurrentDensity >= crouchThreshold;
        bool run = CurrentDensity >= crouchRunThreshold;

        animator.SetBool(crouchParam, crouch);
        animator.SetBool(runParam, run);
        animator.SetFloat(densityParam, CurrentDensity);
    }
}