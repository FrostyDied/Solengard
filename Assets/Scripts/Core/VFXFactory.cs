using UnityEngine;

public static class VFXFactory
{
    // ── 1. EXPLOSÃO DE MORTE DO INIMIGO ──────────────────────────────────────────
    public static void SpawnDeathExplosion(Vector3 pos, Color color)
    {
        var go = new GameObject("DeathVFX");
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        Object.Destroy(go, 2f);

        var main = ps.main;
        main.duration        = 0.4f;
        main.loop            = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(2f, 6f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startColor      = new ParticleSystem.MinMaxGradient(color, Color.white);
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 0.3f;

        var colorLife = ps.colorOverLifetime;
        colorLife.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorLife.color = grad;

        var sizeLife = ps.sizeOverLifetime;
        sizeLife.enabled = true;
        var curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        sizeLife.size = new ParticleSystem.MinMaxCurve(1f, curve);

        ps.Play();
    }

    // ── 2. AURA DE ATAQUE 360° ────────────────────────────────────────────────────
    public static void SpawnAttackAura(Vector3 pos, float radius)
    {
        var go = new GameObject("AttackAuraVFX");
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        Object.Destroy(go, 0.35f);

        var main = ps.main;
        main.duration        = 0.2f;
        main.loop            = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(radius * 0.8f, radius * 1.4f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.85f, 0.1f), new Color(1f, 0.5f, 0f));
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 0.2f;

        var colorLife = ps.colorOverLifetime;
        colorLife.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0f),
                    new GradientColorKey(new Color(1f, 0.3f, 0f),    1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorLife.color = grad;

        ps.Play();
    }

    // ── 3. HIT SPARK (inimigo tomou dano) ─────────────────────────────────────────
    public static void SpawnHitSpark(Vector3 pos)
    {
        var go = new GameObject("HitVFX");
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        Object.Destroy(go, 0.3f);

        var main = ps.main;
        main.duration        = 0.1f;
        main.loop            = false;
        main.startLifetime   = 0.2f;
        main.startSpeed      = new ParticleSystem.MinMaxCurve(1f, 4f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
        main.startColor      = new Color(1f, 1f, 0.8f);
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 0.1f;

        ps.Play();
    }

    // ── 4. COLETA DE XP (cristal sendo absorvido) ─────────────────────────────────
    public static void SpawnXPCollect(Vector3 pos)
    {
        var go = new GameObject("XPCollectVFX");
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        Object.Destroy(go, 0.5f);

        var main = ps.main;
        main.duration        = 0.2f;
        main.loop            = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.5f, 2f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(0.4f, 0.7f, 1f), Color.white);
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 0.15f;

        ps.Play();
    }
}
