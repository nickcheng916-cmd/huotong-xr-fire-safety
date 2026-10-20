using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PicoScoreUIAutoBuilder : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;
    public Canvas picoScoreCanvas;
    public PicoScoreUdpReceiver scoreReceiver;

    [Header("Auto Setup")]
    public bool buildOnStart = true;
    public bool followCamera = true;

    /*
     * 重要：
     * 这版使用你 Hierarchy 里已经存在的 PicoScoreUICanvas。
     * 不再创建 PicoScoreAutoUICanvas。
     * 不再隐藏 PicoScoreUICanvas。
     */
    private const string ExistingCanvasName = "PicoScoreUICanvas";

    /*
     * 位置目标：
     * 位于左侧 Heart Rate UI 下方，左边缘尽量和 Heart Rate UI 对齐。
     */
    private static readonly Vector3 FORCE_LOCAL_POSITION = new Vector3(-1.05f, -0.34f, 2.15f);
    private static readonly Vector3 FORCE_LOCAL_ROTATION = Vector3.zero;
    private static readonly Vector3 FORCE_LOCAL_SCALE = new Vector3(0.00128f, 0.00128f, 0.00128f);

    private static readonly Vector2 FORCE_CANVAS_SIZE = new Vector2(720f, 360f);
    private static readonly Vector2 FORCE_PANEL_SIZE = new Vector2(720f, 360f);

    [Header("Colors")]
    public Color panelColor = new Color(0.02f, 0.04f, 0.06f, 0.42f);
    public Color titleBarColor = new Color(0.0f, 0.62f, 1.0f, 0.95f);
    public Color borderColor = new Color(0.0f, 0.62f, 1.0f, 1.0f);
    public Color labelColor = new Color(0.45f, 0.85f, 1.0f, 1.0f);
    public Color textColor = Color.white;
    public Color scoreColor = new Color(0.35f, 1.0f, 0.35f, 1.0f);
    public Color progressBackgroundColor = new Color(0.08f, 0.1f, 0.12f, 0.6f);
    public Color progressFillColor = new Color(0.1f, 0.85f, 1.0f, 1.0f);

    private RectTransform canvasRect;
    private RectTransform panelRect;

    private TextMeshProUGUI connectionText;
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI poseText;
    private TextMeshProUGUI resultText;
    private TextMeshProUGUI distanceText;
    private TextMeshProUGUI suggestionText;
    private Image scoreFillImage;

    private void Start()
    {
        if (buildOnStart)
        {
            RebuildUI();
        }
    }

    private void LateUpdate()
    {
        if (followCamera)
        {
            ApplyTransform();
        }
    }

    [ContextMenu("Rebuild Pico Score UI")]
    public void RebuildUI()
    {
        FindReferences();
        SetupExistingCanvas();
        ClearOldChildren();
        CreatePanel();
        CreateTitleBar();
        CreateTextElements();
        CreateProgressBar();
        BindReceiver();
        ApplyTransform();

        Debug.Log("[PicoScoreUIAutoBuilder] Rebuilt UI inside existing PicoScoreUICanvas.");
    }

    private void FindReferences()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (picoScoreCanvas == null)
        {
            GameObject canvasObj = GameObject.Find(ExistingCanvasName);

            if (canvasObj != null)
            {
                picoScoreCanvas = canvasObj.GetComponent<Canvas>();

                if (picoScoreCanvas == null)
                {
                    picoScoreCanvas = canvasObj.AddComponent<Canvas>();
                }
            }
        }

        if (picoScoreCanvas == null)
        {
            GameObject canvasObj = new GameObject(ExistingCanvasName);
            picoScoreCanvas = canvasObj.AddComponent<Canvas>();
        }

        if (scoreReceiver == null)
        {
            scoreReceiver = FindObjectOfType<PicoScoreUdpReceiver>();
        }

        if (scoreReceiver == null)
        {
            GameObject receiverObj = new GameObject("PicoScoreUdpReceiver");
            scoreReceiver = receiverObj.AddComponent<PicoScoreUdpReceiver>();
            scoreReceiver.listenPort = 7007;
            scoreReceiver.autoStart = true;

            Debug.Log("[PicoScoreUIAutoBuilder] Created PicoScoreUdpReceiver automatically.");
        }
    }

    private void SetupExistingCanvas()
    {
        GameObject canvasObj = picoScoreCanvas.gameObject;
        canvasObj.name = ExistingCanvasName;
        canvasObj.SetActive(true);

        picoScoreCanvas.renderMode = RenderMode.WorldSpace;
        picoScoreCanvas.worldCamera = targetCamera;
        picoScoreCanvas.overrideSorting = true;
        picoScoreCanvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvasObj.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;

        GraphicRaycaster raycaster = canvasObj.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = canvasObj.AddComponent<GraphicRaycaster>();
        }

        raycaster.enabled = false;

        CanvasGroup group = canvasObj.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = canvasObj.AddComponent<CanvasGroup>();
        }

        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;

        canvasRect = canvasObj.GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            canvasRect = canvasObj.AddComponent<RectTransform>();
        }

        canvasRect.sizeDelta = FORCE_CANVAS_SIZE;
        canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
        canvasRect.anchorMax = new Vector2(0.5f, 0.5f);
        canvasRect.pivot = new Vector2(0.5f, 0.5f);
    }

    private void ClearOldChildren()
    {
        Transform root = picoScoreCanvas.transform;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private void CreatePanel()
    {
        GameObject panelObj = new GameObject("ScorePanel");
        panelObj.transform.SetParent(picoScoreCanvas.transform, false);

        panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = FORCE_PANEL_SIZE;
        panelRect.localRotation = Quaternion.identity;
        panelRect.localScale = Vector3.one;

        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = panelColor;
        panelImage.raycastTarget = false;

        CreateLine(panelObj.transform, "TopLine", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -50f), new Vector2(0f, 3f));
        CreateLine(panelObj.transform, "BottomLine", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 3f), new Vector2(0f, 4f));
        CreateLine(panelObj.transform, "LeftLine", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(3f, 0f), new Vector2(5f, 0f));
        CreateLine(panelObj.transform, "RightLine", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-3f, 0f), new Vector2(5f, 0f));
    }

    private void CreateTitleBar()
    {
        GameObject titleBar = new GameObject("TitleBar");
        titleBar.transform.SetParent(panelRect.transform, false);

        RectTransform rect = titleBar.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 50f);

        Image image = titleBar.AddComponent<Image>();
        image.color = titleBarColor;
        image.raycastTarget = false;

        GameObject titleTextObj = new GameObject("TitleText");
        titleTextObj.transform.SetParent(titleBar.transform, false);

        RectTransform titleRect = titleTextObj.AddComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(20f, 0f);
        titleRect.offsetMax = new Vector2(-20f, 0f);

        TextMeshProUGUI titleText = titleTextObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "FIRE ESCAPE SCORE";
        titleText.fontSize = 29f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = textColor;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.raycastTarget = false;
    }

    private void CreateTextElements()
    {
        connectionText = CreateText(
            "ConnectionText",
            new Vector2(-135f, 105f),
            new Vector2(430f, 32f),
            "Score: Waiting",
            21f,
            FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft,
            textColor
        );

        CreateText(
            "ScoreLabelText",
            new Vector2(240f, 105f),
            new Vector2(170f, 28f),
            "SCORE",
            20f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            labelColor
        );

        scoreText = CreateText(
            "ScoreText",
            new Vector2(240f, 48f),
            new Vector2(180f, 80f),
            "--",
            60f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            scoreColor
        );

        poseText = CreateText(
            "PoseText",
            new Vector2(-135f, 68f),
            new Vector2(430f, 32f),
            "Pose: NO DATA",
            22f,
            FontStyles.Normal,
            TextAlignmentOptions.MidlineLeft,
            textColor
        );

        resultText = CreateText(
            "ResultText",
            new Vector2(-100f, 30f),
            new Vector2(520f, 34f),
            "Result: Waiting",
            23f,
            FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft,
            textColor
        );

        distanceText = CreateText(
            "DistanceText",
            new Vector2(-135f, -8f),
            new Vector2(430f, 32f),
            "Distance: -- m",
            22f,
            FontStyles.Normal,
            TextAlignmentOptions.MidlineLeft,
            textColor
        );

        CreateText(
            "SuggestionLabelText",
            new Vector2(-100f, -50f),
            new Vector2(520f, 28f),
            "SUGGESTION",
            20f,
            FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft,
            labelColor
        );

        suggestionText = CreateText(
            "SuggestionText",
            new Vector2(-100f, -92f),
            new Vector2(520f, 60f),
            "Suggestion: Waiting for PC score data",
            21f,
            FontStyles.Normal,
            TextAlignmentOptions.TopLeft,
            textColor
        );

        suggestionText.enableWordWrapping = true;
        suggestionText.overflowMode = TextOverflowModes.Overflow;
        suggestionText.lineSpacing = -5f;
        suggestionText.paragraphSpacing = -3f;
    }

    private void CreateProgressBar()
    {
        GameObject bgObj = new GameObject("ScoreFillBackground");
        bgObj.transform.SetParent(panelRect.transform, false);

        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = new Vector2(0f, -150f);
        bgRect.sizeDelta = new Vector2(640f, 18f);

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = progressBackgroundColor;
        bgImage.raycastTarget = false;

        GameObject fillObj = new GameObject("ScoreFillImage");
        fillObj.transform.SetParent(panelRect.transform, false);

        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0.5f, 0.5f);
        fillRect.anchorMax = new Vector2(0.5f, 0.5f);
        fillRect.pivot = new Vector2(0.5f, 0.5f);
        fillRect.anchoredPosition = new Vector2(0f, -150f);
        fillRect.sizeDelta = new Vector2(640f, 18f);

        scoreFillImage = fillObj.AddComponent<Image>();
        scoreFillImage.color = progressFillColor;
        scoreFillImage.raycastTarget = false;
        scoreFillImage.type = Image.Type.Filled;
        scoreFillImage.fillMethod = Image.FillMethod.Horizontal;
        scoreFillImage.fillOrigin = 0;
        scoreFillImage.fillAmount = 1f;
    }

    private TextMeshProUGUI CreateText(
        string name,
        Vector2 position,
        Vector2 size,
        string value,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment,
        Color color
    )
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(panelRect.transform, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;

        return text;
    }

    private void BindReceiver()
    {
        if (scoreReceiver == null)
        {
            scoreReceiver = FindObjectOfType<PicoScoreUdpReceiver>();
        }

        if (scoreReceiver == null)
        {
            GameObject receiverObj = new GameObject("PicoScoreUdpReceiver");
            scoreReceiver = receiverObj.AddComponent<PicoScoreUdpReceiver>();
            scoreReceiver.listenPort = 7007;
            scoreReceiver.autoStart = true;
        }

        scoreReceiver.connectionText = connectionText;
        scoreReceiver.scoreText = scoreText;
        scoreReceiver.poseText = poseText;
        scoreReceiver.resultText = resultText;
        scoreReceiver.distanceText = distanceText;
        scoreReceiver.suggestionText = suggestionText;
        scoreReceiver.scoreFillImage = scoreFillImage;
    }

    private void ApplyTransform()
    {
        if (picoScoreCanvas == null)
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera != null && picoScoreCanvas.transform.parent != targetCamera.transform)
        {
            picoScoreCanvas.transform.SetParent(targetCamera.transform, false);
        }

        picoScoreCanvas.transform.localPosition = FORCE_LOCAL_POSITION;
        picoScoreCanvas.transform.localEulerAngles = FORCE_LOCAL_ROTATION;
        picoScoreCanvas.transform.localScale = FORCE_LOCAL_SCALE;

        if (canvasRect != null)
        {
            canvasRect.sizeDelta = FORCE_CANVAS_SIZE;
        }

        if (panelRect != null)
        {
            panelRect.sizeDelta = FORCE_PANEL_SIZE;
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.localRotation = Quaternion.identity;
            panelRect.localScale = Vector3.one;
        }
    }

    private void CreateLine(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 position,
        Vector2 size
    )
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = obj.AddComponent<Image>();
        image.color = borderColor;
        image.raycastTarget = false;
    }
}