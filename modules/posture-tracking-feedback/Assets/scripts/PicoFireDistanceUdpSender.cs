using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class PicoFireDistanceUdpSender : MonoBehaviour
{
    [Header("UDP Target: PC")]
    public string pcIp = "192.168.31.95";
    public int pcPort = 6006;

    [Header("Scene References")]
    public Transform player;      // 一般拖 Main Camera 或 XR Origin
    public Transform fireSource;  // 拖 FireSource_Demo

    [Header("Distance Settings")]
    public bool ignoreHeight = true;
    public float warningDistance = 2.5f;
    public float dangerDistance = 1.2f;

    [Header("Send Settings")]
    public bool autoStart = true;
    public float sendRate = 15f;

    [Header("Debug")]
    public bool logJson = false;

    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;
    private float timer;

    [Serializable]
    public class FireDistancePacket
    {
        public string protocol = "PICO_FIRE_DISTANCE_V1";
        public float distanceToFire;
        public bool inWarningArea;
        public bool inDangerArea;
        public Vec3 playerWorld;
        public Vec3 fireWorld;
        public double timestamp;
    }

    [Serializable]
    public class Vec3
    {
        public float x;
        public float y;
        public float z;

        public Vec3(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }
    }

    private void Start()
    {
        if (player == null && Camera.main != null)
            player = Camera.main.transform;

        if (autoStart)
            StartSender();
    }

    public void StartSender()
    {
        try
        {
            udpClient = new UdpClient();
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(pcIp), pcPort);
            Debug.Log("[PicoFireDistanceUdpSender] Started. Target = " + pcIp + ":" + pcPort);
        }
        catch (Exception e)
        {
            Debug.LogError("[PicoFireDistanceUdpSender] Start failed: " + e.Message);
        }
    }

    private void Update()
    {
        if (udpClient == null || player == null || fireSource == null)
            return;

        timer += Time.deltaTime;

        if (timer < 1f / sendRate)
            return;

        timer = 0f;
        SendDistancePacket();
    }

    private void SendDistancePacket()
    {
        Vector3 playerPos = player.position;
        Vector3 firePos = fireSource.position;

        Vector3 a = playerPos;
        Vector3 b = firePos;

        if (ignoreHeight)
        {
            a.y = 0f;
            b.y = 0f;
        }

        float distance = Vector3.Distance(a, b);

        FireDistancePacket packet = new FireDistancePacket();
        packet.distanceToFire = distance;
        packet.inWarningArea = distance <= warningDistance;
        packet.inDangerArea = distance <= dangerDistance;
        packet.playerWorld = new Vec3(playerPos);
        packet.fireWorld = new Vec3(firePos);
        packet.timestamp = Time.realtimeSinceStartupAsDouble;

        string json = JsonUtility.ToJson(packet);

        byte[] data = Encoding.UTF8.GetBytes(json);
        udpClient.Send(data, data.Length, remoteEndPoint);

        if (logJson)
            Debug.Log("[PICO → PC] " + json);
    }

    private void OnDestroy()
    {
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }
    }

    private void OnApplicationQuit()
    {
        OnDestroy();
    }
}