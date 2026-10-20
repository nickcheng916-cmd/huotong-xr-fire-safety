using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleRepulsion : MonoBehaviour
{
    public Transform repulsionSource;        // 交互物体（如手、武器）
    public float repulsionRadius = 2f;       // 斥力作用半径
    public float repulsionStrength = 10f;    // 斥力强度
    public float damping = 0.95f;            // 速度阻尼，让粒子逐渐恢复

    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void LateUpdate()
    {
        if (repulsionSource == null) return;

        // 获取当前所有粒子
        int maxParticles = ps.main.maxParticles;
        if (particles == null || particles.Length < maxParticles)
            particles = new ParticleSystem.Particle[maxParticles];
        int particleCount = ps.GetParticles(particles);

        Vector3 sourcePos = repulsionSource.position;

        for (int i = 0; i < particleCount; i++)
        {
            Vector3 dirToParticle = particles[i].position - sourcePos;
            float dist = dirToParticle.magnitude;

            if (dist < repulsionRadius && dist > 0.01f)
            {
                // 斥力方向：从物体指向粒子
                Vector3 forceDir = dirToParticle.normalized;
                // 力的大小随距离减小而增大（距离越近力越大）
                float forceMagnitude = repulsionStrength * (1 - dist / repulsionRadius);
                Vector3 force = forceDir * forceMagnitude;

                // 应用力（改变速度）
                particles[i].velocity += force * Time.deltaTime;

                // 可选：稍微阻尼，避免粒子飞出太远
                particles[i].velocity *= damping;
            }
        }

        // 将修改后的粒子写回系统
        ps.SetParticles(particles, particleCount);
    }
}