using System;

[Serializable]
public class KinectPosePacket
{
    public string protocol = "KINECT_ESCAPE_POSE_V1";

    public bool isTracked = false;

    public string mainPose = "NO_BODY";

    public string[] tags;

    public float timestamp;

    public float confidence;
}