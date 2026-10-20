using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KinectPostureFollowPanel : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;
    public KinectFirePostureEvaluator evaluator;

    [Header("Auto Disable Old UI")]
    public bool hideOldKinectUICanvas = true;
    public string oldCanvasName = "KinectUICanvas";

    /*
     * 这一版已经根据你最新截图调整：
     * 1. 大致方位保持不变
     * 2. 面板整体再向右移动一点
     * 3. 高度保持不变
     */
    private static readonly Vector3 FORCE_LOCAL_POSITION = new Vector3(-1.06f, -0.36f, 2.2f);
    private static readonly Vector3 FORCE_LOCAL_ROTATION = Vector3.zero;
    private static readonly Vector3 FORCE_LOCAL_SCALE = new Vector3(0.00145f, 0.00145f, 0.00145f);

    private static readonly Vector2 FORCE_PANEL_SIZE = new Vector2(800f, 360f);

    [Header("Panel Size")]
    public float titleHeight = 58f;

    [Header("Font Size")]
    public float titleFontSize = 32f;
    public float bodyFontSize = 24f;
    public float scoreFontSize = 32f;

    [Header("Colors")]
    public Color panelColor = new Color(0.02f, 0.04f, 0.06f, 0.78f);
    public Color titleBarColor = new Color(0.1f, 0.65f, 1f, 0.95f);
    public Color lineColor = new Color(0.1f, 0.65f, 1f, 1f);

    private Canvas canvas;
    private RectTransform canvasRect;
    private TMP_Text titleText;
    private TMP_Text bodyText;

    private const string panelCanvasName = "KinectPostureFollowPanelCanvas";

    private void Awake()
    {
        SetupPanel();
    }

    private void Start()
    {
        SetupPanel();
    }

    private void OnEnable()
    {
        SetupPanel();
    }

    private void Update()
    {
        FindReferencesIfNeeded();
        UpdatePanelTransform();
        UpdateText();
    }

    [ContextMenu("Setup Kinect Posture Panel")]
    public void SetupPanel()
    {
        FindReferencesIfNeeded();
        DisableOldKinectUI();
        DisableOldLayoutScripts();
        CreateOrFindCanvas();
        CreatePanelVisuals();
        UpdatePanelTransform();
        UpdateText();
    }

    private void FindReferencesIfNeeded()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (evaluator == null)
        {
            evaluator = FindObjectOfType<KinectFirePostureEvaluator>();
        }
    }

    private void DisableOldKinectUI()
    {
        if (!hideOldKinectUICanvas)
        {
            return;
        }

        GameObject oldCanvas = GameObject.Find(oldCanvasName);

        if (oldCanvas != null && oldCanvas.name != panelCanvasName)
        {
            oldCanvas.SetActive(false);
        }

        if (evaluator != null)
        {
            evaluator.statusText = null;
            evaluator.actionText = null;
            evaluator.scoreText = null;
        }
    }

    private void DisableOldLayoutScripts()
    {
        MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>();

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];

            if (behaviour == null)
            {
                continue;
            }

            if (behaviour == this)
            {
                continue;
            }

            if (behaviour.GetType().Name == "KinectUIAutoFollowLayout")
            {
                behaviour.enabled = false;
            }
        }
    }

    private void CreateOrFindCanvas()
    {
        GameObject canvasObject = GameObject.Find(panelCanvasName);

        if (canvasObject == null)
        {
            canvasObject = new GameObject(panelCanvasName);
        }

        canvas = canvasObject.GetComponent<Canvas>();

        if (canvas == null)
        {
            canvas = canvasObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = targetCamera;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();

        if (scaler == null)
        {
            scaler = canvasObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;

        GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();

        if (raycaster == null)
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        canvasRect = canvasObject.GetComponent<RectTransform>();

        if (canvasRect == null)
        {
            canvasRect = canvasObject.AddComponent<RectTransform>();
        }

        canvasRect.sizeDelta = FORCE_PANEL_SIZE;
        canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
        canvasRect.anchorMax = new Vector2(0.5f, 0.5f);
        canvasRect.pivot = new Vector2(0.5f, 0.5f);
    }

    private void CreatePanelVisuals()
    {
        if (canvas == null)
        {
            return;
        }

        Transform root = canvas.transform;

        GameObject background = GetOrCreateChild(root, "Background");
        RectTransform backgroundRect = GetRect(background);
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        backgroundRect.SetAsFirstSibling();

        Image backgroundImage = GetImage(background);
        backgroundImage.color = panelColor;

        GameObject titleBar = GetOrCreateChild(root, "TitleBar");
        RectTransform titleRect = GetRect(titleBar);
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = new Vector2(0f, titleHeight);

        Image titleImage = GetImage(titleBar);
        titleImage.color = titleBarColor;

        GameObject titleTextObject = GetOrCreateChild(titleBar.transform, "TitleText");
        RectTransform titleTextRect = GetRect(titleTextObject);
        titleTextRect.anchorMin = Vector2.zero;
        titleTextRect.anchorMax = Vector2.one;
        titleTextRect.offsetMin = new Vector2(24f, 0f);
        titleTextRect.offsetMax = new Vector2(-24f, 0f);

        titleText = GetText(titleTextObject);
        titleText.text = "KINECT POSTURE";
        titleText.fontSize = titleFontSize;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject bodyTextObject = GetOrCreateChild(root, "BodyText");
        RectTransform bodyRect = GetRect(bodyTextObject);
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(28f, 22f);
        bodyRect.offsetMax = new Vector2(-28f, -titleHeight - 16f);

        bodyText = GetText(bodyTextObject);
        bodyText.fontSize = bodyFontSize;
        bodyText.color = Color.white;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.enableWordWrapping = true;
        bodyText.overflowMode = TextOverflowModes.Overflow;
        bodyText.lineSpacing = -10f;
        bodyText.paragraphSpacing = -6f;

        CreateLine(root, "TopLine", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -titleHeight), new Vector2(0f, 4f));
        CreateLine(root, "BottomLine", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 3f), new Vector2(0f, 5f));
        CreateLine(root, "LeftLine", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(3f, 0f), new Vector2(6f, 0f));
    }

    private GameObject GetOrCreateChild(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);

        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent, false);
        return child;
    }

    private RectTransform GetRect(GameObject obj)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();

        if (rect == null)
        {
            rect = obj.AddComponent<RectTransform>();
        }

        return rect;
    }

    private Image GetImage(GameObject obj)
    {
        Image image = obj.GetComponent<Image>();

        if (image == null)
        {
            image = obj.AddComponent<Image>();
        }

        return image;
    }

    private TMP_Text GetText(GameObject obj)
    {
        TMP_Text text = obj.GetComponent<TMP_Text>();

        if (text == null)
        {
            text = obj.AddComponent<TextMeshProUGUI>();
        }

        text.raycastTarget = false;
        return text;
    }

    private void CreateLine(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta
    )
    {
        GameObject line = GetOrCreateChild(parent, name);
        RectTransform rect = GetRect(line);

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Image image = GetImage(line);
        image.color = lineColor;
    }

    private void UpdatePanelTransform()
    {
        if (canvas == null)
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera != null && canvas.transform.parent != targetCamera.transform)
        {
            canvas.transform.SetParent(targetCamera.transform, false);
        }

        canvas.transform.localPosition = FORCE_LOCAL_POSITION;
        canvas.transform.localEulerAngles = FORCE_LOCAL_ROTATION;
        canvas.transform.localScale = FORCE_LOCAL_SCALE;

        if (canvasRect != null)
        {
            canvasRect.sizeDelta = FORCE_PANEL_SIZE;
        }
    }

    private void UpdateText()
    {
        if (bodyText == null)
        {
            return;
        }

        if (evaluator == null)
        {
            bodyText.text = "<color=#ff5555>KinectFirePostureEvaluator Missing</color>";
            return;
        }

        string kinectStatus = evaluator.hasKinectData
            ? "<color=#55ff55>Connected</color>"
            : "<color=#ffaa00>Waiting</color>";

        string trackedStatus = evaluator.isTracked
            ? "<color=#55ff55>True</color>"
            : "<color=#ff5555>False</color>";

        string lowStatus = evaluator.isLowPosture
            ? "<color=#55ff55>True</color>"
            : "<color=#ff5555>False</color>";

        string coverStatus = evaluator.isCovering
            ? "<color=#55ff55>True</color>"
            : "<color=#ff5555>False</color>";

        string runningStatus = evaluator.isRunning
            ? "<color=#ffaa00>True</color>"
            : "<color=#55ff55>False</color>";

        string result = GetResultText();
        string suggestion = GetSuggestionText();

        bodyText.text =
            "<b>Kinect:</b> " + kinectStatus + "\n" +
            "<b>Tracked:</b> " + trackedStatus + "\n" +
            "<b>Pose:</b> " + evaluator.currentPose + "\n" +
            "<b>Fire Distance:</b> " + evaluator.fireDistance.ToString("F2") + " m\n" +
            "<b>Low Posture:</b> " + lowStatus + "     <b>Cover:</b> " + coverStatus + "\n" +
            "<b>Running:</b> " + runningStatus + "\n" +
            "<b>Result:</b> " + result + "\n" +
            "<b>Suggestion:</b> " + suggestion + "\n\n" +
            "<size=" + scoreFontSize + "><b>Score:</b> <color=#55ff55>" + evaluator.score + "</color></size>";
    }

    private string GetResultText()
    {
        if (!evaluator.hasKinectData)
        {
            return "<color=#ffaa00>Waiting for Kinect data</color>";
        }

        if (!evaluator.isTracked)
        {
            return "<color=#ff5555>No body detected</color>";
        }

        if (evaluator.isDangerStand)
        {
            return "<color=#ff3333>Danger stand</color>";
        }

        if (evaluator.isCorrectEscapePosture)
        {
            return "<color=#55ff55>Correct escape posture</color>";
        }

        if (evaluator.isLowPosture && evaluator.isCovering)
        {
            return "<color=#55ff55>Low posture with cover</color>";
        }

        if (evaluator.isLowPosture)
        {
            return "<color=#ffaa00>Low posture</color>";
        }

        if (evaluator.isCovering)
        {
            return "<color=#ffaa00>Cover mouth</color>";
        }

        if (evaluator.isRunning)
        {
            return "<color=#ffaa00>Running</color>";
        }

        return evaluator.currentPose;
    }

    private string GetSuggestionText()
    {
        if (!evaluator.hasKinectData)
        {
            return "Check PC Kinect sender / UDP.";
        }

        if (!evaluator.isTracked)
        {
            return "Stand inside Kinect range.";
        }

        if (evaluator.fireDistance > evaluator.warningDistance)
        {
            return "Keep moving away from fire.";
        }

        if (evaluator.fireDistance <= evaluator.dangerDistance && !evaluator.isLowPosture)
        {
            return "Crouch immediately.";
        }

        if (evaluator.fireDistance <= evaluator.warningDistance && !evaluator.isLowPosture)
        {
            return "Lower your body.";
        }

        if (evaluator.fireDistance <= evaluator.warningDistance && !evaluator.isCovering)
        {
            return "Cover mouth or head.";
        }

        if (evaluator.fireDistance <= evaluator.warningDistance && evaluator.isRunning)
        {
            return "Slow down and stay low.";
        }

        return "Correct posture. Keep evacuating.";
    }
}