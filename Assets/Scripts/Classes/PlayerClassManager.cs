using System.Collections;
using UnityEngine;

public class PlayerClassManager : MonoBehaviour
{
    public static PlayerClassManager Instance { get; private set; }
    public ClassDefinition CurrentClass { get; private set; }

    const string SELECTED_CLASS_KEY = "selected_class";
    const string DEFAULT_CLASS      = "warrior";

    // Boosts ativos na run atual
    public System.Collections.Generic.List<string> ActiveBoosts { get; private set; } = new();

    public void AddBoost(string boostId)
    {
        if (!ActiveBoosts.Contains(boostId))
            ActiveBoosts.Add(boostId);
    }

    public bool HasBoost(string boostId) => ActiveBoosts.Contains(boostId);

    public void ClearBoosts() { ActiveBoosts.Clear(); SedeSangueStacks = 0; }

    public void ActivateSpecialPower()
    {
        if (CurrentClass == null) return;
        switch (CurrentClass.classId)
        {
            case "warrior":     StartCoroutine(Special_FuriaSanguinaria()); break;
            case "mage":        StartCoroutine(Special_NovaArcana());       break;
            case "assassin":    StartCoroutine(Special_FaseSombria());      break;
            case "necromancer": StartCoroutine(Special_MaldicaoEmArea());   break;
            case "paladin":     StartCoroutine(Special_JulgamentoDivino()); break;
            case "hunter":      StartCoroutine(Special_ChuvaDeFlechas());   break;
        }
    }

    // ── GUERREIRO — Fúria Sanguinária ────────────────────────────────────────
    System.Collections.IEnumerator Special_FuriaSanguinaria()
    {
        var pc = PlayerController.Instance;
        var pa = pc?.GetComponent<PlayerAttack>();
        var ph = pc?.GetComponent<PlayerHealth>();
        var sr = pc?.GetComponent<SpriteRenderer>() ?? pc?.GetComponentInChildren<SpriteRenderer>();
        if (pa == null) yield break;

        float origDamage   = pa.attackDamage;
        float origCooldown = pa.attackCooldown;
        float duracao      = 10f;

        pa.attackDamage   *= 1.5f;
        pa.attackCooldown *= 0.5f;
        if (sr) sr.color   = new Color(1f, 0.2f, 0.1f, 1f);

        float maxRadius = Camera.main != null ? Camera.main.orthographicSize * 0.7f : 9f;
        StartCoroutine(RepeatingExpandRing(
            () => pc != null ? pc.transform.position : Vector3.zero,
            new Color(1f, 0.1f, 0.05f, 0.9f),
            maxRadius, 1.5f, pa.attackDamage, 3, 3f));

        StartCoroutine(ProceduralVFX.PulsingRing(
            () => pc != null ? pc.transform.position : Vector3.zero,
            new Color(1f, 0.1f, 0.05f, 0.6f), 1.5f, duracao));

        bool ativo = true;
        _furiaSanguinariaAtiva = true;
        System.Action onKill = () => {
            if (!ativo) return;
            if (ph != null) ph.Heal(5f);
            if (HasBoost("sede_sangue")) SedeSangueStacks++;
        };
        EnemyBase.OnEnemyDied += onKill;

        yield return new UnityEngine.WaitForSeconds(duracao);

        ativo = false;
        _furiaSanguinariaAtiva = false;
        EnemyBase.OnEnemyDied -= onKill;
        pa.attackDamage   = origDamage;
        pa.attackCooldown = origCooldown;
        if (sr) sr.color  = Color.white;
    }

    // ── MAGO — Nova Arcana ────────────────────────────────────────────────────
    System.Collections.IEnumerator Special_NovaArcana()
    {
        var pc = PlayerController.Instance;
        if (pc == null) yield break;

        var sr = pc.GetComponent<SpriteRenderer>() ?? pc.GetComponentInChildren<SpriteRenderer>();
        if (sr) sr.color = Color.white * 2f;

        var pa = pc.GetComponent<PlayerAttack>();
        float dmg = pa != null ? pa.attackDamage * 3f : 60f;
        float range = Camera.main != null ? Mathf.Max(Camera.main.orthographicSize, Camera.main.orthographicSize * Camera.main.aspect) * 1.5f : 20f;

        for (int burst = 0; burst < 2; burst++)
        {
            if (burst > 0) yield return new UnityEngine.WaitForSeconds(3f);

            if (sr) sr.color = Color.white * 2f;

            for (int i = 0; i < 8; i++)
            {
                float angle = i * (360f / 8) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float d = dmg;
                StartCoroutine(ProceduralVFX.EnergyBolt(
                    pc.transform.position, dir,
                    speed: 14f, range: range,
                    coreColor:  new Color(0.8f, 0.4f, 1f),
                    trailColor: new Color(1f, 0.8f, 1f),
                    size: 0.35f,
                    onHit: hitPos => {
                        UnityEngine.Object.FindFirstObjectByType<PlayerAttack>()
                            ?.ApplyDamageAtPointPublic(hitPos, 1.2f, d * 1.5f);
                    }));
            }

            StartCoroutine(ProceduralVFX.ExplosionRing(
                pc.transform.position, new Color(0.8f, 0.4f, 1f), 3f, 0.5f));

            yield return new UnityEngine.WaitForSeconds(0.1f);
            if (sr) sr.color = Color.white;
        }
    }

    // ── ASSASSINO — Fase Sombria ──────────────────────────────────────────────
    System.Collections.IEnumerator Special_FaseSombria()
    {
        var pc = PlayerController.Instance;
        var ph = pc?.GetComponent<PlayerHealth>();
        var pa = pc?.GetComponent<PlayerAttack>();
        var sr = pc?.GetComponent<SpriteRenderer>() ?? pc?.GetComponentInChildren<SpriteRenderer>();
        if (pc == null) yield break;

        float origSpeed      = pc.moveSpeed;
        bool  origInvincible = ph != null && ph.IsInvincible;
        float duracao        = 10f;

        float maxRadius = Camera.main != null ? Camera.main.orthographicSize * 0.7f : 9f;
        float dmg = pa != null ? pa.attackDamage * 2f : 20f;
        StartCoroutine(ExpandingDamageRing(
            pc.transform.position,
            new Color(0.5f, 0f, 1f, 0.9f),
            maxRadius, 1.2f, dmg));

        if (sr) sr.color = new Color(0.5f, 0.1f, 0.8f, 0.4f);
        if (ph != null) ph.IsInvincible = true;
        pc.SetMoveSpeed(origSpeed * 1.8f);
        _faseSombriaAtiva = true;

        yield return new UnityEngine.WaitForSeconds(duracao);

        _faseSombriaAtiva = false;
        if (sr) sr.color = Color.white;
        if (ph != null) ph.IsInvincible = origInvincible;
        pc.SetMoveSpeed(origSpeed);
    }
    bool _faseSombriaAtiva = false;
    public bool FaseSombriaAtiva => _faseSombriaAtiva;

    bool _furiaSanguinariaAtiva = false;
    public bool FuriaSanguinariaAtiva => _furiaSanguinariaAtiva;
    public int SedeSangueStacks { get; private set; } = 0;

    // ── NECROMANTE — Maldição em Área ─────────────────────────────────────────
    System.Collections.IEnumerator Special_MaldicaoEmArea()
    {
        var pc = PlayerController.Instance;
        var pa = pc?.GetComponent<PlayerAttack>();
        if (pc == null || pa == null) yield break;

        float duracao  = CurrentClass.specialDuration + (HasBoost("maldicao_ampliada") ? 3f : 0f);
        float elapsed  = 0f;
        float interval = 0.4f;
        float nextShot = 0f;

        var sr = pc.GetComponent<SpriteRenderer>() ?? pc.GetComponentInChildren<SpriteRenderer>();
        if (sr) sr.color = new Color(0.3f, 0.9f, 0.3f, 1f);

        while (elapsed < duracao)
        {
            if (elapsed >= nextShot)
            {
                nextShot = elapsed + interval;
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * (360f / 8) * Mathf.Deg2Rad;
                    Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    float d     = pa.attackDamage;
                    float r     = pa.attackRange;
                    float skullRange = Camera.main != null ? Mathf.Max(Camera.main.orthographicSize, Camera.main.orthographicSize * Camera.main.aspect) * 1.5f : 20f;
                    StartCoroutine(ProceduralVFX.SkullProjectile(
                        pc.transform.position, dir, 10f, skullRange,
                        onHit: hitPos => {
                            float aoeRadius = HasBoost("caveira_explosiva") ? 1.2f : 0.6f;
                            pa.ApplyDamageAtPointPublic(hitPos, aoeRadius, d);
                            StartCoroutine(ProceduralVFX.ExplosionRing(
                                hitPos, new Color(0.2f, 1f, 0.2f), aoeRadius * 1.5f, 0.2f));
                        }));
                }
            }
            elapsed += UnityEngine.Time.deltaTime;
            yield return null;
        }

        if (sr) sr.color = Color.white;
    }

    // ── PALADINO — Julgamento Divino ──────────────────────────────────────────
    System.Collections.IEnumerator Special_JulgamentoDivino()
    {
        var pc = PlayerController.Instance;
        var pa = pc?.GetComponent<PlayerAttack>();
        if (pc == null || pa == null) yield break;

        var sr = pc.GetComponent<SpriteRenderer>() ?? pc.GetComponentInChildren<SpriteRenderer>();
        if (sr) sr.color = new Color(1f, 0.95f, 0.4f, 1f);

        float stunDuration = HasBoost("luz_cegante") ? 4f : 2f;
        float radius = Camera.main != null ? Mathf.Max(Camera.main.orthographicSize, Camera.main.orthographicSize * Camera.main.aspect) * 1.5f : 20f;

        StartCoroutine(ProceduralVFX.ExplosionRing(
            pc.transform.position, new Color(1f, 0.9f, 0.2f), radius, 0.6f));
        StartCoroutine(ProceduralVFX.ExplosionRing(
            pc.transform.position, new Color(1f, 0.95f, 0.5f), radius * 0.6f, 0.4f));

        var hits = UnityEngine.Physics2D.OverlapCircleAll(pc.transform.position, radius);
        foreach (var h in hits)
        {
            var eb = h.GetComponent<EnemyBase>() ?? h.GetComponentInParent<EnemyBase>();
            if (eb != null && !eb.IsDead)
            {
                eb.TakeDamage(pa.attackDamage * 8f);
                eb.ApplyStun(stunDuration);
            }
        }

        if (HasBoost("consagracao"))
            StartCoroutine(Consagracao(pc.transform.position, radius * 0.7f, 8f));

        yield return new UnityEngine.WaitForSeconds(0.3f);
        if (sr) sr.color = Color.white;
    }

    System.Collections.IEnumerator Consagracao(Vector3 pos, float radius, float duracao)
    {
        StartCoroutine(ProceduralVFX.PulsingRing(
            () => pos, new Color(1f, 0.85f, 0.1f, 0.5f), radius, duracao));

        float elapsed = 0f;
        while (elapsed < duracao)
        {
            var hits = UnityEngine.Physics2D.OverlapCircleAll(pos, radius);
            foreach (var h in hits)
            {
                var eb = h.GetComponent<EnemyBase>() ?? h.GetComponentInParent<EnemyBase>();
                if (eb != null && !eb.IsDead)
                {
                    eb.TakeDamage(5f);
                    eb.moveSpeed = Mathf.Max(eb.moveSpeed * 0.97f, 0.5f);
                }
            }
            elapsed += 0.5f;
            yield return new UnityEngine.WaitForSeconds(0.5f);
        }
    }

    System.Collections.IEnumerator ExpandingDamageRing(
        Vector3 center, Color color, float maxRadius, float expandTime, float damage)
    {
        var go = new UnityEngine.GameObject("VFX_ExpandRing");
        var lr = go.AddComponent<UnityEngine.LineRenderer>();
        lr.material      = ProceduralVFX.GetPublicMat();
        lr.loop          = true;
        lr.positionCount = 48;
        lr.useWorldSpace = true;
        lr.sortingOrder  = 300;

        var hitEnemies = new System.Collections.Generic.HashSet<EnemyBase>();
        float elapsed = 0f;

        while (elapsed < expandTime)
        {
            float t     = elapsed / expandTime;
            float r     = UnityEngine.Mathf.Lerp(0.1f, maxRadius, t);
            float alpha = 1f - t * 0.3f;
            float width = UnityEngine.Mathf.Lerp(0.3f, 0.08f, t);

            lr.startWidth = width; lr.endWidth = width;
            lr.startColor = new UnityEngine.Color(color.r, color.g, color.b, alpha);
            lr.endColor   = lr.startColor;

            for (int i = 0; i < 48; i++)
            {
                float angle = (float)i / 48 * UnityEngine.Mathf.PI * 2f;
                lr.SetPosition(i, center + new Vector3(
                    UnityEngine.Mathf.Cos(angle) * r,
                    UnityEngine.Mathf.Sin(angle) * r, 0));
            }

            var cols = UnityEngine.Physics2D.OverlapCircleAll(center, r + 0.3f);
            foreach (var col in cols)
            {
                var eb = col.GetComponent<EnemyBase>() ?? col.GetComponentInParent<EnemyBase>();
                if (eb != null && !eb.IsDead && !hitEnemies.Contains(eb))
                {
                    hitEnemies.Add(eb);
                    eb.TakeDamage(damage);
                }
            }

            elapsed += UnityEngine.Time.deltaTime;
            yield return null;
        }

        UnityEngine.Object.Destroy(go);
    }

    System.Collections.IEnumerator RepeatingExpandRing(
        System.Func<Vector3> getCenter, Color color,
        float maxRadius, float expandTime, float damage,
        int repeats, float interval)
    {
        for (int i = 0; i < repeats; i++)
        {
            if (i > 0) yield return new UnityEngine.WaitForSeconds(interval);
            yield return StartCoroutine(ExpandingDamageRing(
                getCenter(), color, maxRadius, expandTime, damage));
        }
    }

    // ── CAÇADOR — Chuva de Flechas ────────────────────────────────────────────
    System.Collections.IEnumerator Special_ChuvaDeFlechas()
    {
        var pc = PlayerController.Instance;
        var pa = pc?.GetComponent<PlayerAttack>();
        if (pc == null || pa == null) yield break;

        var sr = pc.GetComponent<SpriteRenderer>() ?? pc.GetComponentInChildren<SpriteRenderer>();
        if (sr) sr.color = new Color(0.6f, 1f, 0.4f, 1f);

        float duracao  = CurrentClass.specialDuration;
        float elapsed  = 0f;
        float interval = 0.15f;
        float nextShot = 0f;
        bool  dupla    = HasBoost("rajada_dupla");
        float dmg      = pa.attackDamage;
        float camRange = Camera.main != null ? Mathf.Max(Camera.main.orthographicSize, Camera.main.orthographicSize * Camera.main.aspect) * 1.5f : 20f;
        float range    = camRange * (HasBoost("olho_aguia") ? 1.4f : 1f);
        bool  piercing = HasBoost("flechas_perfurantes");

        while (elapsed < duracao)
        {
            if (elapsed >= nextShot)
            {
                nextShot = elapsed + interval;
                int totalFlechas = dupla ? 16 : 8;
                for (int i = 0; i < totalFlechas; i++)
                {
                    float angle = (float)i / totalFlechas * Mathf.PI * 2f;
                    Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    bool piercing = HasBoost("flechas_perfurantes");
                    float d = dmg;
                    StartCoroutine(ProceduralVFX.ArrowStreak(
                        pc.transform.position, dir, 20f, range,
                        new Color(0.6f, 1f, 0.3f),
                        onHit: hitPos => {
                            UnityEngine.Object.FindFirstObjectByType<PlayerAttack>()
                                ?.ApplyDamageAtPointPublic(hitPos, piercing ? 0.8f : 0.3f, d * 2.5f);
                        }));
                }
            }
            elapsed += UnityEngine.Time.deltaTime;
            yield return null;
        }

        if (sr) sr.color = Color.white;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LoadSelectedClass();
    }

    void Start()
    {
        var player = PlayerController.Instance?.gameObject;
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p;
        }

        if (player != null)
            ApplyClassToPlayer(player);
        else
            StartCoroutine(WaitForPlayerAndApply());
    }

    IEnumerator WaitForPlayerAndApply()
    {
        while (PlayerController.Instance == null)
            yield return null;
        ApplyClassToPlayer(PlayerController.Instance.gameObject);
    }

    void LoadSelectedClass()
    {
        string classId = PlayerPrefs.GetString(SELECTED_CLASS_KEY, DEFAULT_CLASS);
        CurrentClass = Resources.Load<ClassDefinition>($"Classes/{classId}");

        if (CurrentClass == null)
        {
            Debug.LogWarning($"[ClassManager] Classe '{classId}' não encontrada — usando {DEFAULT_CLASS}");
            CurrentClass = Resources.Load<ClassDefinition>($"Classes/{DEFAULT_CLASS}");
        }

        Debug.Log($"[ClassManager] Classe carregada: {CurrentClass?.className ?? "NENHUMA"}");
    }

    public void SelectClass(string classId)
    {
        var def = Resources.Load<ClassDefinition>($"Classes/{classId}");
        if (def == null)
        {
            Debug.LogError($"[ClassManager] Classe '{classId}' não existe");
            return;
        }
        if (!IsClassUnlocked(classId))
        {
            Debug.LogWarning($"[ClassManager] Classe '{classId}' bloqueada");
            return;
        }
        PlayerPrefs.SetString(SELECTED_CLASS_KEY, classId);
        PlayerPrefs.Save();
        CurrentClass = def;
    }

    public bool IsClassUnlocked(string classId)
    {
        var def = Resources.Load<ClassDefinition>($"Classes/{classId}");
        if (def == null) return false;
        if (def.unlockedByDefault) return true;
        return PlayerPrefs.GetInt($"class_unlocked_{classId}", 0) == 1;
    }

    public void UnlockClass(string classId)
    {
        PlayerPrefs.SetInt($"class_unlocked_{classId}", 1);
        PlayerPrefs.Save();
        Debug.Log($"[ClassManager] Classe '{classId}' desbloqueada");
    }

    public void ApplyClassToPlayer(GameObject playerGO)
    {
        Debug.Log($"[ClassManager] ApplyClassToPlayer chamado. Classe={CurrentClass?.className ?? "NULL"}, selected_class PlayerPrefs={PlayerPrefs.GetString(SELECTED_CLASS_KEY, "vazio")}");

        if (CurrentClass == null) { Debug.LogError("[ClassManager] CurrentClass é null — LoadSelectedClass falhou?"); return; }

        var perm = PermanentUpgradeSystem.Instance;

        var health = playerGO.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.SetMaxHealth(CurrentClass.maxHP);
            float vidaBonus = (perm?.GetBonus(PermanentUpgradeId.VidaMaxima) ?? 1f) - 1f;
            if (vidaBonus > 0f) health.AumentarVidaMax(health.MaxHealth * vidaBonus);
        }

        var attack = playerGO.GetComponent<PlayerAttack>();
        if (attack != null)
        {
            Debug.Log($"[ClassManager] Aplicando {CurrentClass.className}: interval={CurrentClass.attackInterval}");
            attack.SetClassConfig(
                CurrentClass.attackDamage,
                CurrentClass.attackRange,
                CurrentClass.attackInterval,
                CurrentClass.attackType,
                CurrentClass.attackArc,
                CurrentClass.projectileCount);
            attack.attackDamage   *= perm?.GetBonus(PermanentUpgradeId.Poder)  ?? 1f;
            attack.attackRange    *= perm?.GetBonus(PermanentUpgradeId.Area)   ?? 1f;
            attack.attackCooldown /= perm?.GetBonus(PermanentUpgradeId.Recarga) ?? 1f;
        }

        var controller = playerGO.GetComponent<PlayerController>();
        if (controller != null)
            controller.SetMoveSpeed(CurrentClass.moveSpeed * (perm?.GetBonus(PermanentUpgradeId.Movimento) ?? 1f));

        playerGO.transform.localScale = Vector3.one * CurrentClass.worldScale;

        var anim = playerGO.GetComponent<CharacterAnimator>();
        int idleCount = CurrentClass.idleFrames?.Length ?? 0;
        Debug.Log($"[ClassManager] CharacterAnimator={anim != null}, idleFrames={idleCount}, walkFrames={CurrentClass.walkFrames?.Length ?? 0}");
        if (anim != null && idleCount > 0)
        {
            anim.OverrideFrames(
                CurrentClass.idleFrames,
                CurrentClass.walkFrames,
                CurrentClass.attackFrames,
                CurrentClass.hurtFrames,
                CurrentClass.deathFrames);
            Debug.Log($"[ClassManager] OverrideFrames aplicado — {idleCount} frames idle");
        }
        else if (idleCount == 0)
        {
            Debug.LogWarning($"[ClassManager] idleFrames vazio para '{CurrentClass.classId}' — rode Solengard > Classes > Setup Hero Animations no Editor");
        }

        Debug.Log($"[ClassManager] Player configurado como {CurrentClass.className}: sprite trocado, stats aplicados");
    }
}
