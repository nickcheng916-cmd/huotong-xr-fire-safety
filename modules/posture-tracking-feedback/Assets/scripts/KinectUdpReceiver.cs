using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class KinectUdpReceiver : MonoBehaviour
{
    [Header("Network")]
    public int listenPort = 5005;
    public bool autoStart = true;

    [Header("Timeout")]
    public float staleAfterSeconds = 0.8f;

    [Header("Debug")]
    public bool logRawJson = false;

    public KinectPosePacket LatestPose { get; private set; }
    public bool HasFreshPose => LatestPose != null && Time.time - lastReceiveLocalTime <= staleAfterSeconds;
    public float LastReceiveLocalTime => lastReceiveLocalTime;
    public string LastRawJson => lastRawJson;

    private UdpClient client;
    private Thread receiveThread;
    private volatile bool running;

    private readonly object jsonLock = new object();
    private string pendingJson;
    private string lastRawJson;
    private float lastReceiveLocalTime = -999f;

    private void Start()
    {
        if (autoStart)
            StartReceiver();
    }

    public void StartReceiver()
    {
        if (running) return;

        try
        {
            client = new UdpClient(listenPort);
            client.Client.ReceiveTimeout = 500;
            running = true;
            receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            receiveThread.Start();
            Debug.Log("[KinectUdpReceiver] Listening on UDP port " + listenPort);
        }
        catch (Exception e)
        {
            Debug.LogError("[KinectUdpReceiver] Failed to start: " + e.Message);
            StopReceiver();
        }
    }

    private void ReceiveLoop()
    {
        IPEndPoint any = new IPEndPoint(IPAddress.Any, listenPort);

        while (running)
        {
            try
            {
                byte[] bytes = client.Receive(ref any);
                string json = Encoding.UTF8.GetString(bytes);
                lock (jsonLock)
                {
                    pendingJson = json;
                }
            }
            catch (SocketException)
            {
                // Timeout is expected; loop again so StopReceiver can exit cleanly.
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[KinectUdpReceiver] Receive error: " + e.Message);
            }
        }
    }

    private void Update()
    {
        string json = null;
        lock (jsonLock)
        {
            if (!string.IsNullOrEmpty(pendingJson))
            {
                json = pendingJson;
                pendingJson = null;
            }
        }

        if (string.IsNullOrEmpty(json))
            return;

        try
        {
            KinectPosePacket packet = JsonUtility.FromJson<KinectPosePacket>(json);
            if (packet == null || packet.protocol != "KINECT_ESCAPE_POSE_V1")
                return;

            LatestPose = packet;
            lastRawJson = json;
            lastReceiveLocalTime = Time.time;

            if (logRawJson)
                Debug.Log("[KinectUdpReceiver] " + json);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[KinectUdpReceiver] JSON parse failed: " + e.Message + " JSON=" + json);
        }
    }

    public void StopReceiver()
    {
        running = false;

        try { client?.Close(); } catch { }
        client = null;

        if (receiveThread != null && receiveThread.IsAlive)
        {
            try { receiveThread.Join(100); } catch { }
        }
        receiveThread = null;
    }

    private void OnDestroy()
    {
        StopReceiver();
    }
}
