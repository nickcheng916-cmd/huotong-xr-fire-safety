using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SmokeUIController : MonoBehaviour
{
    public SmokeExposureController smokeController;

    [Header("Main UI References")]
    public Image cardBackground;
    public Image accentBar;
    public Image statusBadgeBackground;
    public Image progressFill;
    public Image progressGlow;

    public TMP_Text percentText;
    public TMP_Text badgeText;
    public TMP_Text tipText;
    public TMP_Text detailText;
    public TMP_Text footerText;

    [Header("Action Preview")]
    public Image actionPreviewImage;
    public TMP_Text actionLabelText;
    public Sprite runSprite;
    public Sprite crouchRunSprite;

    [Tooltip("动作预览图切换阈值。-1 表示自动使用 crouchThreshold")]
    public float actionSwitchThreshold = 0.51f;

    [Header("Preview Breathing Effect")]
    public bool enablePreviewBreathing = true;
    public float previewPulseSpeed = 1.8f;
    public float previewScaleMin = 0.96f;
    public float previewScaleMax = 1.02f;
    public float previewAlphaMin = 0.90f;
    public float previewAlphaMax = 1.00f;

    [Header("Safe Colors")]
    public Color safeCardColor = new Color(0.08f, 0.12f, 0.18f, 0.92f);
    public Color safeAccentColor = new Color(0.23f, 0.82f, 0.95f, 1f);
    public Color safeFillColor = new Color(0.23f, 0.82f, 0.95f, 1f);

    [Header("Warning Colors")]
    public Color warningCardColor = new Color(0.20f, 0.14f, 0.08f, 0.94f);
    public Color warningAccentColor = new Color(1.00f, 0.72f, 0.20f, 1f);
    public Color warningFillColor = new Color(1.00f, 0.72f, 0.20f, 1f);

    [Header("Danger Colors")]
    public Color dangerCardColor = new Color(0.24f, 0.08f, 0.08f, 0.96f);
    public Color dangerAccentColor = new Color(1.00f, 0.32f, 0.32f, 1f);
    public Color dangerFillColor = new Color(1.00f, 0.32f, 0.32f, 1f);

    [Header("Bar Glow Effect")]
    public float glowPulseSpeed = 3.2f;
    public float glowMinAlpha = 0.10f;
    public float glowMaxAlpha = 0.42f;

    [Header("Danger Flash")]
    public float redFlashThreshold = 0.70f;
    public float redFlashSpeed = 4.5f;
    [Range(0f, 1f)] public float redFlashStrength = 0.35f;
    public Color flashRed = new Color(1f, 0.15f, 0.15f, 1f);

    private Vector3 previewBaseScale = Vector3.one;

    private void Start()
    {
        if (actionPreviewImage != null)
        {
            previewBaseScale = actionPreviewImage.rectTransform.localScale;
        }
    }

    private void Update()
    {
        if (smokeController == null)
        {
            smokeController = FindObjectOfType<SmokeExposureController>();
            if (smokeController == null)
            {
                ShowDisconnectedState();
                return;
            }
        }

        float d = smokeController.CurrentDensity;
        float head = smokeController.HeadDensity;
        float chest = smokeController.ChestDensity;

        float switchThreshold = actionSwitchThreshold >= 0f
            ? actionSwitchThreshold
            : smokeController.crouchThreshold;

        bool useCrouchRunPreview = d >= switchThreshold;

        string badge;
        string tip;

        Color cardColor;
        Color accentColor;
        Color fillColor;

        if (d >= smokeController.crouchRunThreshold)
        {
            badge = "DANGER";
            tip = "Suggestion: Crouch and run";
            cardColor = dangerCardColor;
            accentColor = dangerAccentColor;
            fillColor = dangerFillColor;
        }
        else if (d >= smokeController.crouchThreshold)
        {
            badge = "WARNING";
            tip = "Suggestion: Keep crouching";
            cardColor = warningCardColor;
            accentColor = warningAccentColor;
            fillColor = warningFillColor;
        }
        else if (d > 0.15f)
        {
            badge = "MILD SMOKE";
            tip = "Suggestion: Lower your head";
            cardColor = safeCardColor;
            accentColor = safeAccentColor;
            fillColor = safeFillColor;
        }
        else
        {
            badge = "SAFE";
            tip = "Suggestion: Move normally";
            cardColor = safeCardColor;
            accentColor = safeAccentColor;
            fillColor = safeFillColor;
        }

        UpdateTexts(d, head, chest, badge, tip);
        UpdateColors(d, fillColor, cardColor, accentColor);
        UpdateActionPreview(useCrouchRunPreview, d);
    }

    private void UpdateTexts(float d, float head, float chest, string badge, string tip)
    {
        if (percentText != null)
            percentText.text = $"{Mathf.RoundToInt(d * 100f)}%";

        if (badgeText != null)
            badgeText.text = badge;

        if (tipText != null)
            tipText.text = tip;

        if (detailText != null)
            detailText.text = $"Head {Mathf.RoundToInt(head * 100f)}%    Chest {Mathf.RoundToInt(chest * 100f)}%";

        if (footerText != null && smokeController != null)
        {
            footerText.text =
                $"Crouch >= {Mathf.RoundToInt(smokeController.crouchThreshold * 100f)}%    " +
                $"Crouch Run >= {Mathf.RoundToInt(smokeController.crouchRunThreshold * 100f)}%";
        }
    }

    private void UpdateColors(float d, Color fillColor, Color cardColor, Color accentColor)
    {
        if (cardBackground != null)
            cardBackground.color = cardColor;

        if (accentBar != null)
            accentBar.color = accentColor;

        if (statusBadgeBackground != null)
            statusBadgeBackground.color = accentColor;

        if (progressFill != null)
        {
            progressFill.color = fillColor;
            progressFill.fillAmount = d;
        }

        if (progressGlow != null)
        {
            Color glow = fillColor;

            float pulse = Mathf.Lerp(
                glowMinAlpha,
                glowMaxAlpha,
                0.5f + 0.5f * Mathf.Sin(Time.time * glowPulseSpeed)
            );

            if (smokeController != null && d < smokeController.crouchThreshold)
                pulse *= 0.5f;

            glow.a = pulse;
            progressGlow.color = glow;
            progressGlow.fillAmount = d;
        }

        // ===== 70% 以上开始红色闪烁 =====
        if (d >= redFlashThreshold)
        {
            float t = 0.5f + 0.5f * Mathf.Sin(Time.time * redFlashSpeed);
            float flash = Mathf.Lerp(0f, redFlashStrength, t);

            if (cardBackground != null)
                cardBackground.color = Color.Lerp(cardBackground.color, flashRed, flash * 0.45f);

            if (accentBar != null)
                accentBar.color = Color.Lerp(accentBar.color, flashRed, flash * 0.85f);

            if (statusBadgeBackground != null)
                statusBadgeBackground.color = Color.Lerp(statusBadgeBackground.color, flashRed, flash);

            if (progressGlow != null)
            {
                Color dangerGlow = flashRed;
                dangerGlow.a = Mathf.Lerp(0.08f, 0.35f, t);
                progressGlow.color = Color.Lerp(progressGlow.color, dangerGlow, flash * 0.9f);
            }
        }
    }

    private void UpdateActionPreview(bool useCrouchRunPreview, float density)
    {
        if (actionPreviewImage == null)
            return;

        if (useCrouchRunPreview)
        {
            if (crouchRunSprite != null)
                actionPreviewImage.sprite = crouchRunSprite;

            if (actionLabelText != null)
                actionLabelText.text = "Action: Crouch Run";
        }
        else
        {
            if (runSprite != null)
                actionPreviewImage.sprite = runSprite;

            if (actionLabelText != null)
                actionLabelText.text = "Action: Run";
        }

        if (!enablePreviewBreathing)
            return;

        float stateStrength = 1f;

        if (smokeController != null)
        {
            if (density >= smokeController.crouchRunThreshold)
                stateStrength = 1.15f;
            else if (density >= smokeController.crouchThreshold)
                stateStrength = 1.0f;
            else
                stateStrength = 0.85f;
        }

        float t = 0.5f + 0.5f * Mathf.Sin(Time.time * previewPulseSpeed * stateStrength);

        float scale = Mathf.Lerp(previewScaleMin, previewScaleMax, t);
        actionPreviewImage.rectTransform.localScale = previewBaseScale * scale;

        Color c = actionPreviewImage.color;
        c.a = Mathf.Lerp(previewAlphaMin, previewAlphaMax, t);
        actionPreviewImage.color = c;
    }

    private void ShowDisconnectedState()
    {
        if (percentText != null) percentText.text = "--";
        if (badgeText != null) badgeText.text = "NO SIGNAL";
        if (tipText != null) tipText.text = "Suggestion: Assign SmokeExposureController";
        if (detailText != null) detailText.text = "Head --    Chest --";
        if (footerText != null) footerText.text = "Check scene references";
        if (actionLabelText != null) actionLabelText.text = "Action: --";
    }
}