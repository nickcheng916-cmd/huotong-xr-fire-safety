using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PicoScoreUdpReceiver : MonoBehaviour
{
    [Header("UDP Listen")]
    public int listenPort = 7007;
    public bool autoStart = true;

    [Header("Timeout")]
    public float staleAfterSeconds = 1.0f;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI poseText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI suggestionText;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI connectionText;

    [Tooltip("Optional: If you have a score fill image, set Image Type to Filled.")]
    public Image scoreFillImage;

    [Header("Debug")]
    public bool logRawJson = false;

    public ScorePacket LatestPacket { get; private set; }

    public bool HasFreshScore
    {
        get
        {
            return LatestPacket != null &&
                   Time.time - lastReceiveLocalTime <= staleAfterSeconds;
        }
    }

    [Serializable]
    public class ScorePacket
    {
        public string protocol;

        public int score;
        public int maxScore;

        public string pose;
        public string result;
        public string suggestion;

        public float fireDistance;
        public bool hasKinect;
        public bool hasDistance;
        public bool isTracked;

        public bool isLowPosture;
        public bool isCovering;
        public bool isRunning;
        public bool inWarningArea;
        public bool inDangerArea;

        public double timestamp;
    }

    private UdpClient udpClient;
    private Thread receiveThread;
    private volatile bool running;

    private readonly object lockObj = new object();
    private string pendingJson;

    private float lastReceiveLocalTime = -999f;

    private void Start()
    {
        if (autoStart)
        {
            StartReceiver();
        }
    }

    public void StartReceiver()
    {
        if (running)
        {
            return;
        }

        try
        {
            udpClient = new UdpClient(listenPort);
            running = true;

            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            Debug.Log("[PicoScoreUdpReceiver] Listening on port " + listenPort);
        }
        catch (Exception e)
        {
            Debug.LogError("[PicoScoreUdpReceiver] Start failed: " + e.Message);
        }
    }

    private void ReceiveLoop()
    {
        IPEndPoint remote = new IPEndPoint(IPAddress.Any, listenPort);

        while (running)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remote);
                string json = Encoding.UTF8.GetString(data);

                lock (lockObj)
                {
                    pendingJson = json;
                }
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                if (running)
                {
                    Debug.LogWarning("[PicoScoreUdpReceiver] Socket receive error.");
                }
            }
            catch (Exception e)
            {
                if (running)
                {
                    Debug.LogWarning("[PicoScoreUdpReceiver] Receive error: " + e.Message);
                }
            }
        }
    }

    private void Update()
    {
        ReadPendingJson();
        UpdateUI();
    }

    private void ReadPendingJson()
    {
        string json = null;

        lock (lockObj)
        {
            if (!string.IsNullOrEmpty(pendingJson))
            {
                json = pendingJson;
                pendingJson = null;
            }
        }

        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        try
        {
            ScorePacket packet = JsonUtility.FromJson<ScorePacket>(json);

            if (packet != null && packet.protocol == "FIRE_ESCAPE_SCORE_V1")
            {
                LatestPacket = packet;
                lastReceiveLocalTime = Time.time;

                if (logRawJson)
                {
                    Debug.Log("[PICO Score Received] " + json);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[PicoScoreUdpReceiver] JSON parse failed: " + e.Message);
        }
    }

    private void UpdateUI()
    {
        if (!HasFreshScore)
        {
            if (connectionText != null)
            {
                connectionText.text = "Score: Waiting";
            }

            if (scoreText != null)
            {
                scoreText.text = "--";
            }

            if (poseText != null)
            {
                poseText.text = "Pose: NO DATA";
            }

            if (resultText != null)
            {
                resultText.text = "Result: Waiting";
            }

            if (suggestionText != null)
            {
                suggestionText.text = "Suggestion: Waiting for PC score data";
            }

            if (distanceText != null)
            {
                distanceText.text = "Distance: -- m";
            }

            if (scoreFillImage != null)
            {
                scoreFillImage.fillAmount = 0f;
            }

            return;
        }

        ScorePacket p = LatestPacket;

        if (connectionText != null)
        {
            connectionText.text = "Score: Connected";
        }

        if (scoreText != null)
        {
            scoreText.text = p.score.ToString();
        }

        if (poseText != null)
        {
            poseText.text = "Pose: " + GetEnglishPose(p);
        }

        if (resultText != null)
        {
            resultText.text = "Result: " + GetEnglishResult(p);
        }

        if (suggestionText != null)
        {
            suggestionText.text = "Suggestion: " + GetEnglishSuggestion(p);
        }

        if (distanceText != null)
        {
            if (p.hasDistance)
            {
                distanceText.text = "Distance: " + p.fireDistance.ToString("F2") + " m";
            }
            else
            {
                distanceText.text = "Distance: -- m";
            }
        }

        if (scoreFillImage != null)
        {
            float v = 0f;

            if (p.maxScore > 0)
            {
                v = Mathf.Clamp01((float)p.score / p.maxScore);
            }

            scoreFillImage.fillAmount = v;
        }
    }

    private string GetEnglishPose(ScorePacket p)
    {
        if (p == null || string.IsNullOrEmpty(p.pose))
        {
            return "NO DATA";
        }

        return p.pose;
    }

    private string GetEnglishResult(ScorePacket p)
    {
        if (p == null)
        {
            return "Waiting";
        }

        if (!p.hasKinect)
        {
            return "No Kinect data";
        }

        if (!p.isTracked)
        {
            return "No body detected";
        }

        if (p.inDangerArea && !p.isLowPosture)
        {
            return "Danger stand";
        }

        if (p.inWarningArea && p.isLowPosture && p.isCovering && !p.isRunning)
        {
            return "Correct escape posture";
        }

        if (p.inWarningArea && p.isLowPosture && p.isCovering)
        {
            return "Low posture with cover";
        }

        if (p.inWarningArea && p.isLowPosture)
        {
            return "Low posture";
        }

        if (p.inWarningArea && p.isCovering)
        {
            return "Cover mouth";
        }

        if (p.isRunning)
        {
            return "Running";
        }

        if (p.hasDistance && !p.inWarningArea && !p.inDangerArea)
        {
            return "Safe distance";
        }

        if (!string.IsNullOrEmpty(p.result) && IsAsciiText(p.result))
        {
            return p.result;
        }

        return "Normal";
    }

    private string GetEnglishSuggestion(ScorePacket p)
    {
        if (p == null)
        {
            return "Waiting for PC score data";
        }

        if (!p.hasKinect)
        {
            return "Check PC Kinect sender";
        }

        if (!p.isTracked)
        {
            return "Stand inside Kinect range";
        }

        if (!p.hasDistance)
        {
            return "Check fire distance data";
        }

        if (p.inDangerArea && !p.isLowPosture)
        {
            return "Crouch immediately";
        }

        if (p.inWarningArea && !p.isLowPosture)
        {
            return "Lower your body";
        }

        if (p.inWarningArea && !p.isCovering)
        {
            return "Cover your mouth or head";
        }

        if (p.inWarningArea && p.isRunning)
        {
            return "Slow down and stay low";
        }

        if (p.inWarningArea && p.isLowPosture && p.isCovering)
        {
            return "Good posture. Keep evacuating";
        }

        if (!p.inWarningArea && !p.inDangerArea)
        {
            return "Keep moving away from fire";
        }

        return "Keep moving safely";
    }

    private bool IsAsciiText(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] > 127)
            {
                return false;
            }
        }

        return true;
    }

    private void OnDestroy()
    {
        StopReceiver();
    }

    private void OnApplicationQuit()
    {
        StopReceiver();
    }

    public void StopReceiver()
    {
        running = false;

        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }

        if (receiveThread != null)
        {
            try
            {
                receiveThread.Join(100);
            }
            catch
            {
                // Ignore thread shutdown errors.
            }

            receiveThread = null;
        }
    }
}