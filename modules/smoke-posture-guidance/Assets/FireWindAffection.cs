using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class FireWindAffection : MonoBehaviour
{
    [Header("风效果设置")]
    [Tooltip("风对火焰的影响系数，1=正常，0=不受风影响")]
    public float windInfluenceMultiplier = 2f;
    [Tooltip("火焰本身向上的基础速度，保证火焰始终向上飘")]
    public float baseUpwardSpeed = 2f;

    private ParticleSystem fireParticle;
    private bool isInitialized = false; // 初始化完成标志，彻底避免提前执行报错

    void Start()
    {
        // 1. 强制获取粒子系统，确保实例有效
        fireParticle = GetComponent<ParticleSystem>();
        if (fireParticle == null)
        {
            Debug.LogError("找不到粒子系统组件！", this);
            enabled = false;
            return;
        }

        // 2. 强制初始化粒子配置，避免手动设置出错
        var mainModule = fireParticle.main;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
        mainModule.startSpeed = 0; // 关闭初始速度，完全由脚本控制

        var velModule = fireParticle.velocityOverLifetime;
        velModule.enabled = true;
        velModule.space = ParticleSystemSimulationSpace.World;
        velModule.x = 0;
        velModule.y = 0;
        velModule.z = 0;

        // 关闭所有干扰速度的模块
        var forceModule = fireParticle.forceOverLifetime;
        forceModule.enabled = false;
        var limitVelModule = fireParticle.limitVelocityOverLifetime;
        limitVelModule.enabled = false;

        // 3. 标记初始化完成，Update才会执行
        isInitialized = true;
        Debug.Log($"✅ 火焰脚本初始化完成：{gameObject.name}");
    }

    void Update()
    {
        // 【关键】未初始化完成、粒子系统无效、风管理器不存在，直接跳过，彻底避免报错
        if (!isInitialized || fireParticle == null || WindManager.Instance == null)
        {
            return;
        }

        // 读取全局风力参数
        float windStrength = WindManager.CurrentWindStrength * windInfluenceMultiplier;
        Vector2 windDir = WindManager.CurrentWindDir;

        // 【关键】每次都从有效粒子实例上获取模块，绝对不会出现无效实例报错
        var velModule = fireParticle.velocityOverLifetime;
        // 给粒子赋值最终速度
        velModule.x = windDir.x * windStrength;
        velModule.y = baseUpwardSpeed + windDir.y * windStrength;
        velModule.z = 0;

        // 调试日志，确认风力正常传递
        if (windStrength > 0.1f)
        {
            Debug.Log($"🌬️  火焰收到风力：{windStrength:F2}，风向：{windDir}");
        }
    }
}