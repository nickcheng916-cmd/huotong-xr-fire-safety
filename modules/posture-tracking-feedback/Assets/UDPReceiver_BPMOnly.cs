using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using System.Collections.Concurrent;
using System.Globalization;
using UnityEngine.UI;

public class UDPReceiver_BPMOnly : MonoBehaviour
{
    [Header("Network")]
    public int listenPort = 8080;
    public float staleAfterSeconds = 3f;

    [Header("Existing VR UI References")]
    public TextMeshProUGUI bpmText;
    public RectTransform heartIcon;

    [Header("Animation")]
    public float maxBeatScale = 1.3f;
    public Color normalColor = Color.white;
    public Color dangerColor = Color.red;
    public int dangerThreshold = 100;

    [Header("Modern UI - Auto Build")]
    public bool autoBuildModernUI = true;
    public bool hideLegacyUI = true;
    public Vector2 panelSize = new Vector2(560f, 180f);
    public float minVisualBpm = 50f;
    public float maxVisualBpm = 150f;
    public float uiSmoothSpeed = 8f;

    [Header("Modern UI - Colors")]
    public Color cardColor = new Color(0.045f, 0.060f, 0.090f, 0.92f);
    public Color cardHighlightColor = new Color(1f, 1f, 1f, 0.065f);
    public Color calmColor = new Color(0.22f, 0.78f, 1.00f, 1f);
    public Color warningColor = new Color(1.00f, 0.68f, 0.25f, 1f);
    public Color highColor = new Color(1.00f, 0.22f, 0.28f, 1f);
    public Color lowColor = new Color(0.58f, 0.64f, 1.00f, 1f);

    private UdpClient udpClient;
    private Thread receiveThread;
    private volatile bool isRunning;

    private readonly ConcurrentQueue<string> dataQueue = new ConcurrentQueue<string>();
    private readonly ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();

    private float currentBpm;
    private float lastValidPacketTime = -999f;
    private float displayedBpm;
    private Image heartImage;

    private RectTransform modernRoot;
    private RectTransform modernHeartIcon;
    private TMP_Text heartGlyphText;
    private TMP_Text mainBpmText;
    private TMP_Text unitText;
    private TMP_Text statusText;
    private TMP_Text hintText;
    private TMP_Text rangeText;
    private Image cardBackground;
    private Image leftAccent;
    private Image statusBadge;
    private Image progressFill;
    private Image progressGlow;
    private Image pulseRing;
    private Image signalDot;

    private void Start()
    {
        displayedBpm = currentBpm;

        if (heartIcon != null)
            heartImage = heartIcon.GetComponent<Image>();

        if (autoBuildModernUI)
            BuildModernHeartRateUI();

        StartUDP();
        RefreshUI(true);
    }

    private void OnEnable()
    {
        if (Application.isPlaying && receiveThread == null)
            StartUDP();
    }

    private void ReceiveData()
    {
        try
        {
            IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);

            while (isRunning)
            {
                try
                {
                    byte[] data = udpClient.Receive(ref anyIP);
                    string text = Encoding.UTF8.GetString(data).Trim();

                    dataQueue.Enqueue(text);
                    logQueue.Enqueue($"Received UDP heart rate: [{text}] from {anyIP.Address}:{anyIP.Port}");
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode != SocketError.TimedOut && isRunning)
                        logQueue.Enqueue($"UDP Socket error: {e.SocketErrorCode} / {e.Message}");
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }
        catch (Exception e)
        {
            logQueue.Enqueue($"UDP initialization failed. Check whether port {listenPort} is occupied or blocked: {e.Message}");
        }
    }

    private void Update()
    {
        while (logQueue.TryDequeue(out string log))
            Debug.Log(log);

        string latestData = null;
        while (dataQueue.TryDequeue(out string data))
            latestData = data;

        if (!string.IsNullOrEmpty(latestData))
        {
            if (TryParseBpm(latestData, out float parsedBpm))
            {
                currentBpm = parsedBpm;
                lastValidPacketTime = Time.unscaledTime;
            }
            else
            {
                Debug.LogWarning($"Unable to parse heart-rate data. Current payload: [{latestData}]");

                if (bpmText != null)
                {
                    bpmText.text = "RAW: " + latestData;
                    bpmText.color = dangerColor;
                }
            }
        }

        RefreshUI(false);
    }

    private bool TryParseBpm(string text, out float bpm)
    {
        return BpmPacketParser.TryParse(text, out bpm);
    }

    private void RefreshUI(bool immediate)
    {
        displayedBpm = immediate
            ? currentBpm
            : Mathf.Lerp(displayedBpm, currentBpm, Time.deltaTime * uiSmoothSpeed);

        bool hasSignal = currentBpm > 0f &&
            Time.unscaledTime - lastValidPacketTime <= staleAfterSeconds;
        int bpmRounded = Mathf.RoundToInt(displayedBpm);
        HeartRateState state = hasSignal ? GetState(displayedBpm) : HeartRateState.Disconnected;
        Color stateColor = GetStateColor(state);
        string stateLabel = GetStateLabel(state);
        string hint = GetHint(state);

        if (bpmText != null)
        {
            bpmText.text = hasSignal ? bpmRounded + " BPM" : "-- BPM";
            bpmText.color = stateColor;
        }

        RectTransform targetHeart = modernHeartIcon != null ? modernHeartIcon : heartIcon;
        if (targetHeart != null && hasSignal)
        {
            float beatFrequency = displayedBpm / 60f * Mathf.PI * 2f;
            float wave = Mathf.Pow(Mathf.Abs(Mathf.Sin(Time.time * beatFrequency / 2f)), 4f);
            float currentScale = Mathf.Lerp(1f, maxBeatScale, wave);
            targetHeart.localScale = new Vector3(currentScale, currentScale, 1f);

            if (heartImage != null)
                heartImage.color = stateColor;
        }

        if (heartGlyphText != null)
            heartGlyphText.color = stateColor;

        if (mainBpmText != null)
            mainBpmText.text = hasSignal ? bpmRounded.ToString(CultureInfo.InvariantCulture) : "--";

        if (unitText != null)
            unitText.color = new Color(1f, 1f, 1f, 0.72f);

        if (statusText != null)
            statusText.text = stateLabel;

        if (hintText != null)
            hintText.text = hint;

        if (rangeText != null)
            rangeText.text = $"Visual range {Mathf.RoundToInt(minVisualBpm)}-{Mathf.RoundToInt(maxVisualBpm)} BPM  |  High > {dangerThreshold} BPM";

        if (cardBackground != null)
            cardBackground.color = Color.Lerp(cardColor, stateColor, state == HeartRateState.High ? 0.12f : 0.04f);

        if (leftAccent != null)
            leftAccent.color = stateColor;

        if (statusBadge != null)
            statusBadge.color = stateColor;

        float fill = hasSignal ? Mathf.InverseLerp(minVisualBpm, maxVisualBpm, displayedBpm) : 0f;
        fill = Mathf.Clamp01(fill);

        if (progressFill != null)
        {
            progressFill.fillAmount = fill;
            progressFill.color = stateColor;
        }

        if (progressGlow != null)
        {
            float glowPulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f);
            Color glowColor = stateColor;
            glowColor.a = Mathf.Lerp(0.10f, state == HeartRateState.High ? 0.45f : 0.26f, glowPulse);
            progressGlow.fillAmount = fill;
            progressGlow.color = glowColor;
        }

        if (pulseRing != null)
        {
            float beatFrequency = Mathf.Max(0.5f, displayedBpm / 60f) * Mathf.PI * 2f;
            float wave = Mathf.Pow(Mathf.Abs(Mathf.Sin(Time.time * beatFrequency / 2f)), 3f);
            pulseRing.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.22f, wave);
            Color ringColor = stateColor;
            ringColor.a = Mathf.Lerp(0.10f, 0.36f, 1f - wave);
            pulseRing.color = ringColor;
        }

        if (signalDot != null)
        {
            Color dot = stateColor;
            dot.a = 0.65f + 0.35f * Mathf.Sin(Time.time * 6f);
            signalDot.color = dot;
        }
    }

    private void BuildModernHeartRateUI()
    {
        Canvas canvas = null;
        if (bpmText != null)
            canvas = bpmText.GetComponentInParent<Canvas>();
        if (canvas == null && heartIcon != null)
            canvas = heartIcon.GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
            return;

        Transform old = canvas.transform.Find("ModernHeartRateHUD");
        if (old != null)
            Destroy(old.gameObject);

        GameObject root = CreateUIObject("ModernHeartRateHUD", canvas.transform);
        modernRoot = root.GetComponent<RectTransform>();
        modernRoot.anchorMin = new Vector2(0.5f, 0.5f);
        modernRoot.anchorMax = new Vector2(0.5f, 0.5f);
        modernRoot.pivot = new Vector2(0.5f, 0.5f);
        modernRoot.anchoredPosition = Vector2.zero;
        modernRoot.sizeDelta = panelSize;

        cardBackground = root.AddComponent<Image>();
        cardBackground.color = cardColor;
        cardBackground.raycastTarget = false;
        cardBackground.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        cardBackground.type = Image.Type.Sliced;

        Shadow cardShadow = root.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        cardShadow.effectDistance = new Vector2(7f, -7f);

        Outline outline = root.AddComponent<Outline>();
        outline.effectColor = cardHighlightColor;
        outline.effectDistance = new Vector2(1f, -1f);

        Image topGlow = CreateImage("TopHighlight", root.transform, new Color(1f, 1f, 1f, 0.055f));
        SetTopLeft(topGlow.rectTransform, 12f, -10f, panelSize.x - 24f, 36f);

        leftAccent = CreateImage("LeftAccent", root.transform, calmColor);
        SetTopLeft(leftAccent.rectTransform, 0f, 0f, 8f, panelSize.y);

        TextMeshProUGUI title = CreateText("Title", root.transform, "HEART RATE", 21, FontStyles.Bold, TextAlignmentOptions.Left);
        title.characterSpacing = 7f;
        title.color = new Color(1f, 1f, 1f, 0.92f);
        SetTopLeft(title.rectTransform, 28f, -18f, 230f, 28f);

        signalDot = CreateImage("SignalDot", root.transform, calmColor);
        SetTopLeft(signalDot.rectTransform, 230f, -24f, 12f, 12f);

        TextMeshProUGUI liveText = CreateText("LiveText", root.transform, "LIVE UDP", 13, FontStyles.Bold, TextAlignmentOptions.Left);
        liveText.color = new Color(1f, 1f, 1f, 0.56f);
        SetTopLeft(liveText.rectTransform, 250f, -20f, 110f, 20f);

        pulseRing = CreateImage("PulseRing", root.transform, new Color(1f, 1f, 1f, 0.1f));
        SetTopLeft(pulseRing.rectTransform, 34f, -58f, 82f, 82f);

        GameObject heartGo = CreateUIObject("HeartGlyph", root.transform);
        modernHeartIcon = heartGo.GetComponent<RectTransform>();
        SetTopLeft(modernHeartIcon, 45f, -65f, 60f, 60f);
        TextMeshProUGUI heartText = heartGo.AddComponent<TextMeshProUGUI>();
        heartText.text = "♥";
        heartText.fontSize = 54f;
        heartText.fontStyle = FontStyles.Bold;
        heartText.alignment = TextAlignmentOptions.Center;
        heartText.raycastTarget = false;
        heartGlyphText = heartText;

        mainBpmText = CreateText("MainBPM", root.transform, "70", 66, FontStyles.Bold, TextAlignmentOptions.Left);
        mainBpmText.enableAutoSizing = false;
        SetTopLeft(mainBpmText.rectTransform, 132f, -55f, 160f, 76f);

        unitText = CreateText("BPMUnit", root.transform, "BPM", 20, FontStyles.Bold, TextAlignmentOptions.Left);
        SetTopLeft(unitText.rectTransform, 292f, -82f, 64f, 30f);

        statusBadge = CreateImage("StatusBadge", root.transform, calmColor);
        statusBadge.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        statusBadge.type = Image.Type.Sliced;
        SetTopLeft(statusBadge.rectTransform, panelSize.x - 160f, -24f, 128f, 34f);

        statusText = CreateText("StatusText", statusBadge.transform, "NORMAL", 16, FontStyles.Bold, TextAlignmentOptions.Center);
        statusText.color = Color.white;
        Stretch(statusText.rectTransform, 0f, 0f, 0f, 0f);

        Image progressBg = CreateImage("ProgressBG", root.transform, new Color(1f, 1f, 1f, 0.12f));
        progressBg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        progressBg.type = Image.Type.Sliced;
        SetTopLeft(progressBg.rectTransform, 132f, -132f, panelSize.x - 170f, 18f);

        progressGlow = CreateImage("ProgressGlow", progressBg.transform, calmColor);
        Stretch(progressGlow.rectTransform, 0f, 0f, 0f, 0f);
        progressGlow.type = Image.Type.Filled;
        progressGlow.fillMethod = Image.FillMethod.Horizontal;
        progressGlow.fillOrigin = 0;
        progressGlow.fillAmount = 0f;

        progressFill = CreateImage("ProgressFill", progressBg.transform, calmColor);
        Stretch(progressFill.rectTransform, 0f, 0f, 0f, 0f);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;
        progressFill.fillAmount = 0f;

        hintText = CreateText("HintText", root.transform, "状态平稳，继续保持。", 18, FontStyles.Bold, TextAlignmentOptions.Left);
        hintText.color = new Color(1f, 1f, 1f, 0.86f);
        SetTopLeft(hintText.rectTransform, 132f, -108f, panelSize.x - 180f, 24f);

        rangeText = CreateText("RangeText", root.transform, "Visual range 50-150 BPM", 12, FontStyles.Normal, TextAlignmentOptions.Left);
        rangeText.color = new Color(1f, 1f, 1f, 0.48f);
        SetTopLeft(rangeText.rectTransform, 132f, -153f, panelSize.x - 170f, 20f);

        if (hideLegacyUI)
        {
            if (bpmText != null)
                bpmText.gameObject.SetActive(false);
            if (heartIcon != null)
                heartIcon.gameObject.SetActive(false);
        }

        bpmText = mainBpmText as TextMeshProUGUI;
        heartIcon = modernHeartIcon;
        heartImage = null;
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.layer = parent != null ? parent.gameObject.layer : 5;
        go.transform.SetParent(parent, false);
        return go;
    }

    private Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = CreateUIObject(name, parent);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject go = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        tmp.color = Color.white;
        return tmp;
    }

    private void SetTopLeft(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private void Stretch(RectTransform rt, float left, float top, float right, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    private HeartRateState GetState(float bpm)
    {
        if (bpm <= 0f)
            return HeartRateState.Disconnected;
        if (bpm < 60f)
            return HeartRateState.Low;
        if (bpm > dangerThreshold)
            return HeartRateState.High;
        if (bpm >= dangerThreshold - 12f)
            return HeartRateState.Elevated;
        return HeartRateState.Normal;
    }

    private Color GetStateColor(HeartRateState state)
    {
        switch (state)
        {
            case HeartRateState.Low:
                return lowColor;
            case HeartRateState.Elevated:
                return warningColor;
            case HeartRateState.High:
            case HeartRateState.Disconnected:
                return highColor;
            default:
                return calmColor;
        }
    }

    private string GetStateLabel(HeartRateState state)
    {
        switch (state)
        {
            case HeartRateState.Low:
                return "LOW";
            case HeartRateState.Elevated:
                return "ELEVATED";
            case HeartRateState.High:
                return "HIGH";
            case HeartRateState.Disconnected:
                return "NO DATA";
            default:
                return "NORMAL";
        }
    }

    private string GetHint(HeartRateState state)
    {
        switch (state)
        {
            case HeartRateState.Low:
                return "心率偏低，请观察状态。";
            case HeartRateState.Elevated:
                return "心率上升，建议放慢动作。";
            case HeartRateState.High:
                return "心率过高，请及时休息。";
            case HeartRateState.Disconnected:
                return "等待 UDP 心率数据...";
            default:
                return "状态平稳，继续保持。";
        }
    }

    private void StartUDP()
    {
        if (isRunning)
            return;

        try
        {
            udpClient = new UdpClient(listenPort);
            udpClient.Client.ReceiveTimeout = 1000;
            isRunning = true;
            receiveThread = new Thread(ReceiveData) { IsBackground = true };
            receiveThread.Start();
            Debug.Log($"Heart-rate UDP listener started on port {listenPort}");
        }
        catch (Exception e)
        {
            StopUDP();
            Debug.LogError($"Cannot start heart-rate UDP listener: {e.Message}");
        }
    }

    private void OnDisable()
    {
        StopUDP();
    }

    private void OnDestroy()
    {
        StopUDP();
    }

    private void OnApplicationQuit()
    {
        StopUDP();
    }

    private void StopUDP()
    {
        isRunning = false;

        if (udpClient != null)
        {
            udpClient.Close();
        }

        if (receiveThread != null && receiveThread.IsAlive)
            receiveThread.Join();

        receiveThread = null;
        udpClient = null;
        currentBpm = 0f;
        lastValidPacketTime = -999f;
        Debug.Log("Heart-rate UDP listener closed");
    }

    private enum HeartRateState
    {
        Disconnected,
        Low,
        Normal,
        Elevated,
        High
    }
}
