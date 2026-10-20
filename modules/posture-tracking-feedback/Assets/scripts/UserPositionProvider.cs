using UnityEngine;

public class UserPositionProvider : MonoBehaviour
{
    [Header("References")]
    [Tooltip("拖 XR Origin 里的 Main Camera。玩家地面位置只来自这个相机。")]
    public Transform xrCamera;

    [Header("Calibration")]
    public bool autoSetOriginOnFirstFrame = true;
    public bool useCalibratedPosition = true;
    public float responseScale = 1.0f;
    public float smoothTime = 0.0f;

    [Header("Runtime Debug - Player Position")]
    public Vector3 UserGroundPosition;
    public Vector3 RawUserGroundPosition;
    public Vector3 CalibratedUserGroundPosition;
    public Vector3 OriginOffset;
    public bool HasOrigin;

    [Header("Runtime Debug - Wrist Trackers")]
    public bool HasValidTrackerData;
    public Vector3 TrackerAPosition;
    public Vector3 TrackerBPosition;
    public Vector3 TrackerCenterPosition;

    private Vector3 smoothVelocity;

    private void Update()
    {
        UpdatePlayerPositionFromHeadset();
    }

    private void UpdatePlayerPositionFromHeadset()
    {
        if (xrCamera == null)
        {
            return;
        }

        Vector3 raw = xrCamera.position;
        raw.y = 0f;

        RawUserGroundPosition = raw;

        if (!HasOrigin && autoSetOriginOnFirstFrame)
        {
            SetCurrentPositionAsOrigin();
        }

        Vector3 calibrated = raw;

        if (useCalibratedPosition)
        {
            calibrated = raw - OriginOffset;
            calibrated.y = 0f;
        }

        CalibratedUserGroundPosition = calibrated;

        Vector3 finalPosition = calibrated * responseScale;
        finalPosition.y = 0f;

        if (smoothTime <= 0f)
        {
            UserGroundPosition = finalPosition;
        }
        else
        {
            UserGroundPosition = Vector3.SmoothDamp(
                UserGroundPosition,
                finalPosition,
                ref smoothVelocity,
                smoothTime
            );
        }
    }

    // 保留这个方法名，是为了兼容你当前 PicoMotionTrackerReader 里的调用。
    // 注意：这里不会再改变 UserGroundPosition。
    public void SetFootPositions(Vector3 trackerA, Vector3 trackerB)
    {
        SetTrackerPositions(trackerA, trackerB);
    }

    // 手腕 tracker 只记录手腕位置，不参与玩家地面位置计算。
    public void SetTrackerPositions(Vector3 trackerA, Vector3 trackerB)
    {
        TrackerAPosition = trackerA;
        TrackerBPosition = trackerB;
        TrackerCenterPosition = (trackerA + trackerB) * 0.5f;

        HasValidTrackerData = true;
    }

    public void MarkTrackerDataInvalid()
    {
        HasValidTrackerData = false;
    }

    public void SetCurrentPositionAsOrigin()
    {
        OriginOffset = RawUserGroundPosition;
        OriginOffset.y = 0f;

        HasOrigin = true;

        CalibratedUserGroundPosition = Vector3.zero;
        UserGroundPosition = Vector3.zero;
        smoothVelocity = Vector3.zero;

        Debug.Log("[UserPositionProvider] Origin set to " + OriginOffset.ToString("F4"));
    }

    public void ResetOrigin()
    {
        HasOrigin = false;
        OriginOffset = Vector3.zero;
        CalibratedUserGroundPosition = Vector3.zero;
        UserGroundPosition = Vector3.zero;
        smoothVelocity = Vector3.zero;

        Debug.Log("[UserPositionProvider] Origin reset.");
    }

    public Vector3 GetGamePosition()
    {
        return UserGroundPosition;
    }

    public Vector3 GetTrackerAPosition()
    {
        return TrackerAPosition;
    }

    public Vector3 GetTrackerBPosition()
    {
        return TrackerBPosition;
    }
}