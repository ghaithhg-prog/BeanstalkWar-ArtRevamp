using UnityEngine;

/// <summary>
/// طاقة الجبار (Titan Power Buff):
/// تمنح للاعب فور قتله لأحد الزعماء:
/// شفاء كامل للصحة + زيادة هائلة في الضرر والسرعة + هالة ذهبية نارية مشتعلة
/// </summary>
public class TitanPowerBuff : MonoBehaviour
{
    public float duration = 60f;
    private float timer = 0f;
    private bool isActive = false;

    private CharacterCombat combat;
    private Health health;
    private PlayerMovement movement;

    private float damageMultiplier = 1.6f; // +60% ضرر
    private float speedMultiplier = 1.35f;  // +35% سرعة

    private Light auraLight;
    private string activeBossName = "";

    public void ActivateBuff(string bossName, float buffDuration = 60f)
    {
        activeBossName = bossName;
        duration = buffDuration;
        timer = duration;

        if (combat == null) combat = GetComponent<CharacterCombat>();
        if (health == null) health = GetComponent<Health>();
        if (movement == null) movement = GetComponent<PlayerMovement>();

        // شفاء كامل فوري لصحة القاتل كهدية لنصره!
        if (health != null && !health.isDead)
        {
            health.currentHealth = health.maxHealth;
        }

        if (!isActive)
        {
            isActive = true;

            // مضاعفة الضرر
            if (combat != null)
            {
                combat.attackDamage *= damageMultiplier;
            }

            // مضاعفة السرعة
            if (movement != null)
            {
                movement.walkSpeed *= speedMultiplier;
                movement.moveSpeed *= speedMultiplier;
            }

            // إضافة هالة ضوئية نارية ذهبية حول البطل
            if (auraLight == null)
            {
                auraLight = gameObject.AddComponent<Light>();
                auraLight.type = LightType.Point;
                auraLight.color = new Color(1f, 0.75f, 0.1f); // ذهبي ساطع
                auraLight.range = 8f;
                auraLight.intensity = 4f;
            }
            else
            {
                auraLight.enabled = true;
            }
        }

        Debug.Log($"🔥 TITAN POWER BUFF ACTIVATED on {gameObject.name} for slaying {bossName}!");
    }

    void Update()
    {
        if (!isActive) return;

        timer -= Time.deltaTime;

        // نبضات ضوء الهالة الذهبية
        if (auraLight != null)
        {
            auraLight.intensity = 3.5f + Mathf.PingPong(Time.time * 4f, 2f);
        }

        if (timer <= 0f)
        {
            RemoveBuff();
        }
    }

    void RemoveBuff()
    {
        if (!isActive) return;
        isActive = false;

        if (combat != null)
        {
            combat.attackDamage /= damageMultiplier;
        }

        if (movement != null)
        {
            movement.walkSpeed /= speedMultiplier;
            movement.moveSpeed /= speedMultiplier;
        }

        if (auraLight != null)
        {
            auraLight.enabled = false;
        }

        Debug.Log($"Titan Power Buff expired on {gameObject.name}.");
    }

    void OnDestroy()
    {
        RemoveBuff();
    }

    void OnGUI()
    {
        if (MatchEndStatsUI.IsGameOverActive) return;
        if (!isActive) return;

        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null && pm.IsOwner)
        {
            // عرض حالة الطاقة على شاشة اللاعب صاحب البف
            float width = 300f;
            float height = 30f;
            float x = (Screen.width - width) / 2f;
            float y = 55f;

            GUI.color = new Color(1f, 0.8f, 0.2f, 0.9f);
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 13;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;

            GUI.Box(new Rect(x, y, width, height), $"🔥 TITAN POWER: +60% DMG | {Mathf.CeilToInt(timer)}s left", style);
            GUI.color = Color.white;
        }
    }
}
