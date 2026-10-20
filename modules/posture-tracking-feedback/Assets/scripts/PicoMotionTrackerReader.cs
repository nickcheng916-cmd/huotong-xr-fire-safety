using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Unity.XR.PXR;

public class PicoMotionTrackerReader : MonoBehaviour
{
    [Header("References")]
    public UserPositionProvider userPositionProvider;

    [Header("Debug Markers")]
    public Transform trackerA;
    public Transform trackerB;
    public Transform userCenter;

    [Header("Options")]
    public bool requestMotionTrackingMode = true;
    public bool applyZFlip = true;
    public Transform trackingRoot;

    [Header("Manual ID Override")]
    public bool useManualTrackerIds = false;
    public long manualTrackerAId = 1;
    public long manualTrackerBId = 2;

    [Header("ID Scanner")]
    public bool scanCandidateIds = true;
    public int scanMinId = 0;
    public int scanMaxId = 8;

    [Header("Runtime")]
    public bool hasTwoTrackers;
    public int trackerCount;
    public string debugMessage;

    private readonly List<long> trackerIds = new List<long>();

    private int requestResult = -999;
    private int locationAResult = -999;
    private int locationBResult = -999;

    private bool locationAValid;
    private bool locationBValid;

    private int frame;
    private float nextRetryTime;

    private Vector3 lastPosA;
    private Vector3 lastPosB;
    private Vector3 lastCenter;

    private float deltaA;
    private float deltaB;
    private float deltaCenter;

    private readonly Dictionary<long, Vector3> scanLastPos = new Dictionary<long, Vector3>();
    private readonly Dictionary<long, Vector3> scanCurrentPos = new Dictionary<long, Vector3>();
    private readonly Dictionary<long, float> scanDelta = new Dictionary<long, float>();
    private readonly Dictionary<long, int> scanResult = new Dictionary<long, int>();
    private readonly Dictionary<long, bool> scanValid = new Dictionary<long, bool>();

    private void OnEnable()
    {
        PXR_MotionTracking.RequestMotionTrackerCompleteAction += OnRequestMotionTrackerComplete;
        PXR_MotionTracking.MotionTrackerConnectionAction += OnMotionTrackerConnectionChanged;
    }

    private void OnDisable()
    {
        PXR_MotionTracking.RequestMotionTrackerCompleteAction -= OnRequestMotionTrackerComplete;
        PXR_MotionTracking.MotionTrackerConnectionAction -= OnMotionTrackerConnectionChanged;
    }

    private IEnumerator Start()
    {
        if (requestMotionTrackingMode)
        {
            RequestTwoMotionTrackers();

            yield return new WaitForSeconds(1.5f);

            RequestTwoMotionTrackers();

            yield return new WaitForSeconds(2.0f);

            RequestTwoMotionTrackers();
        }
    }

    private void Update()
    {
        frame++;

        if (requestMotionTrackingMode && trackerIds.Count < 2 && Time.time >= nextRetryTime)
        {
            nextRetryTime = Time.time + 3.0f;
            RequestTwoMotionTrackers();
        }

        if (scanCandidateIds)
        {
            ScanCandidateTrackerIds();
        }

        ReadMotionTrackers();
    }

    public void RequestTwoMotionTrackers()
    {
        try
        {
            requestResult = PXR_MotionTracking.CheckMotionTrackerNumber(MotionTrackerNum.TWO);
        }
        catch
        {
            requestResult = -901;
        }

        Debug.Log("[MotionTracker] CheckMotionTrackerNumber TWO result = " + requestResult);
    }

    private void OnRequestMotionTrackerComplete(RequestMotionTrackerCompleteEventData data)
    {
        trackerIds.Clear();

        trackerCount = (int)data.trackerCount;

        for (int i = 0; i < trackerCount; i++)
        {
            long id = data.trackerIds[i];

            if (!trackerIds.Contains(id))
            {
                trackerIds.Add(id);
            }
        }

        Debug.Log("[MotionTracker] Request complete. Count = " + trackerCount + ", IDs = " + BuildTrackerIdList());
    }

    private void OnMotionTrackerConnectionChanged(long trackerId, int state)
    {
        if (state == 1)
        {
            if (!trackerIds.Contains(trackerId))
            {
                trackerIds.Add(trackerId);
            }
        }
        else
        {
            if (trackerIds.Contains(trackerId))
            {
                trackerIds.Remove(trackerId);
            }
        }

        trackerCount = trackerIds.Count;

        Debug.Log(
            "[MotionTracker] Connection changed. ID = " +
            trackerId +
            ", state = " +
            state +
            ", current count = " +
            trackerCount
        );
    }

    private void ReadMotionTrackers()
    {
        trackerCount = trackerIds.Count;

        long idA;
        long idB;

        if (!TryGetSelectedIds(out idA, out idB))
        {
            hasTwoTrackers = false;
            MarkInvalid();

            debugMessage =
                "Waiting for 2 Motion Trackers."
                + "\nFrame: " + frame
                + "\nRequest Result: " + requestResult
                + "\nTracker Count: " + trackerCount
                + "\nUse Manual IDs: " + useManualTrackerIds
                + "\nManual A ID: " + manualTrackerAId
                + "\nManual B ID: " + manualTrackerBId
                + "\n\nCallback Tracker IDs:"
                + "\n" + BuildTrackerIdList()
                + "\n\nID Scan:"
                + "\n" + BuildScanReport();

            return;
        }

        Vector3 posA;
        Quaternion rotA;
        bool okA = TryReadTrackerById(idA, out posA, out rotA, out locationAResult, out locationAValid);

        Vector3 posB;
        Quaternion rotB;
        bool okB = TryReadTrackerById(idB, out posB, out rotB, out locationBResult, out locationBValid);

        if (!okA || !okB)
        {
            hasTwoTrackers = false;
            MarkInvalid();

            debugMessage =
                "Tracker IDs received, but location read failed."
                + "\nFrame: " + frame
                + "\nRequest Result: " + requestResult
                + "\nTracker Count: " + trackerCount
                + "\nSelected A ID: " + idA
                + "\nSelected B ID: " + idB
                + "\nLocation A Result: " + locationAResult
                + "\nLocation A Valid: " + locationAValid
                + "\nLocation B Result: " + locationBResult
                + "\nLocation B Valid: " + locationBValid
                + "\n\nCallback Tracker IDs:"
                + "\n" + BuildTrackerIdList()
                + "\n\nID Scan:"
                + "\n" + BuildScanReport();

            return;
        }

        Vector3 center = (posA + posB) * 0.5f;

        UpdateDeltas(posA, posB, center);

        if (trackerA != null)
        {
            trackerA.position = posA;
            trackerA.rotation = rotA;
        }

        if (trackerB != null)
        {
            trackerB.position = posB;
            trackerB.rotation = rotB;
        }

        if (userCenter != null)
        {
            Vector3 markerCenter = center;
            markerCenter.y = 0.1f;
            userCenter.position = markerCenter;
        }

        if (userPositionProvider != null)
        {
            userPositionProvider.SetFootPositions(posA, posB);
        }

        hasTwoTrackers = true;

        debugMessage =
            "Motion Tracker realtime OK"
            + "\nFrame: " + frame
            + "\nRequest Result: " + requestResult
            + "\nTracker Count: " + trackerCount
            + "\nUse Manual IDs: " + useManualTrackerIds
            + "\nSelected A ID: " + idA
            + "\nSelected B ID: " + idB
            + "\nLocation A Result: " + locationAResult
            + "\nLocation A Valid: " + locationAValid
            + "\nLocation B Result: " + locationBResult
            + "\nLocation B Valid: " + locationBValid
            + "\n\nTracker A: " + posA.ToString("F4")
            + "\nTracker B: " + posB.ToString("F4")
            + "\nCenter: " + center.ToString("F4")
            + "\n\nDelta A: " + deltaA.ToString("F5")
            + "\nDelta B: " + deltaB.ToString("F5")
            + "\nDelta Center: " + deltaCenter.ToString("F5")
            + "\n\nCallback Tracker IDs:"
            + "\n" + BuildTrackerIdList()
            + "\n\nID Scan:"
            + "\n" + BuildScanReport();
    }

    private bool TryGetSelectedIds(out long idA, out long idB)
    {
        idA = -1;
        idB = -1;

        if (useManualTrackerIds)
        {
            idA = manualTrackerAId;
            idB = manualTrackerBId;
            return true;
        }

        if (trackerIds.Count >= 2)
        {
            idA = trackerIds[0];
            idB = trackerIds[1];
            return true;
        }

        return false;
    }

    private bool TryReadTrackerById(
        long trackerId,
        out Vector3 position,
        out Quaternion rotation,
        out int result,
        out bool isValidPose
    )
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        result = -999;
        isValidPose = false;

        MotionTrackerLocation location = new MotionTrackerLocation();

        try
        {
            result = PXR_MotionTracking.GetMotionTrackerLocation(
                trackerId,
                ref location,
                ref isValidPose
            );
        }
        catch
        {
            result = -911;
            return false;
        }

        if (result != 0 || !isValidPose)
        {
            return false;
        }

        Vector3 rawPos = new Vector3(
            location.pose.Position.x,
            location.pose.Position.y,
            location.pose.Position.z
        );

        Quaternion rawRot = new Quaternion(
            location.pose.Orientation.x,
            location.pose.Orientation.y,
            location.pose.Orientation.z,
            location.pose.Orientation.w
        );

        if (applyZFlip)
        {
            rawPos.z *= -1f;
        }

        if (trackingRoot != null)
        {
            position = trackingRoot.TransformPoint(rawPos);
            rotation = trackingRoot.rotation * rawRot;
        }
        else
        {
            position = rawPos;
            rotation = rawRot;
        }

        return true;
    }

    private void ScanCandidateTrackerIds()
    {
        int minId = Mathf.Max(0, scanMinId);
        int maxId = Mathf.Max(minId, scanMaxId);

        for (long id = minId; id <= maxId; id++)
        {
            Vector3 pos;
            Quaternion rot;
            int result;
            bool valid;

            bool ok = TryReadTrackerById(id, out pos, out rot, out result, out valid);

            scanResult[id] = result;
            scanValid[id] = ok && valid;
            scanCurrentPos[id] = pos;

            Vector3 last;
            if (scanLastPos.TryGetValue(id, out last))
            {
                scanDelta[id] = Vector3.Distance(pos, last);
            }
            else
            {
                scanDelta[id] = 0f;
            }

            scanLastPos[id] = pos;
        }
    }

    private string BuildScanReport()
    {
        if (!scanCandidateIds)
        {
            return "ID scan disabled.";
        }

        StringBuilder sb = new StringBuilder();

        int minId = Mathf.Max(0, scanMinId);
        int maxId = Mathf.Max(minId, scanMaxId);

        for (long id = minId; id <= maxId; id++)
        {
            int r = scanResult.ContainsKey(id) ? scanResult[id] : -999;
            bool v = scanValid.ContainsKey(id) && scanValid[id];
            Vector3 p = scanCurrentPos.ContainsKey(id) ? scanCurrentPos[id] : Vector3.zero;
            float d = scanDelta.ContainsKey(id) ? scanDelta[id] : 0f;

            sb.Append("ID ");
            sb.Append(id);
            sb.Append(" | R:");
            sb.Append(r);
            sb.Append(" | V:");
            sb.Append(v);
            sb.Append(" | D:");
            sb.Append(d.ToString("F5"));
            sb.Append(" | P:");
            sb.AppendLine(p.ToString("F3"));
        }

        return sb.ToString();
    }

    private string BuildTrackerIdList()
    {
        if (trackerIds.Count == 0)
        {
            return "No tracker id received yet.";
        }

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < trackerIds.Count; i++)
        {
            sb.AppendLine("[" + i + "] " + trackerIds[i]);
        }

        return sb.ToString();
    }

    private void UpdateDeltas(Vector3 posA, Vector3 posB, Vector3 center)
    {
        deltaA = Vector3.Distance(posA, lastPosA);
        deltaB = Vector3.Distance(posB, lastPosB);
        deltaCenter = Vector3.Distance(center, lastCenter);

        lastPosA = posA;
        lastPosB = posB;
        lastCenter = center;
    }

    private void MarkInvalid()
    {
        if (userPositionProvider != null)
        {
            userPositionProvider.MarkTrackerDataInvalid();
        }
    }
}