using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

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
    // EVENTO DE CÂMERA (A6)
    // Disparado em todo impacto confirmado. Feel fará o shake depois.
    // Intensidades: 0.3 normal, 0.6 inimigo <20% HP, 1.0 poder especial
    // ═══════════════════════════════════════════
    public static event System.Action<float, Color> OnImpact;

    // ═══════════════════════════════════════════
    // CURVAS DE EASING (A1)
    // ═══════════════════════════════════════════
    static readonly AnimationCurve ScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    static readonly AnimationCurve SpeedCurve = new AnimationCurve(
        new Keyframe(0, 1.6f), new Keyframe(0.3f, 1f), new Keyframe(1, 0.7f));
    static readonly AnimationCurve FadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    // ═══════════════════════════════════════════
    // RUNNER — host persistente para coroutines disparadas de
    // dentro dos efeitos (hit-stop, camadas de impacto) e raiz do pool.
    // DontDestroyOnLoad: nunca morre no meio de um hit-stop.
    // ═══════════════════════════════════════════
    class VFXRuntime : MonoBehaviour { }

    static VFXRuntime _runner;
    static MonoBehaviour Runner
    {
        get
        {
            if (_runner == null)
            {
                var go = new GameObject("VFXRuntime");
                Object.DontDestroyOnLoad(go);
                _runner = go.AddComponent<VFXRuntime>();
            }
            return _runner;
        }
    }

    // ═══════════════════════════════════════════
    // FÍSICA CACHEADA (A0) — zero alocação no loop de voo.
    // O buffer é preenchido e lido na mesma chamada, sem yield no meio,
    // então o compartilhamento entre coroutines é seguro.
    // ═══════════════════════════════════════════
    static ContactFilter2D _enemyFilter;
    static bool _enemyFilterReady;
    static readonly List<Collider2D> _hitBuffer = new List<Collider2D>(16);

    static EnemyBase FindEnemyHit(Vector3 pos, float radius)
    {
        if (!_enemyFilterReady)
        {
            _enemyFilter = new ContactFilter2D { useTriggers = true, useLayerMask = true };
            _enemyFilter.SetLayerMask(LayerMask.GetMask("Enemy"));
            _enemyFilterReady = true;
        }
        Physics2D.OverlapCircle(pos, radius, _enemyFilter, _hitBuffer);
        for (int i = 0; i < _hitBuffer.Count; i++)
        {
            var col = _hitBuffer[i];
            if (col == null) continue;
            var eb = col.GetComponent<EnemyBase>() ?? col.GetComponentInParent<EnemyBase>();
            if (eb != null && !eb.IsDead) return eb;
        }
        return null;
    }

    // EnemyBase.currentHealth é protected e EnemyBase é intocável —
    // reflection cacheada, lida apenas no momento do impacto
    static System.Reflection.FieldInfo _hpField;
    static bool _hpFieldSearched;

    static float HealthRatio(EnemyBase eb)
    {
        if (eb == null || eb.maxHealth <= 0f) return 1f;
        if (!_hpFieldSearched)
        {
            _hpField = typeof(EnemyBase).GetField("currentHealth",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            _hpFieldSearched = true;
        }
        if (_hpField == null) return 1f;
        return (float)_hpField.GetValue(eb) / eb.maxHealth;
    }

    static Color Saturate(Color c, float mult)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        return Color.HSVToRGB(h, Mathf.Clamp01(s * mult), v);
    }

    // ═══════════════════════════════════════════
    // HIT-STOP (A2) — roda sempre no Runner (sobrevive a trocas de cena)
    // Política: NUNCA em hit de ataque base (autofire de survivor travaria
    // o jogo várias vezes por segundo). Só em kill, com cooldown global.
    // Poderes especiais podem chamar DoHitStop direto com valores fortes.
    // ═══════════════════════════════════════════
    static Coroutine _hitStopRoutine;
    static float _lastHitStop = -999f;
    const float KILL_HITSTOP_COOLDOWN = 1.5f;

    public static void DoHitStop(MonoBehaviour host, float duration = 0.08f, float scale = 0.05f)
    {
        if (_hitStopRoutine != null) return; // não empilhar hit-stops
        _hitStopRoutine = Runner.StartCoroutine(HitStopRoutine(duration, scale));
    }

    // Chamado APÓS o onHit aplicar o dano — fonte ÚNICA de VFX por evento:
    //   kill      → NÃO spawnar camadas (o VFX de morte do EnemyBase.Die é o único),
    //               hit-stop sutil com cooldown global, OnImpact 0.6 (câmera reage)
    //   sobreviveu → camadas de impacto normais, SEM hit-stop, OnImpact via camadas
    static void PostHitFeedback(Vector3 pos, Color color, EnemyBase eb)
    {
        bool died = eb == null || eb.IsDead || HealthRatio(eb) <= 0f;
        if (died)
        {
            if (Time.unscaledTime - _lastHitStop >= KILL_HITSTOP_COOLDOWN)
            {
                _lastHitStop = Time.unscaledTime;
                DoHitStop(Runner, 0.05f, 0.3f); // pausa sutil, não freeze
            }
            OnImpact?.Invoke(0.6f, color);
            return;
        }

        float ratio = HealthRatio(eb);
        float intensity = ratio < 0.2f ? 0.6f : 0.3f; // A7
        float scaleMult = ratio < 0.2f ? 1.3f : 1f;   // quase morto: impacto 1.3x maior
        if (ratio < 0.2f) color = Saturate(color, 1.3f);
        SpawnImpactLayersInternal(pos, color, intensity, scaleMult);
    }

    static IEnumerator HitStopRoutine(float duration, float scale)
    {
        float prev = Time.timeScale;
        Time.timeScale = scale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = prev;
        _hitStopRoutine = null;
    }

    // ═══════════════════════════════════════════
    // LAYERED EFFECTS (A3) — 3 camadas por impacto
    // Camada 1 = efeito principal (cada efeito), 2 = partículas, 3 = glow residual
    // ═══════════════════════════════════════════
    public static void SpawnImpactLayers(MonoBehaviour host, Vector3 pos, Color color, float intensity = 0.3f)
        => SpawnImpactLayersInternal(pos, color, intensity, 1f);

    static void SpawnImpactLayersInternal(Vector3 pos, Color color, float intensity, float scaleMult)
    {
        // Roda sempre no Runner: se o host morrer no meio, os sprites voltam ao pool mesmo assim
        if (intensity >= 1f) color = Saturate(color, 3f); // poder especial: saturação máxima (A7)
        int count = Mathf.Min(5 + (int)(intensity * 4), 12); // 6 em hit normal (0.3)
        Runner.StartCoroutine(ImpactParticles(pos, color, count, scaleMult));
        Runner.StartCoroutine(ResidualGlow(pos, color, intensity, scaleMult));
        OnImpact?.Invoke(intensity, color);
    }

    // Tempo unscaled: as camadas de impacto animam DURANTE o hit-stop
    static IEnumerator ImpactParticles(Vector3 pos, Color color, int count, float scaleMult = 1f)
    {
        count = Mathf.Min(count, 12);
        var sprites = new SpriteRenderer[count];
        var dirs = new Vector3[count];
        var speeds = new float[count];
        for (int i = 0; i < count; i++)
        {
            sprites[i] = GetSpriteGO("VFX_ImpactParticle", pos);
            sprites[i].sortingOrder = 305;
            sprites[i].color = color;
            sprites[i].transform.localScale = Vector3.one * (Random.Range(0.16f, 0.32f) * scaleMult);
            float a = (i + Random.value * 0.8f) / count * Mathf.PI * 2f;
            dirs[i] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            speeds[i] = Random.Range(3.75f, 6.75f);
        }

        const float life = 0.3f;
        float t = 0f;
        while (t < life)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            float k = t / life;
            float fade = FadeCurve.Evaluate(k);
            float decel = 1f - k * 0.8f;
            for (int i = 0; i < count; i++)
            {
                sprites[i].transform.position += dirs[i] * (speeds[i] * decel * dt);
                Color c = color; c.a = fade;
                sprites[i].color = c;
            }
            yield return null;
        }
        for (int i = 0; i < count; i++) ReleaseSprite(sprites[i].gameObject);
    }

    static IEnumerator ResidualGlow(Vector3 pos, Color color, float intensity, float scaleMult = 1f)
    {
        var sr = GetSpriteGO("VFX_ResidualGlow", pos);
        sr.sortingOrder = 304;
        // escala final 2.5x (calibração) × 1.3x p/ inimigo quase morto × 1.5x p/ poder especial
        float target = (0.375f + intensity * 1.125f) * scaleMult * (intensity >= 1f ? 1.5f : 1f);
        // squash de impacto (A5): nasce achatado e expande em tempo real
        sr.transform.localScale = new Vector3(0.6f, 1.4f, 1f) * (target * 0.1f);
        sr.transform.DOScale(Vector3.one * target, 0.5f).SetUpdate(true).SetEase(Ease.OutQuad);

        const float life = 0.5f;
        float t = 0f;
        while (t < life)
        {
            t += Time.unscaledDeltaTime;
            Color c = color; c.a = 0.8f * FadeCurve.Evaluate(t / life);
            sr.color = c;
            yield return null;
        }
        ReleaseSprite(sr.gameObject);
    }

    // ═══════════════════════════════════════════
    // ANTICIPATION FLASH (A4) — chamado pelo PlayerAttack antes de cada disparo
    // ═══════════════════════════════════════════
    static bool _flashing;

    public static IEnumerator AnticipationFlash(SpriteRenderer sr)
    {
        if (sr == null || _flashing) yield break; // não capturar branco como cor original
        _flashing = true;
        Color original = sr.color;
        sr.color = Color.white;
        float t = 0f;
        while (t < 0.07f) { t += Time.unscaledDeltaTime; yield return null; }
        if (sr != null) sr.color = original;
        _flashing = false;
    }

    // ═══════════════════════════════════════════
    // POOL DE VFX (Adendo 4) — zero Instantiate/Destroy durante combate
    // ═══════════════════════════════════════════
    const int LINE_POOL_CAP = 16;
    const int SPRITE_POOL_CAP = 24;
    static readonly Stack<GameObject> _linePool = new Stack<GameObject>();
    static readonly Stack<GameObject> _spritePool = new Stack<GameObject>();

    static GameObject GetLineGO(string name, Vector3 pos, out LineRenderer lr, out TrailRenderer tr)
    {
        GameObject go = null;
        while (_linePool.Count > 0 && go == null) go = _linePool.Pop(); // descarta refs destruídas
        if (go == null)
        {
            go = new GameObject("VFX_Line");
            go.transform.SetParent(Runner.transform, false);
            var newLr = go.AddComponent<LineRenderer>();
            newLr.material = GetMat();
            newLr.positionCount = 0;
            var newTr = go.AddComponent<TrailRenderer>();
            newTr.material = GetMat();
            newTr.emitting = false;
            newTr.enabled = false;
        }
        go.name = name;
        go.transform.position = pos;
        lr = go.GetComponent<LineRenderer>();
        tr = go.GetComponent<TrailRenderer>();
        go.SetActive(true);
        return go;
    }

    static readonly Gradient _defaultGrad = new Gradient(); // branco — atribuição copia, sem alloc

    static void ReleaseLine(GameObject go)
    {
        if (go == null) return;
        var tr = go.GetComponent<TrailRenderer>();
        if (tr != null) { tr.emitting = false; tr.Clear(); tr.enabled = false; }
        var lr = go.GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.positionCount = 0;
            lr.loop = false;
            lr.useWorldSpace = true;
            lr.widthMultiplier = 1f;
            lr.colorGradient = _defaultGrad; // gradientes não vazam para o próximo uso
        }
        go.transform.SetParent(Runner.transform, false);
        go.transform.localScale = Vector3.one;
        go.transform.rotation = Quaternion.identity;
        go.SetActive(false);
        if (_linePool.Count < LINE_POOL_CAP) _linePool.Push(go);
        else Object.Destroy(go);
    }

    static Sprite _circleSprite;
    static Sprite GetCircleSprite()
    {
        if (_circleSprite == null)
        {
            const int S = 16;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[S * S];
            float c = (S - 1) * 0.5f;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01(1f - d);
                    px[y * S + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _circleSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
        }
        return _circleSprite;
    }

    static SpriteRenderer GetSpriteGO(string name, Vector3 pos)
    {
        GameObject go = null;
        while (_spritePool.Count > 0 && go == null) go = _spritePool.Pop();
        SpriteRenderer sr;
        if (go == null)
        {
            go = new GameObject("VFX_Sprite");
            go.transform.SetParent(Runner.transform, false);
            sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetCircleSprite();
            sr.material = GetMat();
        }
        else sr = go.GetComponent<SpriteRenderer>();
        go.name = name;
        go.transform.position = pos;
        go.SetActive(true);
        return sr;
    }

    static void ReleaseSprite(GameObject go)
    {
        if (go == null) return;
        go.transform.DOKill();
        go.transform.SetParent(Runner.transform, false);
        go.transform.localScale = Vector3.one;
        go.SetActive(false);
        if (_spritePool.Count < SPRITE_POOL_CAP) _spritePool.Push(go);
        else Object.Destroy(go);
    }

    static void SetupTrail(TrailRenderer tr, float time, float width, Color color, int sortOrder)
    {
        tr.enabled = true;
        tr.Clear(); // não arrastar trilha fantasma do uso anterior do pool
        tr.emitting = true;
        tr.time = time;
        tr.startWidth = width;
        tr.endWidth = 0f;
        tr.startColor = color;
        tr.endColor = new Color(color.r, color.g, color.b, 0f);
        tr.sortingOrder = sortOrder;
    }

    // ═══════════════════════════════════════════
    // TIPO 1 — WHIP (chicote de energia)
    // Usado por: Guerreiro
    // Linha senoidal que vai e volta na direção do ataque
    // ═══════════════════════════════════════════
    public static IEnumerator Whip(Transform originTransform, Vector2 direction,
        float length, float duration, Color color, float width = 0.06f,
        int segments = 20, float amplitude = 0.4f)
    {
        var go = GetLineGO("VFX_Whip",
            originTransform != null ? originTransform.position : Vector3.zero, out var lr, out _);
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
        while (elapsed < duration && originTransform != null)
        {
            Vector3 origin  = originTransform.position;
            float   flicker = amplitude + Random.Range(-0.05f, 0.05f);
            float   t       = elapsed / duration;
            float   reach   = ScaleCurve.Evaluate(t < 0.5f ? t * 2f : (1f - t) * 2f); // A1

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
        ReleaseLine(go);
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
        var go = GetLineGO("VFX_EnergyBolt", origin, out var lr, out var tr);
        go.transform.rotation = Quaternion.FromToRotation(Vector3.right, direction); // squash na direção (A5)
        ConfigCircle(lr, size, coreColor, 300);
        SetupTrail(tr, 0.4f, size * 1.2f, trailColor, 299);

        float traveled = 0f;
        float elapsed  = 0f;
        while (traveled < range)
        {
            float step = speed * SpeedCurve.Evaluate(traveled / range) * Time.deltaTime; // A1
            go.transform.position += (Vector3)(direction * step);
            traveled += step;
            elapsed  += Time.deltaTime;

            float growth  = Mathf.Lerp(0.2f, 1f,
                ScaleCurve.Evaluate(Mathf.Clamp01(traveled / (range * 0.6f)))); // A1
            float squashT = Mathf.Clamp01(elapsed / 0.2f); // A5
            go.transform.localScale = new Vector3(
                growth * Mathf.Lerp(1.7f, 1f, squashT),
                growth * Mathf.Lerp(0.6f, 1f, squashT), 1f);
            tr.startWidth = size * growth * 0.8f;

            var eb = FindEnemyHit(go.transform.position, size * growth * 0.8f);
            if (eb != null)
            {
                Vector3 hitPos = go.transform.position;
                ReleaseLine(go);
                if (onHit != null) onHit(hitPos);       // aplica o dano
                PostHitFeedback(hitPos, coreColor, eb); // feedback pós-dano (kill vs hit)
                yield break;
            }

            yield return null;
        }

        ReleaseLine(go);
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
        var go = GetLineGO("VFX_SlashArc", origin, out var lr, out _);
        lr.startWidth = width;
        lr.endWidth = width * 0.1f;
        lr.positionCount = segments;
        lr.sortingOrder = 300;

        Color head = Color.Lerp(Color.white, color, 0.4f);
        Color tail = color * 0.3f;

        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float halfArc = arcDegrees * 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float currentRadius = radius * ScaleCurve.Evaluate(Mathf.Clamp01(t / 0.4f)); // A1
            float alpha = FadeCurve.Evaluate(t);                                          // A1

            lr.startColor = new Color(head.r, head.g, head.b, alpha);
            lr.endColor   = new Color(tail.r, tail.g, tail.b, 0f);

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

            elapsed += Time.deltaTime;
            yield return null;
        }
        ReleaseLine(go);
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
        var go = GetLineGO("VFX_Star", origin, out var lr, out var tr);
        go.transform.rotation = Quaternion.FromToRotation(Vector3.right, direction); // squash na direção (A5)

        float outerR = 0.18f;
        float innerR = 0.07f;
        int   pts    = 9; // 4 pontas + centro

        lr.useWorldSpace = false; // desenho local p/ a escala (squash) funcionar
        lr.loop          = true;
        lr.positionCount = pts;
        lr.startWidth    = 0.04f;
        lr.endWidth      = 0.04f;
        lr.startColor    = lr.endColor = color;
        lr.sortingOrder  = 301;

        SetupTrail(tr, 0.12f, 0.06f, color, 300);

        float traveled = 0f;
        float rotation = 0f;
        float elapsed  = 0f;

        while (traveled < range)
        {
            float step = speed * SpeedCurve.Evaluate(traveled / range) * Time.deltaTime; // A1
            go.transform.position += (Vector3)(direction * step);
            traveled += step;
            elapsed  += Time.deltaTime;
            rotation += 360f * Time.deltaTime * 4f; // rotação rápida

            float squashT = Mathf.Clamp01(elapsed / 0.2f); // A5
            go.transform.localScale = new Vector3(
                Mathf.Lerp(1.7f, 1f, squashT), Mathf.Lerp(0.6f, 1f, squashT), 1f);

            // Redesenhar a estrela rotacionada (coordenadas locais)
            for (int i = 0; i < pts; i++)
            {
                float baseAngle = (rotation + i * (360f / 4f)) * Mathf.Deg2Rad;
                float r = (i % 2 == 0) ? outerR : innerR;
                lr.SetPosition(i, new Vector3(
                    Mathf.Cos(baseAngle) * r,
                    Mathf.Sin(baseAngle) * r, 0));
            }

            var eb = FindEnemyHit(go.transform.position, outerR * 1.2f);
            if (eb != null)
            {
                Vector3 hitPos = go.transform.position;
                ReleaseLine(go);
                if (onHit != null) onHit(eb);       // aplica o dano
                PostHitFeedback(hitPos, color, eb); // feedback pós-dano (kill vs hit)
                yield break;
            }
            yield return null;
        }
        ReleaseLine(go);
    }

    // ═══════════════════════════════════════════
    // TIPO 4 — DAGGER FLASH (flash de adaga)
    // Usado por: Assassino (ataque rápido)
    // Linha reta que aparece e desaparece em 0.1s
    // ═══════════════════════════════════════════
    public static IEnumerator DaggerFlash(Vector3 origin, Vector2 direction,
        float length, Color color, float width = 0.08f)
    {
        var go = GetLineGO("VFX_DaggerFlash", origin, out var lr, out _);
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
            float t = FadeCurve.Evaluate(elapsed / duration); // A1
            lr.startWidth = width * t;
            lr.endWidth = 0f;
            lr.startColor = new Color(color.r, color.g, color.b, t);
            lr.endColor = new Color(color.r, color.g, color.b, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        ReleaseLine(go);
    }

    // ═══════════════════════════════════════════
    // TIPO 5 — ARROW STREAK (rastro de flecha)
    // Usado por: Caçador
    // Linha fina e rápida com ponta triangular (desenho local — A5)
    // ═══════════════════════════════════════════
    public static IEnumerator ArrowStreak(Transform originTransform, Vector2 direction,
        float speed, float range, Color color, System.Action<EnemyBase> onHit = null)
    {
        Vector3 startPos = originTransform != null ? originTransform.position : Vector3.zero;
        var go = GetLineGO("VFX_Arrow", startPos, out var lr, out var tr);
        go.transform.rotation = Quaternion.FromToRotation(Vector3.right, direction);

        lr.useWorldSpace = false;
        lr.sortingOrder = 300;
        lr.positionCount = 2;
        lr.startWidth = 0.06f;
        lr.endWidth = 0.02f;
        lr.startColor = Color.white;
        lr.endColor = color;
        lr.SetPosition(0, new Vector3(-0.4f, 0f, 0f));
        lr.SetPosition(1, Vector3.zero);

        SetupTrail(tr, 0.08f, 0.04f, color, 299);

        const float arrowHeadLen = 0.075f;
        const float arrowHeadW   = 0.024f;

        var tipL = GetLineGO("TipL", startPos, out var lrL, out _);
        tipL.transform.SetParent(go.transform, false);
        ConfigArrowTip(lrL, new Vector3(-arrowHeadLen, arrowHeadW, 0), color);

        var tipR = GetLineGO("TipR", startPos, out var lrR, out _);
        tipR.transform.SetParent(go.transform, false);
        ConfigArrowTip(lrR, new Vector3(-arrowHeadLen, -arrowHeadW, 0), color);

        float traveled = 0f;
        float elapsed  = 0f;
        while (traveled < range)
        {
            float step = speed * SpeedCurve.Evaluate(traveled / range) * Time.deltaTime; // A1
            go.transform.position += (Vector3)(direction * step);
            traveled += step;
            elapsed  += Time.deltaTime;

            float squashT = Mathf.Clamp01(elapsed / 0.2f); // A5
            go.transform.localScale = new Vector3(
                Mathf.Lerp(1.7f, 1f, squashT), Mathf.Lerp(0.6f, 1f, squashT), 1f);

            var eb = FindEnemyHit(go.transform.position, 0.2f);
            if (eb != null)
            {
                Vector3 hitPos = go.transform.position;
                ReleaseLine(tipL);
                ReleaseLine(tipR);
                ReleaseLine(go);
                if (onHit != null) onHit(eb);       // aplica o dano
                PostHitFeedback(hitPos, color, eb); // feedback pós-dano (kill vs hit)
                yield break;
            }
            yield return null;
        }
        ReleaseLine(tipL);
        ReleaseLine(tipR);
        ReleaseLine(go);
    }

    static void ConfigArrowTip(LineRenderer lr, Vector3 localEnd, Color color)
    {
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.startWidth = 0.036f;
        lr.endWidth = 0.012f;
        lr.startColor = Color.white;
        lr.endColor = color;
        lr.sortingOrder = 300;
        lr.SetPosition(0, Vector3.zero);
        lr.SetPosition(1, localEnd);
    }

    // ═══════════════════════════════════════════
    // TIPO 7 — WHIP CHAIN (chicote em C)
    // Usado por: Guerreiro
    // Uma ponta FIXA no player, a outra se estende em arco C
    // ═══════════════════════════════════════════
    public static IEnumerator WhipChain(Transform playerTransform,
        Vector2 direction, float length, float duration, Color color)
    {
        var go = GetLineGO("VFX_Whip",
            playerTransform != null ? playerTransform.position : Vector3.zero, out var lr, out _);
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
            // Extensão: vai de 0 a 1 rapidamente, depois volta (A1)
            float reach = t < 0.4f
                ? ScaleCurve.Evaluate(t / 0.4f)
                : ScaleCurve.Evaluate(1f - (t - 0.4f) / 0.6f);

            Vector3 anchor = playerTransform.position; // ponta fixa no player

            for (int i = 0; i < segs; i++)
            {
                float s = (float)i / (segs - 1);
                // Forma de C/gancho: bojo proporcional ao comprimento (k=0.4) e
                // assimétrico (fase 0.7) — pico adiantado e ponta hookada (não volta ao eixo).
                float fwd   = s * length * reach;
                float curve = Mathf.Sin(s * Mathf.PI * 0.7f) * length * 0.35f * reach;
                Vector3 pt  = anchor
                    + (Vector3)(direction * fwd)
                    + perp * curve;
                lr.SetPosition(i, pt);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
        ReleaseLine(go);
    }

    // ═══════════════════════════════════════════
    // TIPO 6 — EXPLOSION RING (anel de explosão)
    // Usado por: impactos, poderes especiais
    // Círculo que se expande e desaparece
    // ═══════════════════════════════════════════
    public static IEnumerator ExplosionRing(Vector3 center, Color color,
        float maxRadius, float duration, float width = 0.15f)
    {
        var go = GetLineGO("VFX_Ring", center, out var lr, out _);
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = 32;
        lr.sortingOrder = 300;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float r = maxRadius * ScaleCurve.Evaluate(t); // A1
            float alpha = FadeCurve.Evaluate(t);          // A1
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
        ReleaseLine(go);
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
        Color dark = new Color(0f, 0f, 0f, 0.8f);

        var go = GetLineGO("VFX_Skull", origin, out var lr, out var tr);
        go.transform.rotation = Quaternion.FromToRotation(Vector3.right, direction); // squash na direção (A5)

        ConfigCircle(lr, 0.12f, skullColor, 300, 12);
        var eyeL = CreateDot(go, new Vector3(-0.04f, 0.02f, 0), 0.03f, dark);
        var eyeR = CreateDot(go, new Vector3( 0.04f, 0.02f, 0), 0.03f, dark);

        var mouth = GetLineGO("Mouth", origin, out var lrM, out _);
        mouth.transform.SetParent(go.transform, false);
        lrM.useWorldSpace = false;
        lrM.positionCount = 2;
        lrM.startWidth = 0.02f; lrM.endWidth = 0.02f;
        lrM.startColor = lrM.endColor = dark;
        lrM.sortingOrder = 301;
        lrM.SetPosition(0, new Vector3(-0.05f, -0.04f, 0));
        lrM.SetPosition(1, new Vector3( 0.05f, -0.04f, 0));

        SetupTrail(tr, 0.3f, 0.08f, skullColor, 299);

        float traveled    = 0f;
        float bounceTimer = 0f;
        float elapsed     = 0f;
        while (traveled < range)
        {
            float step = speed * SpeedCurve.Evaluate(traveled / range) * Time.deltaTime; // A1
            bounceTimer += Time.deltaTime;
            elapsed     += Time.deltaTime;

            float bounce = Mathf.Abs(Mathf.Sin(bounceTimer * 12f)) * 0.05f;
            go.transform.position += (Vector3)(direction * step)
                                   + Vector3.up * (bounce - 0.025f);
            traveled += step;

            float squashT = Mathf.Clamp01(elapsed / 0.2f); // A5
            go.transform.localScale = new Vector3(
                Mathf.Lerp(1.7f, 1f, squashT), Mathf.Lerp(0.6f, 1f, squashT), 1f);

            var eb = FindEnemyHit(go.transform.position, 0.35f);
            if (eb != null)
            {
                Vector3 hitPos = go.transform.position;
                ReleaseLine(eyeL); ReleaseLine(eyeR); ReleaseLine(mouth); ReleaseLine(go);
                if (onHit != null) onHit(hitPos);        // aplica o dano (AoE)
                PostHitFeedback(hitPos, skullColor, eb); // feedback pós-dano (kill vs hit)
                yield break;
            }
            yield return null;
        }

        Vector3 finalPos = go.transform.position;
        SpawnImpactLayers(Runner, finalPos, skullColor); // explode no fim do curso (sem hit-stop)
        ReleaseLine(eyeL); ReleaseLine(eyeR); ReleaseLine(mouth); ReleaseLine(go);
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

    // Scratch compartilhado p/ gradiente sem alocação por frame —
    // preenchido e atribuído na mesma chamada, sem yield no meio
    static readonly GradientColorKey[] _crescentColorKeys = new GradientColorKey[2];
    static readonly GradientAlphaKey[] _crescentAlphaKeys = new GradientAlphaKey[3];
    static readonly Gradient _crescentGrad = new Gradient();

    public static IEnumerator CrescentSlash(Vector3 origin, Vector2 direction,
        float radius, float duration, Color color, float width = 0.15f)
    {
        var go = GetLineGO("VFX_Crescent", origin, out var lr, out _);
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
            float currentRadius = Mathf.Lerp(radius * 0.2f, radius, ScaleCurve.Evaluate(t)); // A1
            float alpha         = t < 0.5f
                ? t * 2f
                : Mathf.Lerp(1f, 0.3f, 1f - FadeCurve.Evaluate((t - 0.5f) * 2f)); // A1
            float swingAngle    = Mathf.Lerp(-15f, 15f, t);

            _crescentColorKeys[0] = new GradientColorKey(color, 0f);
            _crescentColorKeys[1] = new GradientColorKey(color, 1f);
            _crescentAlphaKeys[0] = new GradientAlphaKey(0f, 0f);
            _crescentAlphaKeys[1] = new GradientAlphaKey(alpha, 0.5f);
            _crescentAlphaKeys[2] = new GradientAlphaKey(0f, 1f);
            _crescentGrad.SetKeys(_crescentColorKeys, _crescentAlphaKeys);
            lr.colorGradient = _crescentGrad;
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
        ReleaseLine(go);
    }

    // ═══════════════════════════════════════════
    // HELPERS INTERNOS
    // ═══════════════════════════════════════════

    // Configura o LineRenderer (pooled) como círculo em espaço local
    static void ConfigCircle(LineRenderer lr, float radius,
        Color color, int sortOrder, int segments = 24)
    {
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
    }

    static GameObject CreateDot(GameObject parent, Vector3 localPos, float size, Color color)
    {
        var go = GetLineGO("VFX_Dot", parent.transform.position, out var lr, out _);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.startWidth = size; lr.endWidth = size;
        lr.startColor = lr.endColor = color;
        lr.sortingOrder = 302;
        lr.SetPosition(0, Vector3.zero);
        lr.SetPosition(1, new Vector3(0.001f, 0, 0));
        return go;
    }

    public static System.Collections.IEnumerator PulsingRing(
        System.Func<Vector3> getPos, Color color, float radius, float duration)
    {
        var go = GetLineGO("VFX_PulsingRing", getPos(), out var lr, out _);
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
        ReleaseLine(go);
    }
}
