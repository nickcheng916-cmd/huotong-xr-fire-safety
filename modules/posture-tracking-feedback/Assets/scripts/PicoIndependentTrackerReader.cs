using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;

public class PicoIndependentTrackerReader : MonoBehaviour
{
    [Header("References")]
    public UserPositionProvider userPositionProvider;

    [Header("Debug Markers")]
    public Transform trackerA;
    public Transform trackerB;
    public Transform userCenter;

    [Header("Mode")]
    public bool requestIndependentModeOnStart = true;
    public string preferredModeKeyword = "motion";

    [Header("Filter")]
    public bool ignoreHmd = true;
    public bool ignoreControllers = true;

    [Header("Runtime")]
    public bool hasTwoTrackers;
    public string debugMessage;

    private string modeMessage = "Mode request not started.";

    private readonly List<InputDevice> devices = new List<InputDevice>();
    private readonly List<TrackerPose> trackerPoses = new List<TrackerPose>();

    private struct TrackerPose
    {
        public InputDevice device;
        public Vector3 position;
        public Quaternion rotation;
    }

    void Start()
    {
        if (requestIndependentModeOnStart)
        {
            RequestIndependentMode();
        }
    }

    void Update()
    {
        ReadTrackers();
    }

    public void RequestIndependentMode()
    {
        Type motionType = FindTypeByName("PXR_MotionTracking");
        Type modeType = FindTypeByName("MotionTrackerMode");

        if (motionType == null || modeType == null)
        {
            modeMessage =
                "Mode request failed."
                + "\nCannot find PXR_MotionTracking or MotionTrackerMode."
                + "\nPICO SDK type not found in loaded assemblies.";
            Debug.LogWarning("[IndependentTracker] " + modeMessage);
            return;
        }

        MethodInfo method = motionType.GetMethod(
            "CheckMotionTrackerModeAndNumber",
            BindingFlags.Public | BindingFlags.Static
        );

        if (method == null)
        {
            modeMessage =
                "Mode request failed."
                + "\nCannot find CheckMotionTrackerModeAndNumber method.";
            Debug.LogWarning("[IndependentTracker] " + modeMessage);
            return;
        }

        Array modes = Enum.GetValues(modeType);
        string modeList = "";

        foreach (object mode in modes)
        {
            modeList += mode.ToString() + " ";
        }

        object selectedMode = SelectMode(modes);

        if (selectedMode == null)
        {
            modeMessage =
                "Mode request failed."
                + "\nCould not select independent mode."
                + "\nAvailable modes: " + modeList;
            Debug.LogWarning("[IndependentTracker] " + modeMessage);
            return;
        }

        try
        {
            object result;

            ParameterInfo[] ps = method.GetParameters();

            if (ps.Length == 1)
            {
                result = method.Invoke(null, new object[] { selectedMode });
            }
            else if (ps.Length == 2)
            {
                // 有些 SDK 版本第二个参数可能是数量，先传 2。
                result = method.Invoke(null, new object[] { selectedMode, 2 });
            }
            else
            {
                modeMessage =
                    "Mode request failed."
                    + "\nUnsupported CheckMotionTrackerModeAndNumber param count: " + ps.Length
                    + "\nAvailable modes: " + modeList;
                return;
            }

            modeMessage =
                "Mode request sent."
                + "\nSelected mode: " + selectedMode
                + "\nResult: " + result
                + "\nAvailable modes: " + modeList
                + "\nIf PICO popup appears, choose Confirm.";

            Debug.Log("[IndependentTracker] " + modeMessage);
        }
        catch (Exception e)
        {
            modeMessage =
                "Mode request exception."
                + "\n" + e.GetType().Name
                + "\n" + e.Message
                + "\nAvailable modes: " + modeList;

            Debug.LogWarning("[IndependentTracker] " + modeMessage);
        }
    }

    private Type FindTypeByName(string shortName)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (Assembly asm in assemblies)
        {
            Type[] types;

            try
            {
                types = asm.GetTypes();
            }
            catch
            {
                continue;
            }

            foreach (Type t in types)
            {
                if (t.Name == shortName)
                {
                    return t;
                }
            }
        }

        return null;
    }

    private object SelectMode(Array modes)
    {
        string key = preferredModeKeyword.ToLower();

        foreach (object mode in modes)
        {
            string name = mode.ToString().ToLower();

            if (name.Contains(key) && !name.Contains("body"))
            {
                return mode;
            }
        }

        foreach (object mode in modes)
        {
            string name = mode.ToString().ToLower();

            if (name.Contains("independent") ||
                name.Contains("object") ||
                name.Contains("tracker") ||
                name.Contains("motion"))
            {
                if (!name.Contains("body"))
                {
                    return mode;
                }
            }
        }

        return null;
    }

    private void ReadTrackers()
    {
        devices.Clear();
        trackerPoses.Clear();

        InputDevices.GetDevices(devices);

        for (int i = 0; i < devices.Count; i++)
        {
            InputDevice device = devices[i];

            if (!device.isValid)
            {
                continue;
            }

            if (!IsTrackerCandidate(device))
            {
                continue;
            }

            Vector3 pos;
            Quaternion rot;

            bool hasPos = device.TryGetFeatureValue(CommonUsages.devicePosition, out pos);
            bool hasRot = device.TryGetFeatureValue(CommonUsages.deviceRotation, out rot);

            if (!hasPos || !hasRot)
            {
                continue;
            }

            if (IsNearZero(pos))
            {
                continue;
            }

            TrackerPose pose = new TrackerPose();
            pose.device = device;
            pose.position = pos;
            pose.rotation = rot;

            trackerPoses.Add(pose);
        }

        if (trackerPoses.Count < 2)
        {
            hasTwoTrackers = false;

            if (userPositionProvider != null)
            {
                userPositionProvider.MarkTrackerDataInvalid();
            }

            debugMessage =
                "Independent tracker not ready."
                + "\nValid tracker poses: " + trackerPoses.Count
                + "\nXR devices found: " + devices.Count
                + "\n\n[Mode Request]\n" + modeMessage
                + "\n\n" + BuildDeviceSummary();

            return;
        }

        TrackerPose a = trackerPoses[0];
        TrackerPose b = trackerPoses[1];

        Vector3 posA = a.position;
        Vector3 posB = b.position;

        if (trackerA != null)
        {
            trackerA.position = posA;
            trackerA.rotation = a.rotation;
        }

        if (trackerB != null)
        {
            trackerB.position = posB;
            trackerB.rotation = b.rotation;
        }

        if (userCenter != null)
        {
            Vector3 center = (posA + posB) * 0.5f;
            center.y = 0.1f;
            userCenter.position = center;
        }

        if (userPositionProvider != null)
        {
            userPositionProvider.SetFootPositions(posA, posB);
        }

        hasTwoTrackers = true;

        debugMessage =
            "Independent tracker OK"
            + "\nTracker A: " + posA.ToString("F3")
            + "\nTracker B: " + posB.ToString("F3")
            + "\nCenter: " + ((posA + posB) * 0.5f).ToString("F3")
            + "\n\n[Mode Request]\n" + modeMessage
            + "\n\n" + BuildTrackerSummary();
    }

    private bool IsTrackerCandidate(InputDevice device)
    {
        InputDeviceCharacteristics c = device.characteristics;
        string name = device.name.ToLower();
        string manufacturer = device.manufacturer.ToLower();

        if (ignoreHmd)
        {
            if ((c & InputDeviceCharacteristics.HeadMounted) != 0)
            {
                return false;
            }

            if (name.Contains("hmd") || name.Contains("head"))
            {
                return false;
            }
        }

        if (ignoreControllers)
        {
            if ((c & InputDeviceCharacteristics.Controller) != 0)
            {
                return false;
            }

            if (name.Contains("controller"))
            {
                return false;
            }
        }

        bool looksLikeTracker =
            name.Contains("tracker") ||
            name.Contains("motion") ||
            name.Contains("pico motion") ||
            manufacturer.Contains("pico") ||
            (c & InputDeviceCharacteristics.TrackedDevice) != 0;

        return looksLikeTracker;
    }

    private string BuildDeviceSummary()
    {
        if (devices.Count == 0)
        {
            return "No XR input devices found.";
        }

        string s = "Devices:";

        int max = Mathf.Min(devices.Count, 10);

        for (int i = 0; i < max; i++)
        {
            InputDevice d = devices[i];

            s +=
                "\n- " + d.name
                + " | " + d.manufacturer
                + " | " + d.characteristics;
        }

        return s;
    }

    private string BuildTrackerSummary()
    {
        string s = "Tracker Devices:";

        for (int i = 0; i < trackerPoses.Count; i++)
        {
            TrackerPose p = trackerPoses[i];

            s +=
                "\n[" + i + "] "
                + p.device.name
                + " | " + p.device.manufacturer
                + " | " + p.device.characteristics
                + " | " + p.position.ToString("F3");
        }

        return s;
    }

    private bool IsNearZero(Vector3 v)
    {
        return Mathf.Abs(v.x) < 0.0001f &&
               Mathf.Abs(v.y) < 0.0001f &&
               Mathf.Abs(v.z) < 0.0001f;
    }
}