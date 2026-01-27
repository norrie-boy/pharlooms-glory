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
}
