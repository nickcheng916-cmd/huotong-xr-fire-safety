using UnityEngine;

public class SmokeParticleDriver : MonoBehaviour
{
    public ParticleSystem smokeParticle;
    public SmokeZone smokeZone;

    [Header("映射模式")]
    public bool useParticleCount = true;

    [Header("发射率映射")]
    public float minRate = 10f;
    public float maxRate = 95f;

    [Header("粒子数量映射")]
    public int minParticleCount = 10;
    public int maxParticleCount = 120;

    [Header("增长平滑")]
    public float riseSpeed = 0.12f;   // 越小增长越慢
    public float fallSpeed = 0.08f;   // 越小下降越慢

    [Header("调试")]
    [Range(0f, 1f)] public float targetProgress;
    [Range(0f, 1f)] public float currentProgress;

    private void Reset()
    {
        if (smokeParticle == null)
            smokeParticle = GetComponent<ParticleSystem>();

        if (smokeZone == null)
            smokeZone = GetComponent<SmokeZone>();
    }

    private void Update()
    {
        if (smokeParticle == null || smokeZone == null) return;

        float t = 0f;

        if (useParticleCount)
        {
            int count = smokeParticle.particleCount;
            t = Mathf.InverseLerp(minParticleCount, maxParticleCount, count);
        }
        else
        {
            var emission = smokeParticle.emission;

            if (emission.rateOverTime.mode == ParticleSystemCurveMode.Constant)
            {
                float rate = emission.rateOverTime.constant;
                t = Mathf.InverseLerp(minRate, maxRate, rate);
            }
            else
            {
                int count = smokeParticle.particleCount;
                t = Mathf.InverseLerp(minParticleCount, maxParticleCount, count);
            }
        }

        t = Mathf.Clamp01(t);
        targetProgress = t;

        float speed = targetProgress > currentProgress ? riseSpeed : fallSpeed;
        currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, speed * Time.deltaTime);

        smokeZone.SetSmokeProgress(currentProgress);
    }
}