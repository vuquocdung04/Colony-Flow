using UnityEngine;

public static class ColonyFx
{
    public static void Play(ParticleSystem fx, bool detach = false)
    {
        if (fx == null) return;

        if (detach)
        {
            fx.transform.SetParent(null, true);
            Object.Destroy(fx.gameObject, Lifetime(fx));
        }

        fx.Play(true);
    }

    static float Lifetime(ParticleSystem fx)
    {
        ParticleSystem.MainModule main = fx.main;
        return main.duration + main.startLifetime.constantMax;
    }
}
