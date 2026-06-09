using UnityEngine;
using System.Collections;

public static class ProceduralVFX
{
    static Material _mat;
    public static Material GetMat()
    {
        if (_mat == null)
            _mat = new Material(Shader.Find("Sprites/Default"));
        return _mat;
    }
    public static Material GetPublicMat() => GetMat();

    // ═══════════════════════════════════════════
    // TIPO 1 — WHIP (chicote de energia)
    // Usado por: Guerreiro
    // Linha senoidal que vai e volta na direção do ataque
    // ═══════════════════════════════════════════
    public static IEnumerator Whip(Transform originTransform, Vector2 direction,
        float length, float duration, Color color, float width = 0.06f,
        int segments = 20, float amplitude = 0.4f)
    {
        var go = new GameObject("VFX_Whip");
        var lr = go.AddComponent<LineRenderer>();
        lr.material = GetMat();
        lr.startWidth = width;
        lr.endWidth = width * 0.3f;
        lr.positionCount = segments;
        lr.sortingOrder = 300;

        var grad = new Gradient();
        grad.SetKeys(
            new[]{ new GradientColorKey(color, 0f),
                   new GradientColorKey(color * 0.5f, 1f) },
            new[]{ new GradientAlphaKey(1f, 0f),
                   new GradientAlphaKey(0f, 1f) }
        );
        lr.colorGradient = grad;

        Vector3 perp = new Vector3(-direction.y, direction.x, 0);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            Vector3 origin  = originTransform.position;
            float   flicker = amplitude + Random.Range(-0.05f, 0.05f);
            float   t       = elapsed / duration;
            float   reach   = t < 0.5f ? t * 2f : (1f - t) * 2f;

            for (int i = 0; i < segments; i++)
            {
                float s = (float)i / (segments - 1);
                float wave = Mathf.Sin(s * Mathf.PI * 3f + elapsed * 8f)
                             * flicker * (1f - s);
                Vector3 pos = origin
                    + (Vector3)(direction * s * length * reach)
                    + perp * wave;
                lr.SetPosition(i, pos);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        Object.Destroy(go);
    }

    // ═══════════════════════════════════════════
    // TIPO 2 — ENERGY BOLT (tiro de energia)
    // Usado por: Mago, Necromante
    // Projétil com núcleo brilhante + trail
    // ═══════════════════════════════════════════
    public static IEnumerator EnergyBolt(Vector3 origin, Vector2 direction,
        float speed, float range, Color coreColor, Color trailColor,
        float size = 0.15f, System.Action<Vector3> onHit = null)
    {
        var go = new GameObject("VFX_EnergyBolt");
        go.transform.position = origin;

        var core = CreateCircle(go, size, coreColor, 300);

        var tr = go.AddComponent<TrailRenderer>();
        tr.material = GetMat();
        tr.time = 0.4f;
        tr.startWidth = size * 1.2f;
        tr.endWidth = 0f;
        tr.startColor = trailColor;
        tr.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
        tr.sortingOrder = 299;

        float traveled    = 0f;
        float currentSize = size * 0.2f;
        var boltFilter = new ContactFilter2D { useTriggers = true, useLayerMask = true };
        boltFilter.SetLayerMask(LayerMask.GetMask("Enemy"));
        var boltHits = new System.Collections.Generic.List<Collider2D>();

        while (traveled < range)
        {
            float step = speed * Time.deltaTime;
            go.transform.position += (Vector3)(direction * step);
            traveled += step;

            float growT = Mathf.Clamp01(traveled / (range * 0.6f));
            currentSize = Mathf.Lerp(size * 0.2f, size, growT);
            core.transform.localScale = Vector3.one * currentSize;
            tr.startWidth = currentSize * 0.8f;

            // Detecção de colisão em tempo real
            Physics2D.OverlapCircle(go.transform.position, currentSize * 0.8f, boltFilter, boltHits);
            if (boltHits.Count > 0)
            {
                var eb = boltHits[0].GetComponent<EnemyBase>()
                      ?? boltHits[0].GetComponentInParent<EnemyBase>();
                if (eb != null && !eb.IsDead)
                {
                    Vector3 hitPos = go.transform.position;
                    Object.Destroy(go);
                    if (onHit != null) onHit(hitPos);
                    yield break;
                }
            }

            yield return null;
        }

        Object.Destroy(go);
    }

    // ═══════════════════════════════════════════
    // TIPO 3 — SLASH ARC (arco de corte)
    // Usado por: Paladino, Assassino
    // Arco de LineRenderer que se expande de 0 ao ângulo definido
    // ═══════════════════════════════════════════
    public static IEnumerator SlashArc(Vector3 origin, Vector2 direction,
        float arcDegrees, float radius, float duration,
        Color color, float width = 0.2f, int segments = 30)
    {
        var go = new GameObject("VFX_SlashArc");
        var lr = go.AddComponent<LineRenderer>();
        lr.material = GetMat();
        lr.startWidth = width;
        lr.endWidth = width * 0.1f;
        lr.positionCount = segments;
        lr.sortingOrder = 300;

        var grad = new Gradient();
        grad.SetKeys(
            new[]{ new GradientColorKey(Color.white, 0f),
                   new GradientColorKey(color, 0.3f),
                   new GradientColorKey(color * 0.3f, 1f) },
            new[]{ new GradientAlphaKey(1f, 0f),
                   new GradientAlphaKey(0.8f, 0.5f),
                   new GradientAlphaKey(0f, 1f) }
        );
        lr.colorGradient = grad;

        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float halfArc = arcDegrees * 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float currentRadius = radius * (t < 0.4f ? t / 0.4f : 1f);
            float alpha = t < 0.4f ? 1f : 1f - (t - 0.4f) / 0.6f;

            for (int i = 0; i < segments; i++)
            {
                float s = (float)i / (segments - 1);
                float angle = (baseAngle - halfArc + arcDegrees * s)
                              * Mathf.Deg2Rad;
                Vector3 pos = origin + new Vector3(
                    Mathf.Cos(angle) * currentRadius,
                    Mathf.Sin(angle) * currentRadius, 0);
                lr.SetPosition(i, pos);
            }

            // Fade out via alpha scaling no gradient
            var g = lr.colorGradient;
            var ak = g.alphaKeys;
            for (int i = 0; i < ak.Length; i++)
                ak[i] = new GradientAlphaKey(ak[i].alpha * alpha, ak[i].time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Object.Destroy(go);
    }

    // ═══════════════════════════════════════════
    // TIPO 4b — STAR PROJECTILE (estrela ninja)
    // Usado por: Assassino
    // Estrela geométrica de 4 pontas que rotaciona voando
    // ═══════════════════════════════════════════
    public static IEnumerator StarProjectile(Vector3 origin, Vector2 direction,
        float speed, float range, Color color,
        System.Action<EnemyBase> onHit = null)
    {
        var go = new GameObject("VFX_Star");
        go.transform.position = origin;

        // Estrela de 4 pontas usando 2 LineRenderers cruzados
        float outerR = 0.18f;
        float innerR = 0.07f;
        int   pts    = 9; // 4 pontas + centro

        var lr1 = go.AddComponent<LineRenderer>();
        lr1.material    = GetMat();
        lr1.loop        = true;
        lr1.positionCount = pts;
        lr1.startWidth  = 0.04f;
        lr1.endWidth    = 0.04f;
        lr1.startColor  = lr1.endColor = color;
        lr1.sortingOrder = 301;

        // Trail
        var tr = go.AddComponent<TrailRenderer>();
        tr.material    = GetMat();
        tr.time        = 0.12f;
        tr.startWidth  = 0.06f;
        tr.endWidth    = 0f;
        tr.startColor  = color;
        tr.endColor    = new Color(color.r, color.g, color.b, 0f);
        tr.sortingOrder = 300;

        float traveled = 0f;
        float rotation = 0f;

        var hitFilter = new ContactFilter2D { useTriggers = true, useLayerMask = true };
        hitFilter.SetLayerMask(LayerMask.GetMask("Enemy"));
        var hitList = new System.Collections.Generic.List<Collider2D>();

        while (traveled < range)
        {
            float step = speed * Time.deltaTime;
            go.transform.position += (Vector3)(direction * step);
            traveled  += step;
            rotation  += 360f * Time.deltaTime * 4f; // rotação rápida

            // Redesenhar a estrela rotacionada
            for (int i = 0; i < pts; i++)
            {
                float baseAngle = (rotation + i * (360f / 4f)) * Mathf.Deg2Rad;
                float r = (i % 2 == 0) ? outerR : innerR;
                lr1.SetPosition(i, go.transform.position + new Vector3(
                    Mathf.Cos(baseAngle) * r,
                    Mathf.Sin(baseAngle) * r, 0));
            }

            // Detecção de colisão
            Physics2D.OverlapCircle(go.transform.position, outerR * 1.2f, hitFilter, hitList);
            if (hitList.Count > 0)
            {
                var eb = hitList[0].GetComponent<EnemyBase>()
                      ?? hitList[0].GetComponentInParent<EnemyBase>();
                if (eb != null && !eb.IsDead)
                {
                    Object.Destroy(go);
                    if (onHit != null) onHit(eb);
                    yield break;
                }
            }
            yield return null;
        }
        Object.Destroy(go);
    }

    // ═══════════════════════════════════════════
    // TIPO 4 — DAGGER FLASH (flash de adaga)
    // Usado por: Assassino (ataque rápido)
    // Linha reta que aparece e desaparece em 0.1s
    // ═══════════════════════════════════════════
    public static IEnumerator DaggerFlash(Vector3 origin, Vector2 direction,
        float length, Color color, float width = 0.08f)
    {
        var go = new GameObject("VFX_DaggerFlash");
        var lr = go.AddComponent<LineRenderer>();
        lr.material = GetMat();
        lr.sortingOrder = 300;
        lr.positionCount = 3;

        Vector3 perp = new Vector3(-direction.y, direction.x, 0) * 0.05f;
        lr.SetPosition(0, origin + perp);
        lr.SetPosition(1, origin + (Vector3)(direction * length * 0.6f));
        lr.SetPosition(2, origin + (Vector3)(direction * length));

        float elapsed = 0f;
        float duration = 0.12f;
        while (elapsed < duration)
        {
            float t = 1f - elapsed / duration;
            lr.startWidth = width * t;
            lr.endWidth = 0f;
            lr.startColor = new Color(color.r, color.g, color.b, t);
            lr.endColor = new Color(color.r, color.g, color.b, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Object.Destroy(go);
    }

    // ═══════════════════════════════════════════
    // TIPO 5 — ARROW STREAK (rastro de flecha)
    // Usado por: Caçador
    // Linha fina e rápida com ponta triangular
    // ═══════════════════════════════════════════
    public static IEnumerator ArrowStreak(Transform originTransform, Vector2 direction,
        float speed, float range, Color color, System.Action<EnemyBase> onHit = null)
    {
        Vector3 startPos = originTransform.position;
        var go = new GameObject("VFX_Arrow");
        go.transform.position = startPos;
        var lr = go.AddComponent<LineRenderer>();
        lr.material = GetMat();
        lr.sortingOrder = 300;
        lr.positionCount = 2;
        lr.startWidth = 0.06f;
        lr.endWidth = 0.02f;
        lr.startColor = Color.white;
        lr.endColor = color;

        var tr = go.AddComponent<TrailRenderer>();
        tr.material = GetMat();
        tr.time = 0.08f;
        tr.startWidth = 0.04f;
        tr.endWidth = 0f;
        tr.startColor = color;
        tr.endColor = new Color(color.r, color.g, color.b, 0f);
        tr.sortingOrder = 299;

        Vector3 perpDir      = new Vector3(-direction.y, direction.x, 0);
        float   arrowHeadLen = 0.075f;  // era 0.25f → reduzido 70%
        float   arrowHeadW   = 0.024f;  // era 0.08f → reduzido 70%

        var tipL = new GameObject("TipL");
        tipL.transform.SetParent(go.transform, false);
        var lrL = tipL.AddComponent<LineRenderer>();
        lrL.material = GetMat();
        lrL.positionCount = 2;
        lrL.startWidth = 0.036f; lrL.endWidth = 0.012f;  // era 0.12f/0.04f → reduzido 70%
        lrL.startColor = Color.white; lrL.endColor = color;
        lrL.sortingOrder = 300;

        var tipR = new GameObject("TipR");
        tipR.transform.SetParent(go.transform, false);
        var lrR = tipR.AddComponent<LineRenderer>();
        lrR.material = GetMat();
        lrR.positionCount = 2;
        lrR.startWidth = 0.036f; lrR.endWidth = 0.012f;  // era 0.12f/0.04f → reduzido 70%
        lrR.startColor = Color.white; lrR.endColor = color;
        lrR.sortingOrder = 300;

        float traveled = 0f;
        while (traveled < range)
        {
            float   step = speed * Time.deltaTime;
            go.transform.position += (Vector3)(direction * step);
            Vector3 tip  = go.transform.position;
            lr.SetPosition(0, tip - (Vector3)(direction * 0.4f));
            lr.SetPosition(1, tip);
            lrL.SetPosition(0, tip);
            lrL.SetPosition(1, tip - (Vector3)(direction * arrowHeadLen) + perpDir * arrowHeadW);
            lrR.SetPosition(0, tip);
            lrR.SetPosition(1, tip - (Vector3)(direction * arrowHeadLen) - perpDir * arrowHeadW);
            traveled += step;

            var hitFilter = new ContactFilter2D();
            hitFilter.useTriggers = true;
            hitFilter.SetLayerMask(LayerMask.GetMask("Enemy"));
            var hitList = new System.Collections.Generic.List<Collider2D>();
            Physics2D.OverlapCircle(go.transform.position, 0.2f, hitFilter, hitList);
            if (hitList.Count > 0)
            {
                var eb = hitList[0].GetComponent<EnemyBase>()
                      ?? hitList[0].GetComponentInParent<EnemyBase>();
                if (eb != null && !eb.IsDead)
                {
                    Object.Destroy(go);
                    if (onHit != null) onHit(eb);
                    yield break;
                }
            }
            yield return null;
        }
        Object.Destroy(go);
    }

    // ═══════════════════════════════════════════
    // TIPO 7 — WHIP CHAIN (chicote em C)
    // Usado por: Guerreiro
    // Uma ponta FIXA no player, a outra se estende em arco C
    // ═══════════════════════════════════════════
    public static IEnumerator WhipChain(Transform playerTransform,
        Vector2 direction, float length, float duration, Color color)
    {
        var go = new GameObject("VFX_Whip");
        var lr = go.AddComponent<LineRenderer>();
        lr.material      = GetMat();
        lr.sortingOrder  = 300;
        lr.positionCount = 20;
        lr.startWidth    = 0.08f;
        lr.endWidth      = 0.02f;

        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(color, 0.3f),
                    new GradientColorKey(color * 0.6f, 1f) },
            new[] { new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(0f, 1f) }
        );
        lr.colorGradient = grad;

        Vector3 perp    = new Vector3(-direction.y, direction.x, 0);
        float   elapsed = 0f;
        int     segs    = 20;

        while (elapsed < duration && playerTransform != null)
        {
            float t     = elapsed / duration;
            // Extensão: vai de 0 a 1 rapidamente, depois volta
            float reach = t < 0.4f
                ? Mathf.SmoothStep(0f, 1f, t / 0.4f)
                : Mathf.SmoothStep(1f, 0f, (t - 0.4f) / 0.6f);

            Vector3 anchor = playerTransform.position; // ponta fixa no player

            for (int i = 0; i < segs; i++)
            {
                float s = (float)i / (segs - 1);
                // Forma de C: curva perpendicular que abre e fecha
                float curve = Mathf.Sin(s * Mathf.PI) * reach * 0.6f;
                Vector3 pt  = anchor
                    + (Vector3)(direction * s * length * reach)
                    + perp * curve;
                lr.SetPosition(i, pt);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
        Object.Destroy(go);
    }

    // ═══════════════════════════════════════════
    // TIPO 6 — EXPLOSION RING (anel de explosão)
    // Usado por: impactos, poderes especiais
    // Círculo que se expande e desaparece
    // ═══════════════════════════════════════════
    public static IEnumerator ExplosionRing(Vector3 center, Color color,
        float maxRadius, float duration, float width = 0.15f)
    {
        var go = new GameObject("VFX_Ring");
        go.transform.position = center;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.material = GetMat();
        lr.loop = true;
        lr.positionCount = 32;
        lr.sortingOrder = 300;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float r = maxRadius * t;
            float alpha = 1f - t;
            float w = width * (1f - t * 0.5f);

            lr.startWidth = w;
            lr.endWidth = w;
            lr.startColor = new Color(color.r, color.g, color.b, alpha);
            lr.endColor = new Color(color.r, color.g, color.b, alpha);

            for (int i = 0; i < 32; i++)
            {
                float angle = (float)i / 32 * Mathf.PI * 2f;
                lr.SetPosition(i, center + new Vector3(
                    Mathf.Cos(angle) * r,
                    Mathf.Sin(angle) * r, 0));
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        Object.Destroy(go);
    }

    // ═══════════════════════════════════════════
    // TIPO 7 — SKULL PROJECTILE (caveirinha geométrica)
    // Usado por: Necromante
    // ═══════════════════════════════════════════
    public static IEnumerator SkullProjectile(Vector3 origin,
        Vector2 direction, float speed, float range,
        System.Action<Vector3> onHit = null)
    {
        Color skullColor = new Color(0.5f, 1f, 0.4f);
        var go = new GameObject("VFX_Skull");
        go.transform.position = origin;

        CreateCircle(go, 0.12f, skullColor, 300, 12);
        CreateDot(go, new Vector3(-0.04f, 0.02f, 0), 0.03f, new Color(0f, 0f, 0f, 0.8f));
        CreateDot(go, new Vector3( 0.04f, 0.02f, 0), 0.03f, new Color(0f, 0f, 0f, 0.8f));

        var mouth = new GameObject("Mouth");
        mouth.transform.SetParent(go.transform, false);
        var lrM = mouth.AddComponent<LineRenderer>();
        lrM.material = GetMat();
        lrM.useWorldSpace = false;
        lrM.positionCount = 2;
        lrM.startWidth = 0.02f; lrM.endWidth = 0.02f;
        lrM.startColor = lrM.endColor = new Color(0f, 0f, 0f, 0.8f);
        lrM.sortingOrder = 301;
        lrM.SetPosition(0, new Vector3(-0.05f, -0.04f, 0));
        lrM.SetPosition(1, new Vector3( 0.05f, -0.04f, 0));

        var tr = go.AddComponent<TrailRenderer>();
        tr.material = GetMat();
        tr.time = 0.3f;
        tr.startWidth = 0.08f; tr.endWidth = 0f;
        tr.startColor = skullColor;
        tr.endColor = new Color(skullColor.r, skullColor.g, skullColor.b, 0f);
        tr.sortingOrder = 299;

        float traveled    = 0f;
        float bounceTimer = 0f;
        while (traveled < range)
        {
            float step = speed * Time.deltaTime;
            bounceTimer += Time.deltaTime;

            float bounce = Mathf.Abs(Mathf.Sin(bounceTimer * 12f)) * 0.05f;
            go.transform.position += (Vector3)(direction * step)
                                   + Vector3.up * (bounce - 0.025f);
            traveled += step;

            var filter = new ContactFilter2D();
            filter.useTriggers = true;
            filter.SetLayerMask(LayerMask.GetMask("Enemy"));
            var hits = new System.Collections.Generic.List<Collider2D>();
            Physics2D.OverlapCircle(go.transform.position, 0.35f, filter, hits);
            if (hits.Count > 0)
            {
                var eb = hits[0].GetComponent<EnemyBase>()
                      ?? hits[0].GetComponentInParent<EnemyBase>();
                if (eb != null && !eb.IsDead)
                {
                    Vector3 hitPos = go.transform.position;
                    Object.Destroy(go);
                    if (onHit != null) onHit(hitPos);
                    yield break;
                }
            }
            yield return null;
        }

        Vector3 finalPos = go.transform.position;
        Object.Destroy(go);
        if (onHit != null) onHit(finalPos);
    }

    // ═══════════════════════════════════════════
    // TIPO 8 — CROSS SLASH (X de corte)
    // Usado por: Assassino
    // ═══════════════════════════════════════════
    public static IEnumerator CrossSlash(MonoBehaviour host, Vector3 pos, Color color)
    {
        var d1 = new Vector2(0.707f,  0.707f);
        var d2 = new Vector2(0.707f, -0.707f);
        yield return null;
        host.StartCoroutine(DaggerFlash(pos, d1,  0.8f, color, 0.12f));
        host.StartCoroutine(DaggerFlash(pos, -d1, 0.8f, color, 0.12f));
        host.StartCoroutine(DaggerFlash(pos, d2,  0.8f, color, 0.12f));
        host.StartCoroutine(DaggerFlash(pos, -d2, 0.8f, color, 0.12f));
        host.StartCoroutine(ExplosionRing(pos, new Color(0.9f, 0f, 0.8f), 0.5f, 0.2f, 0.08f));
    }

    // ═══════════════════════════════════════════
    // TIPO 9 — CRESCENT SLASH (meia-lua de vento)
    // Usado por: Caçador
    // Arco crescente que expande girando levemente
    // ═══════════════════════════════════════════
    public static IEnumerator CrescentSlash(Vector3 origin, Vector2 direction,
        float radius, float duration, Color color, float width = 0.15f)
    {
        var go = new GameObject("VFX_Crescent");
        var lr = go.AddComponent<LineRenderer>();
        lr.material = GetMat();
        lr.sortingOrder = 300;
        int segments = 40;
        lr.positionCount = segments;

        float arcDegrees = 120f;
        float baseAngle  = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float halfArc    = arcDegrees * 0.5f;
        float elapsed    = 0f;

        while (elapsed < duration)
        {
            float t             = elapsed / duration;
            float currentRadius = Mathf.Lerp(radius * 0.2f, radius, t);
            float alpha         = t < 0.5f ? t * 2f : Mathf.Lerp(1f, 0.3f, (t - 0.5f) * 2f);
            float swingAngle    = Mathf.Lerp(-15f, 15f, t);

            var grad = new Gradient();
            grad.SetKeys(
                new[]{ new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[]{ new GradientAlphaKey(0f, 0f),
                       new GradientAlphaKey(alpha, 0.5f),
                       new GradientAlphaKey(0f, 1f) }
            );
            lr.colorGradient = grad;
            lr.startWidth = width * (1f - t * 0.4f);
            lr.endWidth   = lr.startWidth * 0.3f;

            float finalBase = baseAngle + swingAngle;
            for (int i = 0; i < segments; i++)
            {
                float s     = (float)i / (segments - 1);
                float angle = (finalBase - halfArc + arcDegrees * s) * Mathf.Deg2Rad;
                lr.SetPosition(i, origin + new Vector3(
                    Mathf.Cos(angle) * currentRadius,
                    Mathf.Sin(angle) * currentRadius, 0));
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        Object.Destroy(go);
    }

    // ═══════════════════════════════════════════
    // HELPERS INTERNOS
    // ═══════════════════════════════════════════
    static GameObject CreateCircle(GameObject parent, float radius,
        Color color, int sortOrder, int segments = 24)
    {
        var go = new GameObject("Core");
        go.transform.SetParent(parent.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.material = GetMat();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = segments;
        lr.startWidth = radius * 0.4f;
        lr.endWidth = radius * 0.4f;
        lr.startColor = color;
        lr.endColor = color;
        lr.sortingOrder = sortOrder;
        for (int i = 0; i < segments; i++)
        {
            float a = (float)i / segments * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(
                Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0));
        }
        return go;
    }

    static void CreateDot(GameObject parent, Vector3 localPos, float size, Color color)
    {
        var go = new GameObject("Dot");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        var lr = go.AddComponent<LineRenderer>();
        lr.material = GetMat();
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.startWidth = size; lr.endWidth = size;
        lr.startColor = lr.endColor = color;
        lr.sortingOrder = 302;
        lr.SetPosition(0, Vector3.zero);
        lr.SetPosition(1, new Vector3(0.001f, 0, 0));
    }

    static GameObject CreateExplosion(Vector3 pos, Color color,
        float size, float duration)
    {
        var go = new GameObject("VFX_Impact");
        go.transform.position = pos;
        for (int i = 0; i < 3; i++)
        {
            var ring = new GameObject($"Ring{i}");
            ring.transform.SetParent(go.transform, false);
            var lr = ring.AddComponent<LineRenderer>();
            lr.material = GetMat();
            lr.loop = true;
            lr.positionCount = 24;
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            float r = size * (0.3f + i * 0.35f);
            lr.startColor = new Color(color.r, color.g, color.b,
                                      1f - i * 0.3f);
            lr.endColor = lr.startColor;
            lr.sortingOrder = 300;
            for (int j = 0; j < 24; j++)
            {
                float a = (float)j / 24 * Mathf.PI * 2f;
                lr.SetPosition(j, new Vector3(
                    Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0));
            }
        }
        return go;
    }

    public static System.Collections.IEnumerator PulsingRing(
        System.Func<Vector3> getPos, Color color, float radius, float duration)
    {
        var go = new GameObject("VFX_PulsingRing");
        var lr = go.AddComponent<LineRenderer>();
        lr.material      = GetMat();
        lr.loop          = true;
        lr.positionCount = 32;
        lr.useWorldSpace = true;
        lr.sortingOrder  = 299;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(elapsed * 8f);
            float r     = radius * (0.9f + 0.1f * pulse);
            float alpha = 0.4f + 0.3f * pulse;
            float w     = 0.06f + 0.03f * pulse;

            lr.startWidth = w; lr.endWidth = w;
            lr.startColor = new Color(color.r, color.g, color.b, alpha);
            lr.endColor   = lr.startColor;

            Vector3 center = getPos();
            go.transform.position = center;
            for (int i = 0; i < 32; i++)
            {
                float angle = (float)i / 32 * Mathf.PI * 2f;
                lr.SetPosition(i, center + new Vector3(
                    Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0));
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        Object.Destroy(go);
    }
}
