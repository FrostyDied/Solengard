using UnityEngine;
using System.Collections.Generic;

public class EnvironmentProp : MonoBehaviour
{
    [HideInInspector] public List<Sprite> sprites      = new();
    [HideInInspector] public bool         hasCollider  = true;
    [HideInInspector] public float        colliderRadius = 0.3f;

    SpriteRenderer _sr;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
    }

    public void Initialize(int seed)
    {
        if (sprites == null || sprites.Count == 0) return;
        var rng    = new System.Random(seed);
        var sprite = sprites[rng.Next(sprites.Count)];
        if (sprite == null) return;

        _sr.sprite = sprite;

        if (hasCollider)
        {
            var col = GetComponent<CircleCollider2D>();
            if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = colliderRadius;
            col.offset = new Vector2(0, -sprite.bounds.size.y * 0.3f);
            var rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            gameObject.layer = LayerMask.NameToLayer("Obstacle");
        }
    }
}
