using UnityEngine;
using TMPro;

public class KinectFirePostureEvaluator : MonoBehaviour
{
    [Header("Kinect")]
    public KinectUdpReceiver receiver;

    [Header("Fire Distance")]
    public Transform player;
    public Transform fireSource;
    public bool calculateDistanceHere = true;

    [Tooltip("如果你已经在别的脚本里算好了火焰距离，可以每帧把那个距离赋值给 externalFireDistance")]
    public float externalFireDistance = 999f;

    [Header("Distance Thresholds")]
    public float warningDistance = 2.5f;
    public float dangerDistance = 1.2f;

    [Header("UI")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI actionText;
    public TextMeshProUGUI scoreText;

    [Header("Score")]
    public int startScore = 100;
    public int score = 100;
    public float penaltyCooldown = 1.2f;

    private float lastPenaltyTime = -999f;

    public string currentPose = "NO_DATA";
    public float fireDistance = 999f;

    public bool hasKinectData;
    public bool isTracked;
    public bool isLowPosture;
    public bool isCovering;
    public bool isRunning;
    public bool isStanding;
    public bool isDangerStand;
    public bool isCorrectEscapePosture;

    private void Start()
    {
        score = startScore;

        if (receiver == null)
            receiver = FindObjectOfType<KinectUdpReceiver>();

        if (player == null && Camera.main != null)
            player = Camera.main.transform;
    }

    private void Update()
    {
        UpdateFireDistance();
        UpdateKinectPosture();
        EvaluateFireEscapePosture();
        UpdateUI();
    }

    private void UpdateFireDistance()
    {
        if (calculateDistanceHere && player != null && fireSource != null)
        {
            Vector3 a = player.position;
            Vector3 b = fireSource.position;

            // 只算水平距离，不算高度
            a.y = 0f;
            b.y = 0f;

            fireDistance = Vector3.Distance(a, b);
        }
        else
        {
            fireDistance = externalFireDistance;
        }
    }

    private void UpdateKinectPosture()
    {
        hasKinectData = receiver != null && receiver.HasFreshPose;

        if (!hasKinectData)
        {
            currentPose = "NO_DATA";
            isTracked = false;
            isLowPosture = false;
            isCovering = false;
            isRunning = false;
            isStanding = false;
            return;
        }

        KinectPosePacket pose = receiver.LatestPose;

        if (pose == null || !pose.isTracked)
        {
            currentPose = "NO_BODY";
            isTracked = false;
            isLowPosture = false;
            isCovering = false;
            isRunning = false;
            isStanding = false;
            return;
        }

        isTracked = true;
        currentPose = pose.mainPose;

        isLowPosture =
            PoseIs(pose, "BEND") ||
            PoseIs(pose, "SQUAT") ||
            PoseIs(pose, "CRAWL");

        isCovering =
            PoseIs(pose, "COVER_HEAD") ||
            PoseIs(pose, "COVER_MOUTH");

        isRunning =
            PoseIs(pose, "RUN");

        isStanding =
            PoseIs(pose, "STAND");
    }

    private bool PoseIs(KinectPosePacket pose, string name)
    {
        if (pose == null) return false;

        if (pose.mainPose == name)
            return true;

        if (pose.tags != null)
        {
            for (int i = 0; i < pose.tags.Length; i++)
            {
                if (pose.tags[i] == name)
                    return true;
            }
        }

        return false;
    }

    private void EvaluateFireEscapePosture()
    {
        isDangerStand = false;
        isCorrectEscapePosture = false;

        if (!hasKinectData || !isTracked)
            return;

        bool inWarningArea = fireDistance <= warningDistance;
        bool inDangerArea = fireDistance <= dangerDistance;

        if (!inWarningArea)
        {
            isCorrectEscapePosture = true;
            return;
        }

        if (inDangerArea && !isLowPosture)
        {
            isDangerStand = true;
            Penalize(5);
            return;
        }

        if (inWarningArea && !isLowPosture)
        {
            Penalize(3);
            return;
        }

        if (inWarningArea && !isCovering)
        {
            Penalize(2);
            return;
        }

        if (inWarningArea && isRunning)
        {
            Penalize(2);
            return;
        }

        if (inWarningArea && isLowPosture && isCovering)
        {
            isCorrectEscapePosture = true;
        }
    }

    private void Penalize(int value)
    {
        if (Time.time - lastPenaltyTime < penaltyCooldown)
            return;

        score -= value;
        score = Mathf.Clamp(score, 0, startScore);
        lastPenaltyTime = Time.time;
    }

    private void UpdateUI()
    {
        string action = GetActionResult();
        string suggestion = GetSuggestion();

        if (statusText != null)
        {
            statusText.text =
                "KINECT POSTURE\n" +
                "Kinect: " + (hasKinectData ? "Connected" : "Waiting") + "\n" +
                "Tracked: " + isTracked + "\n" +
                "Pose: " + currentPose + "\n" +
                "Fire Distance: " + fireDistance.ToString("F2") + " m\n" +
                "Low Posture: " + isLowPosture + "\n" +
                "Cover Mouth: " + isCovering + "\n" +
                "Running: " + isRunning + "\n" +
                "Result: " + action + "\n" +
                "Suggestion: " + suggestion;
        }

        if (actionText != null)
        {
            actionText.text = "Action: " + action;
        }

        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }

    private string GetActionResult()
    {
        if (!hasKinectData)
            return "Waiting for Kinect data";

        if (!isTracked)
            return "No body detected";

        if (isDangerStand)
            return "Danger stand";

        if (isCorrectEscapePosture && fireDistance <= warningDistance)
            return "Correct escape posture";

        if (isLowPosture && isCovering)
            return "Low posture with cover";

        if (isLowPosture)
            return "Low posture";

        if (isCovering)
            return "Cover mouth";

        if (isRunning)
            return "Running";

        return currentPose;
    }

    private string GetSuggestion()
    {
        if (!hasKinectData)
            return "请检查 PC 是否正在发送 Kinect 数据";

        if (!isTracked)
            return "请站到 Kinect 摄像头识别范围内";

        if (fireDistance > warningDistance)
            return "保持撤离方向，远离火源";

        if (fireDistance <= dangerDistance && !isLowPosture)
            return "离火源过近，请立即弯腰或蹲下";

        if (fireDistance <= warningDistance && !isLowPosture)
            return "靠近火源，请降低身体姿态";

        if (fireDistance <= warningDistance && !isCovering)
            return "请用手捂住口鼻或护住头部";

        if (fireDistance <= warningDistance && isRunning)
            return "不要快速奔跑，保持低姿态撤离";

        return "姿态正确，保持低姿态撤离";
    }
}