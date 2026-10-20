using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class SmokeCeilingTrigger : MonoBehaviour
{
    public ParticleSystem ceilingSmokePrefab;

    [Header("控制生成数量，避免太多")]
    public int maxSpawnPerFrame = 6;
    [Range(0f, 1f)]
    public float spawnChance = 0.35f;

    [Header("生成位置微扰，避免太整齐")]
    public Vector3 randomOffset = new Vector3(0.2f, 0.05f, 0.2f);

    private ParticleSystem ps;
    private readonly List<ParticleSystem.Particle> enterParticles = new();

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    private void OnParticleTrigger()
    {
        int count = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enterParticles);

        if (count <= 0 || ceilingSmokePrefab == null)
            return;

        int spawned = 0;

        for (int i = 0; i < count; i++)
        {
            if (spawned >= maxSpawnPerFrame)
                break;

            if (Random.value > spawnChance)
                continue;

            Vector3 pos = enterParticles[i].position;

            pos += new Vector3(
                Random.Range(-randomOffset.x, randomOffset.x),
                Random.Range(-randomOffset.y, randomOffset.y),
                Random.Range(-randomOffset.z, randomOffset.z)
            );

            ParticleSystem smoke = Instantiate(ceilingSmokePrefab, pos, Quaternion.identity);
            smoke.Play();

            Destroy(smoke.gameObject, 12f);
            spawned++;
        }
    }
}