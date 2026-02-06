using PharloomsGlory.Modifiers;
using System.Collections;
using UnityEngine;

namespace PharloomsGlory.Components;

public class FishParticleManager : MonoBehaviour
{
    public GameObject original;
    public SceneModifier.FishParticleData data;
    public GameObject current;

    private float waitTime;

    private const float FG_PARTICLE_Z_LIMIT = -10f;
    private ParticleSystem.Particle[] particles;

    public void StartReloadLoop()
    {
        HandleFishParticle(current);
        StartCoroutine(WaitAndReload());
    }

    IEnumerator WaitAndReload()
    {
        waitTime = Random.Range(7f, 13f);
        yield return new WaitForSeconds(waitTime);
        ParticleSystem ps = current.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            Plugin.LogError($"Failed to get Particle System component from current fish particle object");
            yield break;
        }
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        GameObject clone = Instantiate(original, transform.position, transform.rotation);
        clone.SetActive(true);
        clone.transform.parent = transform;
        HandleFishParticle(clone);
        while (ps.IsAlive())
            yield return null;
        Destroy(current);
        current = clone;
        yield return WaitAndReload();
    }

    private void HandleFishParticle(GameObject go)
    {
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        if (ps == null)
            return;
        ParticleSystem.MainModule main = ps.main;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        ParticleSystem.LimitVelocityOverLifetimeModule lvol = ps.limitVelocityOverLifetime;
        lvol.limitX = data.limit;
        ParticleSystem.ForceOverLifetimeModule fol = ps.forceOverLifetime;
        switch (fol.x.mode)
        {
            case ParticleSystemCurveMode.Constant:
                fol.x = data.minCurveMax;
                break;
            case ParticleSystemCurveMode.TwoConstants:
                fol.x = new ParticleSystem.MinMaxCurve(data.minCurveMax, data.maxCurveMax);
                break;
            case ParticleSystemCurveMode.Curve:
                {
                    AnimationCurve curve = new AnimationCurve();
                    curve.AddKey(0f, data.maxCurveMin);
                    curve.AddKey(1f, data.maxCurveMax);
                    fol.x = new ParticleSystem.MinMaxCurve(fol.xMultiplier, curve);
                }
                break;
            case ParticleSystemCurveMode.TwoCurves:
                {
                    AnimationCurve minCurve = new AnimationCurve();
                    minCurve.AddKey(0f, data.minCurveMin);
                    minCurve.AddKey(1f, data.minCurveMax);
                    AnimationCurve maxCurve = new AnimationCurve();
                    maxCurve.AddKey(0f, data.maxCurveMin);
                    maxCurve.AddKey(1f, data.maxCurveMax);
                    fol.x = new ParticleSystem.MinMaxCurve(fol.xMultiplier, minCurve, maxCurve);
                    break;
                }
        }
        {
            AnimationCurve minCurve = new AnimationCurve();
            minCurve.AddKey(0f, -0.7f);
            minCurve.AddKey(1f, 0f);
            AnimationCurve maxCurve = new AnimationCurve();
            maxCurve.AddKey(0f, 0f);
            maxCurve.AddKey(1f, 0.7f);
            fol.y = new ParticleSystem.MinMaxCurve(1, minCurve, maxCurve);
        }
    }

    private void Update()
    {
        if (data.type == SceneModifier.FishParticleType.FG)
        {
            foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>())
            {
                SetupParticlesArray(ps.main.maxParticles);
                int particleCount = ps.GetParticles(particles);
                Vector3[] particleLocalPositions = new Vector3[particleCount];
                for (int i = 0; i < particleCount; i++)
                    particleLocalPositions[i] = particles[i].position;
                Vector3[] particleGlobalPositions = new Vector3[particleCount];
                ps.transform.TransformPoints(particleLocalPositions, particleGlobalPositions);
                for (int i = 0; i < particleCount; i++)
                {
                    Vector3 position = particleGlobalPositions[i];
                    if (position.z > FG_PARTICLE_Z_LIMIT)
                        particles[i].position = ps.transform.InverseTransformPoint(position.x, position.y, FG_PARTICLE_Z_LIMIT);
                }
                ps.SetParticles(particles, particleCount);
            }
        }
    }

    private void SetupParticlesArray(int maxParticles)
    {
        if (particles == null || particles.Length < maxParticles)
            particles = new ParticleSystem.Particle[maxParticles];
    }
}
