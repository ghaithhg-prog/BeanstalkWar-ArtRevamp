using UnityEngine;
using Unity.Netcode;

/// <summary>
/// نظام الخبرة والمستويات - يُضاف لكل لاعب/بوت
/// كل قتل يمنح XP، وكل مستوى يزيد القوة
/// عند المستوى 5 يحصل اللاعب على قدرة خاصة (ضربة ساحقة أو استدعاء وحش)
/// </summary>
public class PlayerXP : NetworkBehaviour
{
    [Header("XP Settings")]
    public int maxLevel = 10;
    public int[] xpPerLevel = { 0, 100, 240, 420, 640, 900, 1200, 1550, 1950, 2400 };
    public int xpPerKill = 80;
    public int xpPerCombatHit = 7;     // ~14-15 ضربة فعالة تنقلك للمستوى 2 فوراً!
    public int xpPerBossHit = 12;      // تشجيع قتال الزعماء
    public int xpPerDamageTaken = 2;   // مكافأة الصمود في الاشتباك
    public int xpPerTreeHit = 8;
    public int xpPerTreeHeal = 8;
    public int xpPerAssist = 30;

    [Header("Level Bonuses")]
    // مضاعف الضرر لكل مستوى (1.0 = بدون زيادة)
    public float damageMultiplierPerLevel = 0.12f;
    // مضاعف الصحة لكل مستوى
    public float healthMultiplierPerLevel = 0.08f;
    // مضاعف السرعة لكل مستوى (أقل حتى لا يصبح سريعاً جداً)
    public float speedMultiplierPerLevel = 0.03f;
    // تخفيض وقت انتظار الضربة بنسبة 5% لكل مستوى لتسريع الضربات والشعور بالقوة
    public float cooldownReductionPerLevel = 0.05f;

    [Header("Level 5 Ultimate")]
    // ضربة ساحقة - ضرر كبير في منطقة واسعة
    public float ultimateDamage = 150f;
    public float ultimateRadius = 6f;
    public float ultimateCooldown = 45f;
    // استدعاء وحش مساعد (يمكن تفعيله لاحقاً)
    public bool canSummon = false;

    [Header("Network State")]
    public NetworkVariable<int> currentLevel = new NetworkVariable<int>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public int CurrentLevel => currentLevel != null ? currentLevel.Value : 1;

    public NetworkVariable<int> currentXP = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> killCount = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> ultimateReady = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private float nextUltimateTime = 0f;
    private CharacterCombat combat;
    private Health health;
    private PlayerMovement movement;
    private float baseAttackDamage;
    private float baseMaxHealth;
    private float baseMoveSpeed;
    private float baseAttackCooldown = 1.0f;

    void Start()
    {
        combat = GetComponent<CharacterCombat>();
        health = GetComponent<Health>();
        movement = GetComponent<PlayerMovement>();

        // حفظ القيم الأساسية
        if (combat != null)
        {
            baseAttackDamage = combat.attackDamage;
            baseAttackCooldown = combat.attackCooldown;
        }
        if (health != null) baseMaxHealth = health.maxHealth;
        if (movement != null) baseMoveSpeed = movement.walkSpeed;
    }

    public override void OnNetworkSpawn()
    {
        currentLevel.OnValueChanged += OnLevelChanged;
    }

    public override void OnNetworkDespawn()
    {
        currentLevel.OnValueChanged -= OnLevelChanged;
    }

    void OnLevelChanged(int previous, int current)
    {
        ApplyLevelBonuses();

        if (current > previous && IsOwner && GetComponent<BotController>() == null)
        {
            string lvlMsg = LanguageSelectionUI.Localize(
                $"⭐ HERO LEVEL UP! Now Level {current}!",
                ar: $"⭐ ترقية البطل! أصبحت بالمستوى {current}!",
                fa: $"⭐ ارتقای قهرمان! اکنون سطح {current}!",
                ru: $"⭐ НОВЫЙ УРОВЕНЬ {current}!",
                de: $"⭐ HELDEN-STUFENAUFSTIEG! Jetzt Stufe {current}!",
                it: $"⭐ LIVELLO EROE AUMENTATO! Ora Livello {current}!",
                tr: $"⭐ KAHRAMAN SEVİYE ATLADI! Şimdi Seviye {current}!",
                fr: $"⭐ NIVEAU HÉROS SUPÉRIEUR ! Niveau {current} !",
                es: $"⭐ ¡SUBIDA DE NIVEL! ¡Ahora Nivel {current}!",
                zh: $"⭐ 英雄升级！达到第 {current} 级！",
                ja: $"⭐ 英雄レベルアップ！レベル {current}！",
                hi: $"⭐ नायक स्तर बढ़ा! अब स्तर {current}!",
                pt: $"⭐ NÍVEL DE HERÓI SUBIU! Agora Nível {current}!"
            );
            PlayerProgressionAndShopUI.Instance?.ShowNotification(lvlMsg);
        }
    }

    // =========================
    // إضافة XP ديناميكي بالمعركة
    // =========================

    public void AddCombatHitXP(float damage = 0f)
    {
        AddXP(xpPerCombatHit);
    }

    public void AddBossHitXP(float damage = 0f)
    {
        AddXP(xpPerBossHit);
    }

    public void AddDamageTakenXP(float damage = 0f)
    {
        AddXP(xpPerDamageTaken);
    }

    public void AddTreeHealXP()
    {
        AddXP(xpPerTreeHeal);
    }

    /// <summary>
    /// يُستدعى من السيرفر عند قتل عدو
    /// </summary>
    public void AddKillXP()
    {
        if (IsServer)
        {
            killCount.Value++;
            AddXP(xpPerKill);
        }
        else
        {
            RequestAddKillXpRpc();
        }
    }

    [Rpc(SendTo.Server)]
    void RequestAddKillXpRpc()
    {
        killCount.Value++;
        AddXP(xpPerKill);
    }

    /// <summary>
    /// يُستدعى عند ضرب الشجرة
    /// </summary>
    public void AddTreeHitXP()
    {
        AddXP(xpPerTreeHit);
    }

    /// <summary>
    /// إضافة XP عام
    /// </summary>
    public void AddXP(int amount)
    {
        if (amount <= 0) return;

        bool isServerOrSingle = NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsServer;
        if (!isServerOrSingle)
        {
            RequestAddXpRpc(amount);
            return;
        }

        currentXP.Value += amount;

        // التحقق من الترقية
        while (currentLevel.Value < maxLevel)
        {
            int nextLevel = currentLevel.Value; // index في المصفوفة (المستوى الحالي = index)
            if (nextLevel >= xpPerLevel.Length) break;

            if (currentXP.Value >= xpPerLevel[nextLevel])
            {
                currentLevel.Value++;
                ApplyLevelBonuses();
                PlayLevelUpFxRpc(currentLevel.Value);
                Debug.Log(gameObject.name + " leveled up to " + currentLevel.Value + "!");
            }
            else
            {
                break;
            }
        }
    }

    [Rpc(SendTo.Server)]
    void RequestAddXpRpc(int amount)
    {
        AddXP(amount);
    }

    // =========================
    // تطبيق مكافآت المستوى
    // =========================

    void ApplyLevelBonuses()
    {
        int level = currentLevel.Value;

        // إعادة حساب القيم الأساسية + المكافأة
        if (combat == null) combat = GetComponent<CharacterCombat>();
        if (health == null) health = GetComponent<Health>();
        if (movement == null) movement = GetComponent<PlayerMovement>();

        if (baseAttackDamage <= 0 && combat != null) baseAttackDamage = combat.attackDamage;
        if (baseMaxHealth <= 0 && health != null) baseMaxHealth = health.maxHealth;
        if (baseMoveSpeed <= 0 && movement != null) baseMoveSpeed = movement.walkSpeed;
        if (baseAttackCooldown <= 0 && combat != null) baseAttackCooldown = combat.attackCooldown;

        float levelBonus = (level - 1); // المستوى 1 = بدون مكافأة

        if (combat != null)
        {
            combat.attackDamage = baseAttackDamage * (1f + levelBonus * damageMultiplierPerLevel);
            // تسريع الضربات (-5% من وقت الانتظار لكل مستوى)
            combat.attackCooldown = Mathf.Max(0.25f, baseAttackCooldown * (1f - levelBonus * cooldownReductionPerLevel));
        }

        if (health != null)
        {
            float oldMaxHealth = health.maxHealth;
            health.maxHealth = baseMaxHealth * (1f + levelBonus * healthMultiplierPerLevel);

            // شفاء فوري لمقدار الصحة المكتسبة عند الترقية!
            if (health.maxHealth > oldMaxHealth)
            {
                float healAmount = health.maxHealth - oldMaxHealth;
                health.currentHealth = Mathf.Min(health.currentHealth + healAmount, health.maxHealth);
            }
        }

        if (movement != null)
        {
            movement.walkSpeed = baseMoveSpeed * (1f + levelBonus * speedMultiplierPerLevel);
        }
    }

    /// <summary>
    /// الحصول على مضاعف الضرر الحالي
    /// </summary>
    public float GetDamageMultiplier()
    {
        return 1f + (currentLevel.Value - 1) * damageMultiplierPerLevel;
    }

    // =========================
    // القدرة الخاصة (المستوى 5+)
    // =========================

    void Update()
    {
        if (!IsOwner) return;

        // القدرة الخاصة - زر R
        if (currentLevel.Value >= 5 && Input.GetKeyDown(KeyCode.R))
        {
            TryActivateUltimate();
        }
    }

    public bool TryActivateUltimate()
    {
        if (!IsOwner) return false;
        if (currentLevel.Value < 5) return false;
        if (Time.time < nextUltimateTime) return false;

        nextUltimateTime = Time.time + ultimateCooldown;
        RequestUltimateRpc();
        return true;
    }

    public float GetUltimateCooldownRatio()
    {
        if (currentLevel.Value < 5) return 1f;
        float remaining = nextUltimateTime - Time.time;
        if (remaining <= 0f) return 0f;
        return Mathf.Clamp01(remaining / Mathf.Max(0.1f, ultimateCooldown));
    }

    public float GetRemainingUltimateCooldown()
    {
        return Mathf.Max(0f, nextUltimateTime - Time.time);
    }

    [Rpc(SendTo.Server)]
    void RequestUltimateRpc()
    {
        if (currentLevel.Value < 5) return;

        Vector3 blastOrigin = transform.position;

        // ضربة ساحقة تضرب كل الأعداء في المنطقة
        Collider[] hits = Physics.OverlapSphere(blastOrigin, ultimateRadius);

        PlayerMovement myMovement = GetComponent<PlayerMovement>();
        if (myMovement == null) return;

        foreach (Collider col in hits)
        {
            if (col.gameObject == gameObject) continue;

            CharacterCombat otherCombat = col.GetComponent<CharacterCombat>();
            if (otherCombat != null && otherCombat.team != myMovement.team)
            {
                Health otherHealth = col.GetComponent<Health>();
                if (otherHealth != null && !otherHealth.isDead)
                {
                    // ضرر ساحق + دفع
                    otherHealth.TakeDamage(ultimateDamage * GetDamageMultiplier(), gameObject);

                    Rigidbody rb = col.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 pushDir = (col.transform.position - blastOrigin).normalized;
                        pushDir.y = 0.5f;
                        rb.AddForce(pushDir * 20f, ForceMode.Impulse);
                    }

                    UnityEngine.AI.NavMeshAgent agent = col.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null && agent.isOnNavMesh)
                    {
                        Vector3 pushDir = (col.transform.position - blastOrigin).normalized;
                        agent.Move(pushDir * 5f);
                    }
                }
            }

            // يمكنه أيضاً ضرب الشجرة بقوة كبيرة
            TreeGrowth tree = col.GetComponent<TreeGrowth>();
            if (tree != null)
            {
                float treeDamage = ultimateDamage * GetDamageMultiplier();
                if (myMovement.team == PlayerMovement.TeamType.Destroyer)
                    tree.TakeDamage(treeDamage, gameObject);
                else
                    tree.Heal(treeDamage * 0.5f); // العلاج أقل من الضرر للتوازن
            }
        }

        PlayUltimateFxRpc(blastOrigin);
        Debug.Log(gameObject.name + " used Ultimate Ability!");
    }

    // =========================
    // Visual FX
    // =========================

    [Rpc(SendTo.ClientsAndHost)]
    void PlayLevelUpFxRpc(int newLevel)
    {
        // تأثير ترقية بسيط - تغيير حجم مؤقت
        StartCoroutine(LevelUpVisual());
    }

    System.Collections.IEnumerator LevelUpVisual()
    {
        Vector3 originalScale = transform.localScale;
        float timer = 0.5f;

        while (timer > 0)
        {
            float pulse = 1f + Mathf.Sin(timer * 20f) * 0.1f;
            transform.localScale = originalScale * pulse;
            timer -= Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    [Rpc(SendTo.ClientsAndHost)]
    void PlayUltimateFxRpc(Vector3 position)
    {
        // تأثير بصري بسيط للضربة الساحقة
        Debug.Log("ULTIMATE BLAST at " + position);
    }

    // =========================
    // UI - عرض المستوى والـ XP
    // =========================

    void OnGUI()
    {
        if (!IsOwner) return;
        if (MatchEndStatsUI.IsGameOverActive) return;
        if (GameStateManager.Instance == null || !GameStateManager.Instance.IsPlaying()) return;
        if (Camera.main == null) return;

        // =========================================================================
        // AAA Mobile MOBA / Battle Royale Style (Free Fire & LoL: Wild Rift)
        // 1. Bottom-Left Joystick Quadrant is 100% CLEAR: Zero overlays on thumb movement!
        // 2. Sleek, Translucent Level & XP Pill positioned cleanly at Top-Left beneath Pause.
        // =========================================================================

        float pillW = 215f;
        float pillH = 26f;
        float x = 16f;
        float y = 52f; // Sits neatly under [⚙️ إيقاف (ESC)]

        Rect pillRect = new Rect(x, y, pillW, pillH);

        // 1. Translucent Obsidian & Forest Glass Background
        GUI.color = new Color(0.04f, 0.08f, 0.05f, 0.72f);
        GUI.DrawTexture(pillRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Crisp Fine Emerald & Gold Rim
        EnchantedNatureGUI.DrawCrispBorder(pillRect, new Color(0.35f, 0.88f, 0.50f, 0.85f), 1.2f);

        // Symmetrical XP Progress Calculation
        int nextLevelXP = currentLevel.Value < xpPerLevel.Length ? xpPerLevel[currentLevel.Value] : xpPerLevel[xpPerLevel.Length - 1];
        int prevLevelXP = (currentLevel.Value - 1) >= 0 && (currentLevel.Value - 1) < xpPerLevel.Length 
            ? xpPerLevel[currentLevel.Value - 1] : 0;

        float xpProgress = 0f;
        if (nextLevelXP > prevLevelXP)
            xpProgress = (float)(currentXP.Value - prevLevelXP) / (nextLevelXP - prevLevelXP);
        xpProgress = Mathf.Clamp01(xpProgress);

        // A. Level Badge (⭐ Lv.X)
        GUIStyle levelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        levelStyle.normal.textColor = new Color(1f, 0.88f, 0.35f);

        string lvlText = LanguageSelectionUI.Localize(
            $"⭐ Lv.{currentLevel.Value}",
            ar: $"⭐ مست.{currentLevel.Value}",
            fa: $"⭐ سط.{currentLevel.Value}",
            ru: $"⭐ Ур.{currentLevel.Value}",
            es: $"⭐ Nvl.{currentLevel.Value}",
            fr: $"⭐ Niv.{currentLevel.Value}"
        );
        GUI.Label(new Rect(x + 6f, y + 2f, 56f, pillH - 4f), lvlText, levelStyle);

        // B. Slender Emerald XP Fill Bar
        float barX = x + 64f;
        float barY = y + 9f;
        float barW = 92f;
        float barH = 8f;

        GUI.color = new Color(0.08f, 0.16f, 0.10f, 0.90f);
        GUI.DrawTexture(new Rect(barX, barY, barW, barH), Texture2D.whiteTexture);

        GUI.color = new Color(0.25f, 0.92f, 0.45f, 0.95f);
        GUI.DrawTexture(new Rect(barX, barY, barW * xpProgress, barH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // C. Kills Indicator (⚔️ X)
        GUIStyle killsStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight
        };
        killsStyle.normal.textColor = new Color(1f, 0.45f, 0.35f);

        string killsText = LanguageSelectionUI.Localize(
            $"⚔️ {killCount.Value}",
            ar: $"⚔️ {killCount.Value}",
            fa: $"⚔️ {killCount.Value}",
            ru: $"⚔️ {killCount.Value}",
            es: $"⚔️ {killCount.Value}",
            fr: $"⚔️ {killCount.Value}"
        );
        GUI.Label(new Rect(x + pillW - 56f, y + 2f, 50f, pillH - 4f), killsText, killsStyle);

        // D. Sleek Compact Stat Buffs Ribbon directly under the pill
        int dmgPct = Mathf.RoundToInt((currentLevel.Value - 1) * damageMultiplierPerLevel * 100f);
        int hpPct = Mathf.RoundToInt((currentLevel.Value - 1) * healthMultiplierPerLevel * 100f);
        int spdPct = Mathf.RoundToInt((currentLevel.Value - 1) * speedMultiplierPerLevel * 100f);
        int cdPct = Mathf.RoundToInt((currentLevel.Value - 1) * cooldownReductionPerLevel * 100f);

        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        subStyle.normal.textColor = new Color(0.85f, 0.95f, 0.75f, 0.90f);

        string statsSummary = LanguageSelectionUI.Localize(
            $"⚔️+{dmgPct}%  🛡️+{hpPct}%  ⚡+{spdPct}%  ⏱️-{cdPct}%",
            ar: $"⚔️+{dmgPct}%  🛡️+{hpPct}%  ⚡+{spdPct}%  ⏱️+{cdPct}%",
            fa: $"⚔️+{dmgPct}%  🛡️+{hpPct}%  ⚡+{spdPct}%",
            ru: $"⚔️+{dmgPct}%  🛡️+{hpPct}%  ⚡+{spdPct}%",
            es: $"⚔️+{dmgPct}%  🛡️+{hpPct}%  ⚡+{spdPct}%",
            fr: $"⚔️+{dmgPct}%  🛡️+{hpPct}%  ⚡+{spdPct}%"
        );
        GUI.Label(new Rect(x + 2f, y + pillH + 1f, pillW + 60f, 14f), statsSummary, subStyle);
    }
}
