using UnityEngine;
using System.Collections;

public class Flammable : MonoBehaviour
{
    [Header("燃烧核心设置")]
    public ParticleSystem fireEffect;     // 火焰粒子预设（支持多子粒子系统）
    public Material burnedMaterial;       // 燃烧后的黑色材质
    public float totalLifeTime = 15f;     // 点燃后到销毁的总时长（秒）
    public float burnSpreadDuration = 10f;// 火势蔓延+材质烧焦的总时长（秒）

    [Header("可燃物互相点燃设置")]
    public float igniteRadius = 3.5f;       // 自身着火后，能点燃周围物体的半径
    public float igniteCheckInterval = 0.3f; // 周围可燃物检测间隔

    private bool isOnFire = false;
    private Renderer objRenderer;
    private Material originalMaterial;
    private Material instanceMaterial; // 实例化材质，避免修改原材质资源
    private ParticleSystem spawnedFire;
    private float burnProgress;

    void Awake()
    {
        // 提前获取渲染组件，做容错判断
        objRenderer = GetComponent<Renderer>();
        if (objRenderer == null)
        {
            Debug.LogError($"物体{gameObject.name}缺少Renderer组件！", this);
            enabled = false;
            return;
        }

        // 保存原始材质，实例化避免污染项目里的原材质资源
        originalMaterial = objRenderer.sharedMaterial;
        instanceMaterial = new Material(originalMaterial);
        objRenderer.material = instanceMaterial;
    }

    // 被火焰粒子碰撞触发点燃
    void OnParticleCollision(GameObject other)
    {
        if (other.CompareTag("Fire") && !isOnFire)
        {
            Ignite();
        }
    }

    // 对外公开的点燃方法，方便其他可燃物调用（核心修复版）
    public void Ignite()
    {
        if (isOnFire || burnedMaterial == null || fireEffect == null) return; // 全容错判断
        isOnFire = true;

        // ==============================================
        // 【核心修复1】生成火焰，强制绑定父物体，彻底杜绝位移
        // ==============================================
        // 直接以当前物体为父物体生成，位置自动归零，无需手动设置
        spawnedFire = Instantiate(fireEffect, transform);
        spawnedFire.transform.localPosition = Vector3.zero;
        spawnedFire.transform.localRotation = Quaternion.identity;
        spawnedFire.transform.localScale = Vector3.one;

        // ==============================================
        // 【核心修复2】递归处理所有子粒子系统，统一设置Local空间
        // ==============================================
        foreach (ParticleSystem ps in spawnedFire.GetComponentsInChildren<ParticleSystem>())
        {
            var main = ps.main;
            // 强制所有粒子使用局部空间，彻底解决多粒子拖尾
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            // 关闭重力，避免粒子下坠
            main.gravityModifier = 0f;
            // 粒子跟随父物体层级缩放，避免错位
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            // 发射器速度模式设为刚体，完美适配移动物体
            main.emitterVelocityMode = ParticleSystemEmitterVelocityMode.Rigidbody;
            // 清除旧粒子，重置状态，避免世界空间残留
            ps.Clear();
            ps.Stop();
        }

        // ==============================================
        // 【核心修复3】禁用物理，彻底杜绝火焰乱飞
        // ==============================================
        if (spawnedFire.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // ==============================================
        // 【核心修复4】初始化火焰发射，从0开始蔓延
        // ==============================================
        // 播放所有子粒子系统（true参数强制播放所有子节点）
        spawnedFire.Play(true);
        // 根节点初始发射率为0，随燃烧进度增加
        var rootEmission = spawnedFire.emission;
        rootEmission.rateOverTime = 0f;

        // 开启燃烧流程协程
        StartCoroutine(BurnProcessCoroutine());
        // 开启周围可燃物检测协程（实现互相点燃）
        StartCoroutine(AutoIgniteNearbyCoroutine());
    }

    // 核心：火势均匀蔓延+材质渐变协程
    IEnumerator BurnProcessCoroutine()
    {
        burnProgress = 0;
        float timer = 0;

        // --- 阶段1：火势蔓延+材质匀速烧焦 ---
        while (timer < burnSpreadDuration && isOnFire && spawnedFire != null)
        {
            timer += Time.deltaTime;
            burnProgress = Mathf.Clamp01(timer / burnSpreadDuration);

            // 1. 火焰渐进变大：粒子数量随进度增加，模拟火势蔓延
            var emission = spawnedFire.emission;
            // 50是粒子最大数量，可根据你的粒子效果自由调整
            emission.rateOverTime = Mathf.Lerp(0, 50f, burnProgress);

            // 2. 材质均匀变色：适配Standard (Specular setup) Shader，使用_Color属性
            Color originalColor = originalMaterial.GetColor("_Color");
            Color burnedColor = burnedMaterial.GetColor("_Color");
            instanceMaterial.SetColor("_Color", Color.Lerp(originalColor, burnedColor, burnProgress));

            yield return null; // 每帧更新，实现平滑过渡
        }

        // 确保最终完全变为烧焦材质
        instanceMaterial.CopyPropertiesFromMaterial(burnedMaterial);

        // --- 阶段2：等待剩余时间后销毁 ---
        float remainTime = totalLifeTime - burnSpreadDuration;
        if (remainTime > 0)
        {
            yield return new WaitForSeconds(remainTime);
        }

        // 销毁前先停止火焰，让粒子自然消散
        if (spawnedFire != null)
        {
            spawnedFire.Stop(withChildren: true);
            Destroy(spawnedFire.gameObject, 2f);
        }

        // 销毁物体
        Destroy(gameObject);
    }

    // 核心：可燃物互相点燃协程
    IEnumerator AutoIgniteNearbyCoroutine()
    {
        // 只要物体还在燃烧，就持续检测周围
        while (isOnFire && gameObject != null)
        {
            // 以自身为中心，检测半径内的所有碰撞体
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, igniteRadius);

            foreach (var hit in hitColliders)
            {
                // 找到其他带Flammable组件的可燃物，尝试点燃
                if (hit.TryGetComponent(out Flammable flammable) && flammable != this)
                {
                    flammable.Ignite();
                }
            }

            // 间隔指定时间后再次检测，避免性能浪费
            yield return new WaitForSeconds(igniteCheckInterval);
        }
    }

    // ==============================================
    // 【核心修复5】LateUpdate强制同步，彻底杜绝一帧滞后
    // ==============================================
    void LateUpdate()
    {
        // 只要火焰存在且正在燃烧，就强制同步位置
        if (spawnedFire != null && isOnFire && gameObject != null)
        {
            // 强制火焰根节点位置=物体位置，100%跟随，不会错位
            spawnedFire.transform.position = transform.position;
            spawnedFire.transform.rotation = transform.rotation;
            spawnedFire.transform.localScale = Vector3.one;
        }
    }

    // 编辑器里可视化点燃半径，方便调试
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, igniteRadius);
    }
}