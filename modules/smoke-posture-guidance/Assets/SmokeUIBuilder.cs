using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SmokeUIBuilder : MonoBehaviour
{
    public SmokeExposureController smokeController;

    [Header("Action Sprites")]
    public Sprite runSprite;
    public Sprite crouchRunSprite;

    [Header("Panel Layout")]
    public Vector2 defaultPanelSize = new Vector2(660f, 280f);
    public Vector2 panelMargin = new Vector2(24f, 24f);

    [ContextMenu("Build Beautiful Smoke UI")]
    public void BuildBeautifulSmokeUI()
    {
        Canvas canvas = FindOrCreateCanvas();
        EnsureEventSystem();

        Canvas.ForceUpdateCanvases();

        Transform old = canvas.transform.Find("SmokeHUD");
        if (old != null)
        {
            if (Application.isPlaying) Destroy(old.gameObject);
            else DestroyImmediate(old.gameObject);
        }

        RectTransform canvasRt = canvas.GetComponent<RectTransform>();
        float canvasW = canvasRt.rect.width;
        float canvasH = canvasRt.rect.height;

        float panelW = Mathf.Min(defaultPanelSize.x, canvasW - panelMargin.x * 2f);
        float panelH = Mathf.Min(defaultPanelSize.y, canvasH - panelMargin.y * 2f);

        panelW = Mathf.Max(panelW, 460f);
        panelH = Mathf.Max(panelH, 250f);

        GameObject root = CreateUIObject("SmokeHUD", canvas.transform);
        RectTransform rootRt = root.GetComponent<RectTransform>();
        SetTopLeft(rootRt, panelMargin.x, -panelMargin.y, panelW, panelH);

        Image panelImage = root.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.12f, 0.18f, 0.92f);
        panelImage.raycastTarget = false;

        Shadow shadow = root.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        shadow.effectDistance = new Vector2(6f, -6f);

        Outline outline = root.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.05f);
        outline.effectDistance = new Vector2(1f, -1f);

        // 左侧强调条
        Image accentBar = CreateImage("AccentBar", root.transform, new Color(0.23f, 0.82f, 0.95f, 1f));
        SetTopLeft(accentBar.rectTransform, 0f, 0f, 8f, panelH);

        // 标题
        TextMeshProUGUI title = CreateText(
            "Title",
            root.transform,
            "SMOKE MONITOR",
            24,
            FontStyles.Bold,
            TextAlignmentOptions.Left
        );
        SetTopLeft(title.rectTransform, 24f, -18f, 240f, 28f);

        // 副标题
        TextMeshProUGUI subtitle = CreateText(
            "Subtitle",
            root.transform,
            "Realtime ceiling smoke estimator",
            15,
            FontStyles.Normal,
            TextAlignmentOptions.Left
        );
        subtitle.color = new Color(1f, 1f, 1f, 0.70f);
        SetTopLeft(subtitle.rectTransform, 24f, -46f, 320f, 20f);

        // 百分比
        TextMeshProUGUI percentText = CreateText(
            "PercentText",
            root.transform,
            "0%",
            46,
            FontStyles.Bold,
            TextAlignmentOptions.Right
        );
        SetTopLeft(percentText.rectTransform, panelW - 180f, -16f, 150f, 54f);

        // 状态徽章
        Image badgeBg = CreateImage("StatusBadge", root.transform, new Color(0.23f, 0.82f, 0.95f, 1f));
        SetTopLeft(badgeBg.rectTransform, panelW - 180f, -74f, 150f, 34f);

        TextMeshProUGUI badgeText = CreateText(
            "BadgeText",
            badgeBg.transform,
            "SAFE",
            18,
            FontStyles.Bold,
            TextAlignmentOptions.Center
        );
        Stretch(badgeText.rectTransform, 0f, 0f, 0f, 0f);

        // 分割线
        Image divider = CreateImage("Divider", root.transform, new Color(1f, 1f, 1f, 0.08f));
        SetTopLeft(divider.rectTransform, 24f, -92f, panelW - 48f, 1f);

        // 进度条背景
        Image progressBg = CreateImage("ProgressBG", root.transform, new Color(1f, 1f, 1f, 0.10f));
        SetTopLeft(progressBg.rectTransform, 24f, -110f, panelW - 48f, 28f);

        // 进度条填充
        Image progressFill = CreateImage("ProgressFill", progressBg.transform, new Color(0.23f, 0.82f, 0.95f, 1f));
        Stretch(progressFill.rectTransform, 0f, 0f, 0f, 0f);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;
        progressFill.fillAmount = 0f;

        // 进度条柔光
        Image progressGlow = CreateImage("ProgressGlow", progressBg.transform, new Color(0.23f, 0.82f, 0.95f, 0.18f));
        Stretch(progressGlow.rectTransform, 0f, 0f, 0f, 0f);
        progressGlow.type = Image.Type.Filled;
        progressGlow.fillMethod = Image.FillMethod.Horizontal;
        progressGlow.fillOrigin = 0;
        progressGlow.fillAmount = 0f;

        // 建议文本
        TextMeshProUGUI tipText = CreateText(
            "TipText",
            root.transform,
            "Suggestion: Move normally",
            24,
            FontStyles.Bold,
            TextAlignmentOptions.Left
        );
        SetTopLeft(tipText.rectTransform, 24f, -154f, panelW - 250f, 30f);

        // 明细文本
        TextMeshProUGUI detailText = CreateText(
            "DetailText",
            root.transform,
            "Head 0%    Chest 0%",
            18,
            FontStyles.Normal,
            TextAlignmentOptions.Left
        );
        detailText.color = new Color(1f, 1f, 1f, 0.80f);
        SetTopLeft(detailText.rectTransform, 24f, -194f, panelW - 250f, 24f);

        // 阈值说明
        TextMeshProUGUI footerText = CreateText(
            "FooterText",
            root.transform,
            "Crouch >= 45%    Crouch Run >= 75%",
            15,
            FontStyles.Normal,
            TextAlignmentOptions.Left
        );
        footerText.color = new Color(1f, 1f, 1f, 0.60f);
        SetTopLeft(footerText.rectTransform, 24f, -222f, panelW - 250f, 20f);

        // ===== 动作预览卡片 =====
        Image previewCard = CreateImage("ActionPreviewCard", root.transform, new Color(0f, 0f, 0f, 0.18f));
        SetTopLeft(previewCard.rectTransform, panelW - 210f, -126f, 170f, 128f);

        Shadow previewShadow = previewCard.gameObject.AddComponent<Shadow>();
        previewShadow.effectColor = new Color(0f, 0f, 0f, 0.20f);
        previewShadow.effectDistance = new Vector2(3f, -3f);

        Outline previewOutline = previewCard.gameObject.AddComponent<Outline>();
        previewOutline.effectColor = new Color(1f, 1f, 1f, 0.08f);
        previewOutline.effectDistance = new Vector2(1f, -1f);

        // 预览卡片顶部小标题
        TextMeshProUGUI previewTitle = CreateText(
            "PreviewTitle",
            previewCard.transform,
            "ACTION PREVIEW",
            14,
            FontStyles.Bold,
            TextAlignmentOptions.Left
        );
        previewTitle.color = new Color(1f, 1f, 1f, 0.72f);
        SetTopLeft(previewTitle.rectTransform, 10f, -8f, 140f, 16f);

        // 图片显示区背景
        Image previewImageBg = CreateImage("ActionPreviewBG", previewCard.transform, new Color(1f, 1f, 1f, 0.05f));
        SetTopLeft(previewImageBg.rectTransform, 10f, -28f, 150f, 72f);

        Outline previewImageOutline = previewImageBg.gameObject.AddComponent<Outline>();
        previewImageOutline.effectColor = new Color(1f, 1f, 1f, 0.06f);
        previewImageOutline.effectDistance = new Vector2(1f, -1f);

        // 图片本体
        Image actionPreview = CreateImage("ActionPreviewImage", previewImageBg.transform, Color.white);
        Stretch(actionPreview.rectTransform, 6f, 6f, 6f, 6f);
        actionPreview.preserveAspect = true;
        actionPreview.sprite = runSprite;

        // 标签底板
        Image actionLabelBg = CreateImage("ActionLabelBG", previewCard.transform, new Color(1f, 1f, 1f, 0.06f));
        SetTopLeft(actionLabelBg.rectTransform, 10f, -106f, 150f, 18f);

        // 标签文字
        TextMeshProUGUI actionLabel = CreateText(
            "ActionLabel",
            actionLabelBg.transform,
            "Action: Run",
            14,
            FontStyles.Bold,
            TextAlignmentOptions.Center
        );
        Stretch(actionLabel.rectTransform, 0f, 0f, 0f, 0f);

        SmokeUIController controller = root.AddComponent<SmokeUIController>();
        controller.smokeController = smokeController != null ? smokeController : FindObjectOfType<SmokeExposureController>();
        controller.cardBackground = panelImage;
        controller.accentBar = accentBar;
        controller.statusBadgeBackground = badgeBg;
        controller.progressFill = progressFill;
        controller.progressGlow = progressGlow;
        controller.percentText = percentText;
        controller.badgeText = badgeText;
        controller.tipText = tipText;
        controller.detailText = detailText;
        controller.footerText = footerText;
        controller.actionPreviewImage = actionPreview;
        controller.actionLabelText = actionLabel;
        controller.runSprite = runSprite;
        controller.crouchRunSprite = crouchRunSprite;
    }

    private Canvas FindOrCreateCanvas()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null) return canvas;

        GameObject canvasGo = new GameObject(
            "SmokeCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Canvas newCanvas = canvasGo.GetComponent<Canvas>();
        newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        newCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return newCanvas;
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule)
        );
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = CreateUIObject(name, parent);
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        string content,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment
    )
    {
        GameObject go = CreateUIObject(name, parent);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
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
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }
}