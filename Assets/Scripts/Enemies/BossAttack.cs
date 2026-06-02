using UnityEngine;
using DG.Tweening;
using System.Collections;

public class BossAttack : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] float attackRange    = 6f;
    [SerializeField] float attackCooldown = 3f;
    [SerializeField] float attackDamage   = 25f;

    [Header("VFX")]
    [SerializeField] GameObject aoeVFXPrefab;
    [SerializeField] GameObject explosionPrefab;
    [SerializeField] GameObject chargeVFXPrefab;

    float     _cooldown;
    Transform _player;

    void Awake()
    {
        AutoLoadVFX();
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
        _cooldown = Random.Range(1f, attackCooldown);
    }

    void Update()
    {
        if (_player == null) return;
        _cooldown -= Time.deltaTime;
        if (_cooldown > 0f) return;

        float dist = Vector2.Distance(transform.position, _player.position);
        if (dist <= attackRange)
            ChooseAttack(dist);

        _cooldown = attackCooldown;
    }

    void ChooseAttack(float dist)
    {
        switch (Random.Range(0, 3))
        {
            case 0: StartCoroutine(AoESmash());    break;
            case 1: StartCoroutine(ChargeAttack()); break;
            case 2: StartCoroutine(GroundSlam());   break;
        }
    }

    // Telegraph pulsante: boss pisca vermelho antes de atacar
    IEnumerator Telegraph(float duration)
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            sr.DOColor(new Color(1f, 0.3f, 0.3f), duration * 0.5f)
              .SetLoops(2, LoopType.Yoyo)
              .SetUpdate(true);
        yield return new WaitForSeconds(duration);
    }

    // ATAQUE 1: Explosão AoE ao redor do boss
    IEnumerator AoESmash()
    {
        yield return StartCoroutine(Telegraph(0.5f));

        SpawnVFX(aoeVFXPrefab, transform.position, Quaternion.identity, scale: 2f, lifetime: 1f);

        var hits = Physics2D.OverlapCircleAll(transform.position, attackRange * 0.8f,
            LayerMask.GetMask("Player"));
        foreach (var h in hits)
            h.GetComponent<PlayerHealth>()?.TakeDamage(attackDamage);
    }

    // ATAQUE 2: Charge em direção ao player
    IEnumerator ChargeAttack()
    {
        if (_player == null) yield break;
        Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
        float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        SpawnVFX(chargeVFXPrefab, transform.position, Quaternion.Euler(0f, 0f, angle), scale: 1.5f, lifetime: 0.5f);
        yield return new WaitForSeconds(0.3f);

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = dir * 15f;
            yield return new WaitForSeconds(0.3f);
            rb.linearVelocity = Vector2.zero;
        }
    }

    // ATAQUE 3: Ground slam — 3 explosões em linha até o player
    IEnumerator GroundSlam()
    {
        if (_player == null) yield break;
        yield return StartCoroutine(Telegraph(0.3f));

        Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
        for (int i = 1; i <= 3; i++)
        {
            Vector3 pos = transform.position + (Vector3)(dir * i * 2f);
            SpawnVFX(explosionPrefab, pos, Quaternion.identity, scale: 1f, lifetime: 1f);

            var hits = Physics2D.OverlapCircleAll(pos, 1.5f, LayerMask.GetMask("Player"));
            foreach (var h in hits)
                h.GetComponent<PlayerHealth>()?.TakeDamage(attackDamage * 0.5f);

            yield return new WaitForSeconds(0.2f);
        }
    }

    static void SpawnVFX(GameObject prefab, Vector3 pos, Quaternion rot, float scale, float lifetime)
    {
        if (prefab == null) return;
        var fx = Instantiate(prefab, pos, rot);
        fx.transform.localScale = Vector3.one * scale;
        Destroy(fx, lifetime);
    }

    void AutoLoadVFX()
    {
        if (aoeVFXPrefab    == null) aoeVFXPrefab    = Resources.Load<GameObject>("VFX/AoE slash orange");
        if (explosionPrefab == null) explosionPrefab  = Resources.Load<GameObject>("VFX/Explosion");
        if (chargeVFXPrefab == null) chargeVFXPrefab  = Resources.Load<GameObject>("VFX/Red energy explosion");
    }
}
