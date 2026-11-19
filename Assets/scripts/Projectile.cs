using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 20f;
    public float damage = 15f;
    public PlayerMovement.TeamType team;
    public GameObject owner;

    [Header("AoE Explosion Settings")]
    public bool isExplosive = false;
    public float aoeRadius = 4.8f;
    public float splashDamageMultiplier = 0.85f;
    public Color explosionColor = new Color(0.22f, 0.75f, 1.0f);

    private bool hasExploded = false;

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null || hasExploded) return;
        if (owner != null && (other.gameObject == owner || other.transform.IsChildOf(owner.transform)))
            return;

        // إذا كان المقذوف سحرياً متفجراً (كقذيفة الساحر زارفون)، ينفجر ويصيب كل شيء في محيطه
        if (isExplosive)
        {
            Explode(transform.position);
            return;
        }

        TreeGrowth tree = other.GetComponent<TreeGrowth>();
        if (tree == null) tree = other.GetComponentInParent<TreeGrowth>();
        if (tree == null && (other.CompareTag("Tree") || other.name.Contains("Trunk") || other.name.Contains("Tree")))
        {
            tree = TreeGrowth.Instance;
        }
        if (tree != null)
        {
            float levelMultiplier = TreeGrowth.GetHeightMultiplier(transform.position);
            float finalAmount = damage * levelMultiplier;

            if (team == PlayerMovement.TeamType.Destroyer)
                tree.TakeDamage(finalAmount, owner);
            else
                tree.Heal(finalAmount);

            Destroy(gameObject);
            return;
        }

        Health health = other.GetComponent<Health>();
        if (health == null) health = other.GetComponentInParent<Health>();
        if (health != null)
        {
            CharacterCombat otherCombat = health.GetComponent<CharacterCombat>() ?? health.GetComponentInParent<CharacterCombat>() ?? other.GetComponent<CharacterCombat>() ?? other.GetComponentInParent<CharacterCombat>();
            if (otherCombat != null && otherCombat.team != this.team && !health.isDead)
            {
                health.TakeDamage(damage, owner);
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// انفجار أركيني كاسح بمحيط AoE يصيب كل الأعداء والشجرة في نطاق الانفجار
    /// </summary>
    public void Explode(Vector3 hitPoint)
    {
        if (hasExploded) return;
        hasExploded = true;

        // 1. توليد التأثير البصري الملحمي للانفجار
        SpawnExplosionVisual(hitPoint);

        // 2. فحص الأهداف والموجة التدميرية داخل محيط الانفجار (AoE)
        Collider[] hits = Physics.OverlapSphere(hitPoint, aoeRadius);
        bool treeAffected = false;

        foreach (Collider col in hits)
        {
            if (col == null) continue;
            if (owner != null && (col.gameObject == owner || col.transform.IsChildOf(owner.transform)))
                continue;

            // إصابة الشجرة المقدسة إذا كانت ضمن نطاق الانفجار
            if (!treeAffected)
            {
                TreeGrowth tree = col.GetComponent<TreeGrowth>() ?? col.GetComponentInParent<TreeGrowth>();
                if (tree == null && (col.CompareTag("Tree") || col.name.Contains("Trunk") || col.name.Contains("Tree")))
                    tree = TreeGrowth.Instance;

                if (tree != null)
                {
                    treeAffected = true;
                    float levelMultiplier = TreeGrowth.GetHeightMultiplier(hitPoint);
                    float finalAmount = damage * levelMultiplier * splashDamageMultiplier;

                    if (team == PlayerMovement.TeamType.Destroyer)
                        tree.TakeDamage(finalAmount, owner);
                    else
                        tree.Heal(finalAmount);
                }
            }

            // إصابة أي عدو أو بوت أو لاعب معادٍ في نطاق الانفجار
            Health health = col.GetComponent<Health>() ?? col.GetComponentInParent<Health>();
            if (health != null && !health.isDead)
            {
                CharacterCombat otherCombat = health.GetComponent<CharacterCombat>() ?? col.GetComponent<CharacterCombat>();
                if (otherCombat != null && otherCombat.team != this.team)
                {
                    float dist = Vector3.Distance(hitPoint, col.transform.position);
                    float falloff = Mathf.Clamp01(1f - (dist / (aoeRadius * 1.25f)));
                    float splashDmg = damage * Mathf.Max(0.55f, falloff);
                    health.TakeDamage(splashDmg, owner);
                }
            }
        }

        // إخفاء المقذوف وتدميره
        Destroy(gameObject);
    }

    private void SpawnExplosionVisual(Vector3 pos)
    {
        GameObject fxObj = new GameObject("Arcane_AoE_Explosion");
        fxObj.transform.position = pos;

        // 1. كرة الطاقة المتفجرة المتوسعة
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(fxObj.transform, false);
        sphere.transform.localScale = Vector3.one * 0.8f;
        Destroy(sphere.GetComponent<Collider>());

        Renderer rend = sphere.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Mobile/Particles/Additive") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = explosionColor;
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", explosionColor);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", explosionColor * 2.5f);
            }
            rend.material = mat;
        }

        // 2. ضوء متوهج للحظة الانفجار
        Light blastLight = fxObj.AddComponent<Light>();
        blastLight.type = LightType.Point;
        blastLight.color = explosionColor;
        blastLight.range = aoeRadius * 1.8f;
        blastLight.intensity = 4.5f;

        // 3. محرك توسع الانفجار وتلاشيه التدريجي
        ExplosionFade fade = fxObj.AddComponent<ExplosionFade>();
        fade.maxRadius = aoeRadius;
        fade.targetLight = blastLight;
    }

    void Start()
    {
        Destroy(gameObject, 5f);
    }
}

/// <summary>
/// تأثير بصري متلاشٍ للانفجار السحري
/// </summary>
public class ExplosionFade : MonoBehaviour
{
    public float maxRadius = 4.8f;
    public Light targetLight;
    private float timer = 0f;
    private float duration = 0.45f;
    private Transform sphereTr;

    void Start()
    {
        if (transform.childCount > 0)
            sphereTr = transform.GetChild(0);
        Destroy(gameObject, duration);
    }

    void Update()
    {
        timer += Time.deltaTime;
        float progress = Mathf.Clamp01(timer / duration);

        if (sphereTr != null)
        {
            float s = Mathf.Lerp(0.8f, maxRadius * 2.2f, Mathf.Sin(progress * Mathf.PI * 0.5f));
            sphereTr.localScale = Vector3.one * s;
        }

        if (targetLight != null)
        {
            targetLight.intensity = Mathf.Lerp(4.5f, 0f, progress);
        }
    }
}
