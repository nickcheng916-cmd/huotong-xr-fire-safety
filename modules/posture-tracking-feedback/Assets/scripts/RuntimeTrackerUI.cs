using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RuntimeTrackerUI : MonoBehaviour
{
    [Header("Panel Placement")]
    public float uiDistance = 2.2f;
    public float rightOffset = 0.72f;
    public float verticalOffset = 0.04f;
    public float uiScale = 0.00145f;

    [Header("Panel Size")]
    public Vector2 panelSize = new Vector2(560f, 420f);
    public float titleFontSize = 30f;
    public float bodyFontSize = 22f;

    [Header("Step Hold")]
    [Tooltip("非正常状态显示保持时间，防止一闪而过。")]
    public float importantStepHoldSeconds = 1.8f;

    [Header("Colors")]
    public Color panelColor = new Color(0.04f, 0.06f, 0.08f, 0.78f);
    public Color titleBarColor = new Color(0.1f, 0.65f, 1f, 0.95f);
    public Color dividerColor = new Color(0.1f, 0.65f, 1f, 0.9f);

    private PicoMotionTrackerReader motionReader;
    private UserPositionProvider userPositionProvider;
    private EmergencyScenarioController scenarioController;

    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform panelRect;
    private TMP_Text titleText;
    private TMP_Text bodyText;

    private float nextFindTime;

    private string heldStepText = "";
    private float heldStepUntilTime;

    private void Awake()
    {
        CreateUI();
    }

    private void Start()
    {
        FindRefs();
    }

    private void Update()
    {
        if (Time.time >= nextFindTime)
        {
            nextFindTime = Time.time + 1f;
            FindRefs();
        }

        UpdatePanelPlacement();
        UpdateUI();
    }

    private void FindRefs()
    {
        if (motionReader == null)
        {
            motionReader = FindObjectOfType<PicoMotionTrackerReader>();
        }

        if (userPositionProvider == null)
        {
            userPositionProvider = FindObjectOfType<UserPositionProvider>();
        }

        if (scenarioController == null)
        {
            scenarioController = FindObjectOfType<EmergencyScenarioController>();
        }
    }

    private void CreateUI()
    {
        Camera cam = Camera.main;

        GameObject oldCanvas = GameObject.Find("EmergencyRuntimeRightPanelCanvas");
        if (oldCanvas != null)
        {
            Destroy(oldCanvas);
        }

        GameObject canvasObj = new GameObject("EmergencyRuntimeRightPanelCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        if (cam != null)
        {
            canvasObj.transform.SetParent(cam.transform);
        }

        canvasObj.transform.localScale = Vector3.one * uiScale;

        canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = panelSize;

        GameObject panelObj = new GameObject("EmergencyRuntimePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);

        panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = panelSize;

        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = panelColor;
        panelImage.raycastTarget = false;

        CreateTitleBar(panelObj.transform);
        CreateBodyArea(panelObj.transform);
        CreateDecorLines(panelObj.transform);

        UpdatePanelPlacement();
    }

    private void CreateTitleBar(Transform parent)
    {
        GameObject titleBarObj = new GameObject("TitleBar");
        titleBarObj.transform.SetParent(parent, false);

        RectTransform titleBarRect = titleBarObj.AddComponent<RectTransform>();
        titleBarRect.anchorMin = new Vector2(0f, 1f);
        titleBarRect.anchorMax = new Vector2(1f, 1f);
        titleBarRect.pivot = new Vector2(0.5f, 1f);
        titleBarRect.anchoredPosition = Vector2.zero;
        titleBarRect.sizeDelta = new Vector2(0f, 58f);

        Image titleBarImage = titleBarObj.AddComponent<Image>();
        titleBarImage.color = titleBarColor;
        titleBarImage.raycastTarget = false;

        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(titleBarObj.transform, false);

        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(20f, 0f);
        titleRect.offsetMax = new Vector2(-20f, 0f);

        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "EMERGENCY STATUS";
        titleText.fontSize = titleFontSize;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.fontStyle = FontStyles.Bold;
        titleText.raycastTarget = false;
    }

    private void CreateBodyArea(Transform parent)
    {
        GameObject bodyObj = new GameObject("BodyText");
        bodyObj.transform.SetParent(parent, false);

        RectTransform bodyRect = bodyObj.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(24f, 18f);
        bodyRect.offsetMax = new Vector2(-24f, -72f);

        bodyText = bodyObj.AddComponent<TextMeshProUGUI>();
        bodyText.fontSize = bodyFontSize;
        bodyText.color = Color.white;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.enableWordWrapping = true;
        bodyText.overflowMode = TextOverflowModes.Overflow;
        bodyText.lineSpacing = -10f;
        bodyText.paragraphSpacing = -8f;
        bodyText.raycastTarget = false;
        bodyText.text = "Loading...";
    }

    private void CreateDecorLines(Transform parent)
    {
        CreateLine(
            parent,
            "TopLine",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, -62f),
            new Vector2(0f, 3f),
            dividerColor
        );

        CreateLine(
            parent,
            "BottomLine",
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 10f),
            new Vector2(0f, 3f),
            dividerColor
        );

        CreateLine(
            parent,
            "LeftAccent",
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(7f, 0f),
            new Vector2(5f, 0f),
            dividerColor
        );
    }

    private void CreateLine(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color
    )
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(parent, false);

        RectTransform lineRect = lineObj.AddComponent<RectTransform>();
        lineRect.anchorMin = anchorMin;
        lineRect.anchorMax = anchorMax;
        lineRect.pivot = new Vector2(0.5f, 0.5f);
        lineRect.anchoredPosition = anchoredPosition;
        lineRect.sizeDelta = sizeDelta;

        Image lineImage = lineObj.AddComponent<Image>();
        lineImage.color = color;
        lineImage.raycastTarget = false;
    }

    private void UpdatePanelPlacement()
    {
        if (canvas == null)
        {
            return;
        }

        Transform t = canvas.transform;
        t.localPosition = new Vector3(rightOffset, verticalOffset, uiDistance);
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one * uiScale;

        if (canvasRect != null)
        {
            canvasRect.sizeDelta = panelSize;
        }

        if (panelRect != null)
        {
            panelRect.sizeDelta = panelSize;
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.localRotation = Quaternion.identity;
            panelRect.localScale = Vector3.one;
        }
    }

    private void UpdateUI()
    {
        if (bodyText == null)
        {
            return;
        }

        bodyText.text =
            BuildTrackerConnectionInfo()
            + "\n\n"
            + BuildPlayerPositionInfo()
            + "\n\n"
            + BuildWristInfo()
            + "\n\n"
            + BuildCurrentStageInfo();
    }

    private string BuildTrackerConnectionInfo()
    {
        if (motionReader == null)
        {
            return
                "<b>TRACKER</b>\n"
                + "<color=#ff5555>Status: Reader Missing</color>";
        }

        bool connected = motionReader.hasTwoTrackers && motionReader.trackerCount >= 2;

        string connectionText = connected
            ? "<color=#55ff55>Connected</color>"
            : "<color=#ff5555>Waiting</color>";

        string trackerDataText = "Unknown";

        if (userPositionProvider != null)
        {
            trackerDataText = userPositionProvider.HasValidTrackerData
                ? "<color=#55ff55>Valid</color>"
                : "<color=#ffcc00>Invalid</color>";
        }

        return
            "<b>TRACKER</b>"
            + "\nStatus: " + connectionText
            + "   Count: " + motionReader.trackerCount
            + "\nData: " + trackerDataText;
    }

    private string BuildPlayerPositionInfo()
    {
        if (userPositionProvider == null)
        {
            return
                "<b>POSITION</b>\n"
                + "<color=#ff5555>UserPosition Missing</color>";
        }

        return
            "<b>POSITION</b>"
            + "\nPlayer: " + userPositionProvider.UserGroundPosition.ToString("F2")
            + "\nWorld:  " + userPositionProvider.RawUserGroundPosition.ToString("F2");
    }

    private string BuildWristInfo()
    {
        if (userPositionProvider == null)
        {
            return
                "<b>WRISTS</b>\n"
                + "<color=#ff5555>Tracker Data Missing</color>";
        }

        return
            "<b>WRISTS</b>"
            + "\nA: " + userPositionProvider.TrackerAPosition.ToString("F2")
            + "\nB: " + userPositionProvider.TrackerBPosition.ToString("F2");
    }

    private string BuildCurrentStageInfo()
    {
        string currentStep = GetCurrentStepText();
        bool isNormal = IsNormalStep(currentStep);

        if (!isNormal)
        {
            heldStepText = currentStep;
            heldStepUntilTime = Time.time + importantStepHoldSeconds;
            return "<b>CURRENT STEP</b>\n" + heldStepText;
        }

        if (!string.IsNullOrEmpty(heldStepText) && Time.time < heldStepUntilTime)
        {
            return "<b>CURRENT STEP</b>\n" + heldStepText;
        }

        heldStepText = currentStep;
        return "<b>CURRENT STEP</b>\n" + currentStep;
    }

    private string GetCurrentStepText()
    {
        if (scenarioController == null)
        {
            return "<color=#ff5555>Scenario Controller Missing</color>";
        }

        if (scenarioController.zones == null || scenarioController.zones.Count == 0)
        {
            return "<color=#ffaa00>No emergency zones found.</color>";
        }

        if (scenarioController.scenarioCompleted)
        {
            return "<color=#55ff55>Escape Complete.</color>";
        }

        if (scenarioController.isInsideFire)
        {
            return "<color=#ff3333>FIRE DANGER: Move away.</color>";
        }

        if (scenarioController.isNearFire)
        {
            return "<color=#ffaa00>Near Fire: Move away.</color>";
        }

        if (scenarioController.isInSmoke)
        {
            if (scenarioController.handNearFace)
            {
                return "<color=#55ff55>Smoke: Hand near face.</color>";
            }

            return "<color=#ffaa00>Smoke: Raise hand to mouth.</color>";
        }

        if (scenarioController.requireStayOnSafeRoute && !scenarioController.isOnSafeRoute)
        {
            return "<color=#ffaa00>Route Warning: Off route.</color>";
        }

        if (scenarioController.isWaving)
        {
            return "<color=#55ccff>Action: Waving detected.</color>";
        }

        return "<color=#55ff55>Status OK: Continue route.</color>";
    }

    private bool IsNormalStep(string stepText)
    {
        return stepText.Contains("Status OK");
    }
}