using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(AudioSource))]
public class FlameParticleAudio : MonoBehaviour
{
    [Header("References")]
    public ParticleSystem targetParticleSystem;
    public AudioSource loopAudioSource;

    [Header("Audio Clips")]
    public AudioClip flameLoopClip;
    public AudioClip igniteClip;
    public AudioClip extinguishClip;

    [Header("Volume")]
    [Range(0f, 1f)] public float minVolume = 0.08f;
    [Range(0f, 1f)] public float maxVolume = 0.75f;

    [Header("Pitch")]
    [Range(0.5f, 2f)] public float minPitch = 0.9f;
    [Range(0.5f, 2f)] public float maxPitch = 1.15f;

    [Header("Fade")]
    public float fadeInSpeed = 3f;
    public float fadeOutSpeed = 2f;

    [Header("Intensity Mapping")]
    [Tooltip("用于估算火焰强度的最大发射率，实际项目里按你的粒子设置填写")]
    public float maxEmissionRate = 50f;

    [Tooltip("粒子数量达到这个值时，认为火焰强度接近满值")]
    public float maxAliveParticles = 200f;

    [Header("3D Audio")]
    [Range(0f, 1f)] public float spatialBlend = 1f;
    public float minDistance = 1.5f;
    public float maxDistance = 15f;

    [Header("Optional Randomness")]
    [Tooltip("给火焰一点轻微随机波动，让声音更自然")]
    public bool useRandomFluctuation = true;
    public float fluctuationSpeed = 1.5f;
    public float fluctuationAmount = 0.06f;

    private bool wasPlayingLastFrame = false;
    private float currentTargetVolume = 0f;

    void Reset()
    {
        targetParticleSystem = GetComponent<ParticleSystem>();
        loopAudioSource = GetComponent<AudioSource>();
        SetupAudioSource();
    }

    void Awake()
    {
        if (targetParticleSystem == null)
            targetParticleSystem = GetComponent<ParticleSystem>();

        if (loopAudioSource == null)
            loopAudioSource = GetComponent<AudioSource>();

        SetupAudioSource();

        if (flameLoopClip != null)
            loopAudioSource.clip = flameLoopClip;
    }

    void SetupAudioSource()
    {
        if (loopAudioSource == null) return;

        loopAudioSource.playOnAwake = false;
        loopAudioSource.loop = true;
        loopAudioSource.spatialBlend = spatialBlend;
        loopAudioSource.minDistance = minDistance;
        loopAudioSource.maxDistance = maxDistance;
        loopAudioSource.dopplerLevel = 0f;
        loopAudioSource.volume = 0f;
    }

    void Update()
    {
        if (targetParticleSystem == null || loopAudioSource == null)
            return;

        bool isPlaying = targetParticleSystem.isPlaying || targetParticleSystem.particleCount > 0;

        if (isPlaying && !wasPlayingLastFrame)
        {
            OnFlameStarted();
        }
        else if (!isPlaying && wasPlayingLastFrame)
        {
            OnFlameStopped();
        }

        float intensity = CalculateFlameIntensity();
        UpdateLoopSound(isPlaying, intensity);

        wasPlayingLastFrame = isPlaying;
    }

    void OnFlameStarted()
    {
        if (igniteClip != null)
        {
            loopAudioSource.PlayOneShot(igniteClip, 1f);
        }

        if (flameLoopClip != null && loopAudioSource.clip != flameLoopClip)
        {
            loopAudioSource.clip = flameLoopClip;
        }

        if (loopAudioSource.clip != null && !loopAudioSource.isPlaying)
        {
            loopAudioSource.Play();
        }
    }

    void OnFlameStopped()
    {
        if (extinguishClip != null)
        {
            AudioSource.PlayClipAtPoint(extinguishClip, transform.position, 1f);
        }
    }

    float CalculateFlameIntensity()
    {
        // 1) 根据粒子存活数估算强度
        float particleFactor = 0f;
        if (maxAliveParticles > 0f)
        {
            particleFactor = Mathf.Clamp01(targetParticleSystem.particleCount / maxAliveParticles);
        }

        // 2) 根据发射率估算强度
        float emissionFactor = 0f;
        var emission = targetParticleSystem.emission;

        // rateOverTime 是 MinMaxCurve，这里取 constant 作为常见情况的估算
        float emissionRate = emission.rateOverTime.constant;

        if (maxEmissionRate > 0f)
        {
            emissionFactor = Mathf.Clamp01(emissionRate / maxEmissionRate);
        }

        // 3) 混合两个指标
        float baseIntensity = Mathf.Max(particleFactor, emissionFactor);

        // 4) 加一点自然随机波动
        if (useRandomFluctuation)
        {
            float noise = Mathf.PerlinNoise(Time.time * fluctuationSpeed, 0f);
            float fluctuation = (noise - 0.5f) * 2f * fluctuationAmount;
            baseIntensity = Mathf.Clamp01(baseIntensity + fluctuation);
        }

        return baseIntensity;
    }

    void UpdateLoopSound(bool flameExists, float intensity)
    {
        if (!flameExists)
        {
            currentTargetVolume = 0f;
        }
        else
        {
            currentTargetVolume = Mathf.Lerp(minVolume, maxVolume, intensity);

            float targetPitch = Mathf.Lerp(minPitch, maxPitch, intensity);
            loopAudioSource.pitch = Mathf.Lerp(loopAudioSource.pitch, targetPitch, Time.deltaTime * 5f);

            if (!loopAudioSource.isPlaying && loopAudioSource.clip != null)
            {
                loopAudioSource.Play();
            }
        }

        float speed = flameExists ? fadeInSpeed : fadeOutSpeed;
        loopAudioSource.volume = Mathf.Lerp(loopAudioSource.volume, currentTargetVolume, Time.deltaTime * speed);

        // 音量很小并且火已结束时，彻底停止
        if (!flameExists && loopAudioSource.volume < 0.01f && loopAudioSource.isPlaying)
        {
            loopAudioSource.Stop();
        }
    }

    // 对外接口：手动点燃
    public void StartFlame()
    {
        if (targetParticleSystem != null)
            targetParticleSystem.Play();
    }

    // 对外接口：手动熄灭
    public void StopFlame()
    {
        if (targetParticleSystem != null)
            targetParticleSystem.Stop();
    }

    // 对外接口：修改强度（如果你的火焰会随玩法变化）
    public void SetEmissionRate(float rate)
    {
        if (targetParticleSystem == null) return;

        var emission = targetParticleSystem.emission;
        emission.rateOverTime = rate;
    }
}
