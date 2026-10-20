using UnityEngine;

public class EmergencyMotionBridge : MonoBehaviour
{
    [Header("References")]
    public UserPositionProvider userPositionProvider;

    [Header("Optional Debug Objects")]
    public Transform playerProxy;
    public Transform trackerAProxy;
    public Transform trackerBProxy;

    [Header("Runtime - Player")]
    public Vector3 playerGroundPosition;
    public bool hasTrackerData;

    [Header("Runtime - Wrist Trackers")]
    public Vector3 trackerAPosition;
    public Vector3 trackerBPosition;
    public Vector3 trackerCenterPosition;

    [Header("Hand Near Face Detection")]
    public float handNearFaceDistance = 0.35f;

    [Header("Wave Detection")]
    [Tooltip("相对头显的手腕速度阈值。走路误触发就调高。")]
    public float waveSpeedThreshold = 1.8f;

    [Tooltip("是否必须抬手后才允许判定挥手。建议开启。")]
    public bool requireHandRaisedForWave = true;

    [Tooltip("手腕至少要高于 Head Y - 该值，才算抬手。")]
    public float waveHandMaxBelowHead = 0.35f;

    [Tooltip("挥手触发后保持多久，避免一闪而过。")]
    public float waveHoldSeconds = 1.5f;

    [Header("Runtime Action State")]
    public bool handNearFace;
    public bool isWaving;

    [Header("Runtime Debug")]
    public float trackerARelativeSpeed;
    public float trackerBRelativeSpeed;
    public bool trackerAHandRaised;
    public bool trackerBHandRaised;

    private Vector3 lastTrackerARelativePosition;
    private Vector3 lastTrackerBRelativePosition;
    private float waveHoldUntilTime;
    private bool initialized;

    private void Update()
    {
        if (userPositionProvider == null)
        {
            return;
        }

        ReadPlayerPosition();
        ReadWristTrackers();
        DetectActions();
        UpdateDebugObjects();
    }

    private void ReadPlayerPosition()
    {
        playerGroundPosition = userPositionProvider.UserGroundPosition;
        hasTrackerData = userPositionProvider.HasValidTrackerData;
    }

    private void ReadWristTrackers()
    {
        trackerAPosition = userPositionProvider.TrackerAPosition;
        trackerBPosition = userPositionProvider.TrackerBPosition;
        trackerCenterPosition = userPositionProvider.TrackerCenterPosition;
    }

    private void DetectActions()
    {
        handNearFace = false;

        Transform head = userPositionProvider.xrCamera;

        if (head == null)
        {
            DetectWaveWithoutHeadReference();
            return;
        }

        Vector3 headPosition = head.position;

        float distanceA = Vector3.Distance(trackerAPosition, headPosition);
        float distanceB = Vector3.Distance(trackerBPosition, headPosition);

        handNearFace =
            distanceA <= handNearFaceDistance ||
            distanceB <= handNearFaceDistance;

        Vector3 relativeA = trackerAPosition - headPosition;
        Vector3 relativeB = trackerBPosition - headPosition;

        // 只看相对头显的水平移动，减少走路时整体位移导致的误判
        Vector3 relativeAHorizontal = new Vector3(relativeA.x, 0f, relativeA.z);
        Vector3 relativeBHorizontal = new Vector3(relativeB.x, 0f, relativeB.z);

        if (!initialized)
        {
            initialized = true;
            lastTrackerARelativePosition = relativeAHorizontal;
            lastTrackerBRelativePosition = relativeBHorizontal;
            trackerARelativeSpeed = 0f;
            trackerBRelativeSpeed = 0f;
            return;
        }

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);

        trackerARelativeSpeed =
            Vector3.Distance(relativeAHorizontal, lastTrackerARelativePosition) / dt;

        trackerBRelativeSpeed =
            Vector3.Distance(relativeBHorizontal, lastTrackerBRelativePosition) / dt;

        lastTrackerARelativePosition = relativeAHorizontal;
        lastTrackerBRelativePosition = relativeBHorizontal;

        float minWaveY = headPosition.y - waveHandMaxBelowHead;

        trackerAHandRaised = trackerAPosition.y >= minWaveY;
        trackerBHandRaised = trackerBPosition.y >= minWaveY;

        bool aFast = trackerARelativeSpeed >= waveSpeedThreshold;
        bool bFast = trackerBRelativeSpeed >= waveSpeedThreshold;

        bool waveCandidate;

        if (requireHandRaisedForWave)
        {
            waveCandidate =
                (trackerAHandRaised && aFast) ||
                (trackerBHandRaised && bFast);
        }
        else
        {
            waveCandidate = aFast || bFast;
        }

        if (waveCandidate)
        {
            waveHoldUntilTime = Time.time + waveHoldSeconds;
        }

        isWaving = Time.time <= waveHoldUntilTime;
    }

    private void DetectWaveWithoutHeadReference()
    {
        handNearFace = false;
        isWaving = false;
        trackerARelativeSpeed = 0f;
        trackerBRelativeSpeed = 0f;
        trackerAHandRaised = false;
        trackerBHandRaised = false;
    }

    private void UpdateDebugObjects()
    {
        if (playerProxy != null)
        {
            playerProxy.position = playerGroundPosition;
        }

        if (trackerAProxy != null)
        {
            trackerAProxy.position = trackerAPosition;
        }

        if (trackerBProxy != null)
        {
            trackerBProxy.position = trackerBPosition;
        }
    }

    public Vector3 GetPlayerPosition()
    {
        return playerGroundPosition;
    }

    public Vector3 GetTrackerAPosition()
    {
        return trackerAPosition;
    }

    public Vector3 GetTrackerBPosition()
    {
        return trackerBPosition;
    }

    public bool IsHandNearFace()
    {
        return handNearFace;
    }

    public bool IsWaving()
    {
        return isWaving;
    }
}