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
    public int[] xpPerLevel = { 0, 100, 220, 360, 520, 700, 900, 1120, 1360, 1620 };
    public int xpPerKill = 80;
    public int xpPerAssist = 30;
    public int xpPerTreeHit = 5;

    [Header("Level Bonuses")]
    // مضاعف الضرر لكل مستوى (1.0 = بدون زيادة)
    public float damageMultiplierPerLevel = 0.12f;
    // مضاعف الصحة لكل مستوى
    public float healthMultiplierPerLevel = 0.08f;
    // مضاعف السرعة لكل مستوى (أقل حتى لا يصبح سريعاً جداً)
    public float speedMultiplierPerLevel = 0.03f;

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

    void Start()
    {
        combat = GetComponent<CharacterCombat>();
        health = GetComponent<Health>();
        movement = GetComponent<PlayerMovement>();

        // حفظ القيم الأساسية
        if (combat != null) baseAttackDamage = combat.attackDamage;
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

        if (current >= 5 && previous < 5)
        {
            Debug.Log(gameObject.name + " reached Level 5! Ultimate ability unlocked!");
        }
    }

    // =========================
    // إضافة XP
    // =========================

    /// <summary>
    /// يُستدعى من السيرفر عند قتل عدو
    /// </summary>
    public void AddKillXP()
    {
        if (!IsServer) return;

        killCount.Value++;
        AddXP(xpPerKill);
    }

    /// <summary>
    /// يُستدعى من السيرفر عند ضرب الشجرة
    /// </summary>
    public void AddTreeHitXP()
    {
        if (!IsServer) return;
        AddXP(xpPerTreeHit);
    }

    /// <summary>
    /// إضافة XP عام
    /// </summary>
    public void AddXP(int amount)
    {
        if (!IsServer) return;

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

                // فتح القدرة الخاصة عند المستوى 5
                if (currentLevel.Value >= 5)
                {
                    ultimateReady.Value = true;
                }
            }
            else
            {
                break;
            }
        }
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

        float levelBonus = (level - 1); // المستوى 1 = بدون مكافأة

        if (combat != null)
        {
            combat.attackDamage = baseAttackDamage * (1f + levelBonus * damageMultiplierPerLevel);
        }

        if (health != null)
        {
            float oldMaxHealth = health.maxHealth;
            health.maxHealth = baseMaxHealth * (1f + levelBonus * healthMultiplierPerLevel);

            // شفاء نسبي عند الترقية
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
        if (GameStateManager.Instance == null || !GameStateManager.Instance.IsPlaying()) return;
        if (Camera.main == null) return;

        // Symmetrical Butterfly-Wing / Celtic Leaf Crest HUD in bottom-left
        float crestW = 280f;
        float crestH = 92f;
        float x = 14f;
        float y = Screen.height - crestH - 14f;

        // Draw organic symmetrical butterfly crest
        EnchantedNatureGUI.DrawButterflyCrest(new Rect(x, y, crestW, crestH), Color.white);

        GUIStyle levelStyle = new GUIStyle(GUI.skin.label);
        levelStyle.fontSize = 13;
        levelStyle.fontStyle = FontStyle.Bold;
        levelStyle.alignment = TextAnchor.MiddleLeft;
        levelStyle.normal.textColor = new Color(0.98f, 0.88f, 0.35f);

        GUIStyle killsStyle = new GUIStyle(GUI.skin.label);
        killsStyle.fontSize = 12;
        killsStyle.fontStyle = FontStyle.Bold;
        killsStyle.alignment = TextAnchor.MiddleRight;
        killsStyle.normal.textColor = new Color(1f, 0.45f, 0.35f);

        // Left Wing: Level
        string lvlText = LanguageSelectionUI.Localize($"⭐ LVL {currentLevel.Value}", ar: $"⭐ مستوى {currentLevel.Value}", ru: $"⭐ УР {currentLevel.Value}", es: $"⭐ NVL {currentLevel.Value}", fr: $"⭐ NIV {currentLevel.Value}");
        GUI.Label(new Rect(x + 22f, y + 10f, 110f, 22f), lvlText, levelStyle);

        // Right Wing: Kills
        string killsText = LanguageSelectionUI.Localize($"⚔️ {killCount.Value} KILLS", ar: $"⚔️ {killCount.Value} قتلات", ru: $"⚔️ {killCount.Value} УБИЙСТВ", es: $"⚔️ {killCount.Value} BAJAS", fr: $"⚔️ {killCount.Value} ÉLIMINATIONS");
        GUI.Label(new Rect(x + crestW - 132f, y + 10f, 110f, 22f), killsText, killsStyle);

        // Symmetrical Leaf XP Progress Curve
        int nextLevelXP = currentLevel.Value < xpPerLevel.Length ? xpPerLevel[currentLevel.Value] : xpPerLevel[xpPerLevel.Length - 1];
        int prevLevelXP = (currentLevel.Value - 1) >= 0 && (currentLevel.Value - 1) < xpPerLevel.Length 
            ? xpPerLevel[currentLevel.Value - 1] : 0;

        float xpProgress = 0f;
        if (nextLevelXP > prevLevelXP)
            xpProgress = (float)(currentXP.Value - prevLevelXP) / (nextLevelXP - prevLevelXP);
        xpProgress = Mathf.Clamp01(xpProgress);

        // XP Fill Bar (Lush Emerald Glow)
        float barX = x + 24f;
        float barY = y + 36f;
        float barW = crestW - 48f;
        float barH = 10f;

        GUI.color = new Color(0.08f, 0.16f, 0.10f, 0.85f);
        GUI.DrawTexture(new Rect(barX, barY, barW, barH), Texture2D.whiteTexture);

        GUI.color = new Color(0.25f, 0.88f, 0.45f);
        GUI.DrawTexture(new Rect(barX, barY, barW * xpProgress, barH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // XP and Ultimate Info
        GUIStyle subStyle = new GUIStyle(GUI.skin.label);
        subStyle.fontSize = 10;
        subStyle.alignment = TextAnchor.MiddleCenter;
        subStyle.normal.textColor = new Color(0.75f, 0.90f, 0.80f);

        string essenceText = LanguageSelectionUI.Localize($"🍃 Essence: {currentXP.Value} / {nextLevelXP} XP", ar: $"🍃 الجوهر: {currentXP.Value} / {nextLevelXP} XP", ru: $"🍃 Эссенция: {currentXP.Value} / {nextLevelXP} XP", es: $"🍃 Esencia: {currentXP.Value} / {nextLevelXP} XP", fr: $"🍃 Essence: {currentXP.Value} / {nextLevelXP} XP");
        GUI.Label(new Rect(x, y + 48f, crestW, 16f), essenceText, subStyle);

        if (currentLevel.Value >= 5)
        {
            bool ready = Time.time >= nextUltimateTime;
            subStyle.normal.textColor = ready ? new Color(0.3f, 0.95f, 1f) : Color.gray;
            string ultReady = LanguageSelectionUI.Localize("✨ [R] NATURE AWAKENING READY! ✨", ar: "✨ [R] صحوة الطبيعة جاهزة! ✨", ru: "✨ [R] ПРОБУЖДЕНИЕ ГОТОВО! ✨", es: "✨ ¡[R] DESPERTAR LISTO! ✨", fr: "✨ [R] ÉVEIL PRÊT ! ✨");
            string ultCharge = LanguageSelectionUI.Localize("⏳ Channelling Ultimate...", ar: "⏳ شحن القدرة الخارقة...", ru: "⏳ Зарядка ульты...", es: "⏳ Canalizando definitiva...", fr: "⏳ Chargement de l'ultime...");
            GUI.Label(new Rect(x, y + 66f, crestW, 18f), ready ? ultReady : ultCharge, subStyle);
        }
        else
        {
            subStyle.normal.textColor = new Color(0.85f, 0.75f, 0.35f);
            string ultLvl5 = LanguageSelectionUI.Localize("🌿 Ultimate Awakes at Level 5 🌿", ar: "🌿 تفتح القدرة الخارقة في المستوى 5 🌿", ru: "🌿 Ульта откроется на 5 уровне 🌿", es: "🌿 La definitiva se activa al nivel 5 🌿", fr: "🌿 L'ultime se débloque au niveau 5 🌿");
            GUI.Label(new Rect(x, y + 66f, crestW, 18f), ultLvl5, subStyle);
        }

        // Top-Right Acorn Purse & Match Stakes (Wager style like 8-Ball Pool)
        if (PlayerProfile.Instance != null)
        {
            float purseW = 230f;
            float purseH = 28f;
            float px = Screen.width - purseW - 12f;
            float py = 44f;

            EnchantedNatureGUI.DrawWoodPlate(new Rect(px, py, purseW, purseH), new Color(0.10f, 0.15f, 0.10f, 0.92f), new Color(0.95f, 0.80f, 0.35f, 0.92f));

            GUIStyle purseStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            purseStyle.normal.textColor = new Color(1f, 0.92f, 0.45f);

            string purseText = LanguageSelectionUI.Localize(
                $"🌰 {PlayerProfile.Instance.Coins}  |  Stake: -60 (Win: +350)",
                ar: $"🌰 {PlayerProfile.Instance.Coins}  |  الرهان: -60 (الفوز: +350)",
                fa: $"🌰 {PlayerProfile.Instance.Coins}  |  هزینه: -۶۰ (پیروزی: +۳۵۰)",
                ru: $"🌰 {PlayerProfile.Instance.Coins}  |  Ставка: -60 (Победа: +350)",
                de: $"🌰 {PlayerProfile.Instance.Coins}  |  Einsatz: -60 (Sieg: +350)",
                it: $"🌰 {PlayerProfile.Instance.Coins}  |  Posta: -60 (Vittoria: +350)",
                tr: $"🌰 {PlayerProfile.Instance.Coins}  |  Bahis: -60 (Zafer: +350)",
                es: $"🌰 {PlayerProfile.Instance.Coins}  |  Apuesta: -60 (Ganar: +350)",
                fr: $"🌰 {PlayerProfile.Instance.Coins}  |  Mise: -60 (Victoire: +350)",
                zh: $"🌰 {PlayerProfile.Instance.Coins}  |  底注: -60 (获胜: +350)",
                ja: $"🌰 {PlayerProfile.Instance.Coins}  |  賭け金: -60 (勝利: +350)",
                pt: $"🌰 {PlayerProfile.Instance.Coins}  |  Aposta: -60 (Vitória: +350)"
            );

            GUI.Label(new Rect(px, py, purseW, purseH), purseText, purseStyle);
        }
    }
}
