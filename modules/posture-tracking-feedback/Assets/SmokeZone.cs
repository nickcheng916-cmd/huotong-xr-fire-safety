using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class SmokeZone : MonoBehaviour
{
    public static readonly List<SmokeZone> ActiveZones = new List<SmokeZone>();

    [Header("最终浓度范围")]
    [Range(0f, 1f)] public float minDensity = 0.10f;   // 10%
    [Range(0f, 1f)] public float maxDensity = 0.95f;   // 95%

    [Header("粒子驱动进度 0~1")]
    [Range(0f, 1f)] public float smokeProgress = 0f;

    [Header("位置影响强度")]
    [Range(0f, 1f)] public float positionInfluence = 0.25f;

    [Header("底部浓度系数（蹲下仍然更安全）")]
    [Range(0f, 1f)] public float bottomDensityFactor = 0.35f;

    [Header("轻微波动")]
    [Range(0f, 0.05f)] public float pulseAmount = 0.01f;
    [Range(0.1f, 5f)] public float pulseSpeed = 1.0f;

    private BoxCollider boxCol;

    private void OnEnable()
    {
        boxCol = GetComponent<BoxCollider>();
        boxCol.isTrigger = true;

        if (!ActiveZones.Contains(this))
            ActiveZones.Add(this);
    }

    private void OnDisable()
    {
        ActiveZones.Remove(this);
    }

    public void SetSmokeProgress(float progress01)
    {
        smokeProgress = Mathf.Clamp01(progress01);
    }

    public float GetCurrentBaseDensity()
    {
        return Mathf.Lerp(minDensity, maxDensity, smokeProgress);
    }

    public float SampleDensity(Vector3 worldPos)
    {
        if (boxCol == null) return 0f;

        Vector3 local = transform.InverseTransformPoint(worldPos) - boxCol.center;
        Vector3 half = boxCol.size * 0.5f;

        float hx = Mathf.Max(half.x, 0.0001f);
        float hy = Mathf.Max(half.y, 0.0001f);
        float hz = Mathf.Max(half.z, 0.0001f);

        bool inside =
            Mathf.Abs(local.x) <= hx &&
            Mathf.Abs(local.y) <= hy &&
            Mathf.Abs(local.z) <= hz;

        if (!inside) return 0f;

        // 横向位置影响
        float nx = 1f - Mathf.Clamp01(Mathf.Abs(local.x) / hx);
        float nz = 1f - Mathf.Clamp01(Mathf.Abs(local.z) / hz);
        float horizontalFactor = Mathf.SmoothStep(0f, 1f, Mathf.Min(nx, nz));

        // 垂直位置影响：越靠上越危险
        float y01 = Mathf.InverseLerp(-hy, hy, local.y);
        float verticalFactor = Mathf.Lerp(bottomDensityFactor, 1f, Mathf.SmoothStep(0f, 1f, y01));

        // 总位置因子
        float positionalFactor = horizontalFactor * verticalFactor;

        // 减弱位置影响，让“烟越来越浓”成为主导
        positionalFactor = Mathf.Lerp(1f, positionalFactor, positionInfluence);

        float baseDensity = GetCurrentBaseDensity();
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

        float density = baseDensity * positionalFactor * pulse;
        return Mathf.Clamp01(density);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) return;

        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = new Color(0f, 0f, 0f, 0.18f);
        Gizmos.DrawCube(col.center, col.size);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(col.center, col.size);

        Gizmos.matrix = old;
    }
#endif
}