using UnityEngine;
using System.Collections;
using TMPro;

public class Health : MonoBehaviour
{
    public TextMeshProUGUI respawnText;

    public float maxHealth = 200f;
    public float currentHealth;
    [System.NonSerialized]
    public float ghostHealth = -1f;
    public bool isDead = false;
    public static float damageFlashTimer = 0f;
    public static void TriggerDamageFlash(float intensity = 0.5f) { damageFlashTimer = Mathf.Max(damageFlashTimer, intensity); }

    private bool initialized = false;

    // تتبع آخر مهاجم لمنح XP عند القتل
    [HideInInspector]
    public GameObject lastAttacker;

    void Awake()
    {
        LevelBoss boss = GetComponent<LevelBoss>();
        if (boss != null)
        {
            float bossHp = boss.GetExpectedMaxHealth();
            maxHealth = bossHp;
            currentHealth = bossHp;
        }
        else if (currentHealth <= 0f)
        {
            currentHealth = maxHealth;
        }
        ghostHealth = currentHealth;
        initialized = true;
    }

    void Start()
    {
        StartCoroutine(LateInitialize());
    }

    IEnumerator LateInitialize()
    {
        yield return null;

        LevelBoss boss = GetComponent<LevelBoss>();
        if (boss != null)
        {
            float bossHp = boss.GetExpectedMaxHealth();
            maxHealth = bossHp;
            if (currentHealth < 1000f || currentHealth <= 0f)
            {
                currentHealth = bossHp;
            }
            isDead = false;
            initialized = true;
        }
        else if (!initialized || currentHealth <= 0f)
        {
            initialized = true;
            if (currentHealth <= 0f) currentHealth = maxHealth;
            ghostHealth = currentHealth;
            isDead = false;
        }
        if (ghostHealth <= 0f) ghostHealth = currentHealth;

        // ✅ ابحث عن RespawnText وأخفه
        if (respawnText == null)
        {
            GameObject obj = GameObject.Find("RespawnText");
            if (obj != null)
                respawnText = obj.GetComponent<TextMeshProUGUI>();
        }

        // ✅ تأكد أنه مخفي دائماً عند البداية
        if (respawnText != null)
            respawnText.gameObject.SetActive(false);
    }

    private float spawnInvulnerabilityTimer = 0f;

    void Update()
    {
        if (spawnInvulnerabilityTimer > 0f)
        {
            spawnInvulnerabilityTimer -= Time.deltaTime;
        }

        // Smoothly drain ghost damage bar towards current health (Free Fire damage lag effect)
        if (ghostHealth < 0f) ghostHealth = currentHealth;
        if (ghostHealth > currentHealth)
        {
            ghostHealth = Mathf.MoveTowards(ghostHealth, currentHealth, Time.deltaTime * 65f);
        }
        else if (ghostHealth < currentHealth)
        {
            ghostHealth = currentHealth;
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead || spawnInvulnerabilityTimer > 0f) return;
        if (!initialized)
        {
            initialized = true;
            if (currentHealth <= 0f) currentHealth = maxHealth;
        }

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        PlayerMovement pmDmg = GetComponent<PlayerMovement>();
        if (pmDmg != null && pmDmg.IsOwner && GetComponent<BotController>() == null) TriggerDamageFlash(0.45f);

        if (currentHealth <= 0)
            Die();
    }

    /// <summary>
    /// TakeDamage مع تحديد المهاجم لمنح XP
    /// </summary>
    public void TakeDamage(float amount, GameObject attacker)
    {
        if (attacker != null)
        {
            lastAttacker = attacker;
            MatchEndStatsUI.Instance?.RegisterDamage(attacker, amount);

            BotController bot = GetComponent<BotController>();
            if (bot != null && bot.enabled)
            {
                bot.OnTakeDamageFrom(attacker, amount);
            }

            // منح خبرة الاشتباك الفعال للمهاجم (أبطال أو زعماء)
            PlayerXP attackerXP = attacker.GetComponent<PlayerXP>();
            if (attackerXP != null)
            {
                LevelBoss bossTarget = GetComponent<LevelBoss>();
                if (bossTarget != null)
                {
                    attackerXP.AddBossHitXP(amount);
                }
                else
                {
                    attackerXP.AddCombatHitXP(amount);
                }
            }

            // منح خبرة الصمود وتلقي الضربات للمتلقي
            PlayerXP victimXP = GetComponent<PlayerXP>();
            if (victimXP != null)
            {
                victimXP.AddDamageTakenXP(amount);
            }
        }
        TakeDamage(amount);
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log(gameObject.name + " died");

        // منح XP وتسجيل القتل في الإحصائيات
        if (lastAttacker != null)
        {
            MatchEndStatsUI.Instance?.RegisterKill(lastAttacker, gameObject);
            PlayerXP killerXP = lastAttacker.GetComponent<PlayerXP>();
            if (killerXP != null)
            {
                killerXP.AddKillXP();
            }
        }

        // إسقاط الكريستالة فوراً إذا كان هذا الكائن يحملها
        if (CrystalItem.Instance != null && CrystalItem.Instance.IsCarrier(gameObject))
        {
            CrystalItem.Instance.DropCrystalServer();
        }

        // إذا كان هذا زعيماً، نُفعّل حدث هزيمة الزعيم فوراً لكسر الختم وتوزيع الغنائم
        LevelBoss boss = GetComponent<LevelBoss>();
        if (boss != null)
        {
            CharacterCombat bCombat = GetComponent<CharacterCombat>();
            if (bCombat != null) bCombat.enabled = false;

            UnityEngine.AI.NavMeshAgent bAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (bAgent != null)
            {
                if (bAgent.isOnNavMesh) bAgent.isStopped = true;
                bAgent.enabled = false;
            }

            boss.OnBossDefeated();
            return;
        }

        // تعطيل الحركة والقتال لجميع الكائنات (لاعب أو بوت)
        PlayerMovement player = GetComponent<PlayerMovement>();
        if (player != null)
        {
            player.ResetFallState();
            player.enabled = false;
        }

        BotController bot = GetComponent<BotController>();
        if (bot != null) bot.enabled = false;

        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        CharacterCombat combat = GetComponent<CharacterCombat>();
        if (combat != null) combat.enabled = false;

        // تجميد الحركة الفيزيائية فوراً لمنع السقوط من الأرض أو التهاوي في الفراغ
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true; // حماية تامة من اختراق الأرض
        }

        // إبقاء الكوليدر مفعلاً حتى تستقر الجثة فوق سطح الأرض
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        // سقوط الجثة أرضاً في مكان موتها بدلاً من الاختفاء (أو تشغيل أنيميشن الموت الحقيقي لـ Erika Archer)
        Transform meshTr = transform.Find("Character_3D_Mesh");
        if (meshTr != null)
        {
            CharacterVisualAnimator visualAnim = meshTr.GetComponent<CharacterVisualAnimator>();
            if (visualAnim != null && visualAnim.UsesRealDeathAnimation)
            {
                visualAnim.TriggerDeathAnimation();
                meshTr.localRotation = Quaternion.identity;
                meshTr.localPosition = new Vector3(0f, -0.95f, 0f);
            }
            else
            {
                meshTr.localRotation = Quaternion.Euler(-80f, 0f, 0f);
                meshTr.localPosition = new Vector3(0f, -0.55f, 0f);
            }
        }

        StartCoroutine(RespawnCoroutine());
    }

    public float currentRespawnCountdown { get; private set; } = 0f;
    public static bool IsLocalPlayerDead { get; private set; } = false;

    IEnumerator RespawnCoroutine()
    {

        PlayerMovement player = GetComponent<PlayerMovement>();
        BotController bot = GetComponent<BotController>();
        bool isHumanOwner = player != null && player.IsOwner && bot == null;

        float respawnTime = 10f; // 10 ثوانٍ كما طلب المستخدم تماماً

        // إظهار وإعداد عداد العودة للحياة للاعب الحقيقي
        if (isHumanOwner)
        {
            IsLocalPlayerDead = true;
            currentRespawnCountdown = 10f;

            if (respawnText == null)
            {
                GameObject obj = GameObject.Find("RespawnText");
                if (obj != null)
                    respawnText = obj.GetComponent<TextMeshProUGUI>();
            }

            if (respawnText != null)
                respawnText.gameObject.SetActive(true);
        }

        while (respawnTime > 0f)
        {
            if (isHumanOwner)
            {
                currentRespawnCountdown = respawnTime;
                if (respawnText != null)
                {
                    int secInt = Mathf.CeilToInt(respawnTime);
                    respawnText.text = LanguageSelectionUI.Localize(
                        $"Respawning in {secInt}s",
                        ar: $"العودة للحياة بعد {secInt} ثوانٍ",
                        fa: $"بازگشت به زندگی پس از {secInt} ثانیه",
                        de: $"Wiederbelebung in {secInt}s",
                        it: $"Rientro in gioco in {secInt}s",
                        tr: $"{secInt}sn içinde yeniden doğuş",
                        ru: $"Возрождение через {secInt} с",
                        fr: $"Réapparition dans {secInt}s",
                        es: $"Reapareciendo en {secInt}s",
                        hi: $"{secInt} सेकंड में पुनर्जन्म",
                        zh: $"{secInt} 秒后复活",
                        ja: $"{secInt} 秒後に復活",
                        pt: $"Ressuscitando em {secInt}s"
                    );
                }
            }

            yield return new WaitForSeconds(0.1f);
            respawnTime -= 0.1f;
        }

        currentRespawnCountdown = 0f;

        // تحديد فريق الكائن بدقة (سواء كان لاعباً حقيقياً أو بوتاً ذكياً)
        PlayerMovement.TeamType currentTeam = PlayerMovement.TeamType.Protector;
        if (player != null)
            currentTeam = player.team;
        else if (bot != null)
            currentTeam = bot.team;

        // استخراج نقطة بداية الفريق
        Vector3 targetSpawnPos = transform.position;
        Transform spawnPoint = null;
        if (SpawnManager.Instance != null)
        {
            spawnPoint = SpawnManager.Instance.GetSpawnPoint(currentTeam);
            if (spawnPoint != null)
            {
                targetSpawnPos = spawnPoint.position;
            }
        }

        // إعادة ضبط وضعية الموديل ثلاثي الأبعاد ووقوفه مجدداً
        Transform meshTr = transform.Find("Character_3D_Mesh");
        if (meshTr != null)
        {
            meshTr.localRotation = Quaternion.identity;
            meshTr.localPosition = new Vector3(0f, -0.95f, 0f);
            CharacterVisualAnimator visualAnim = meshTr.GetComponent<CharacterVisualAnimator>();
            if (visualAnim != null)
            {
                visualAnim.ResetAfterRespawn();
            }
        }

        // إعادة تفعيل وحركة البوت أو اللاعب
        if (bot != null)
        {
            bot.OnRespawn(targetSpawnPos);
        }
        else
        {
            transform.position = targetSpawnPos + Vector3.up * 0.25f;
            if (spawnPoint != null) transform.rotation = spawnPoint.rotation;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (player != null)
            {
                player.ResetFallState();
                player.enabled = true;
                player.ResetFallState();
            }
        }

        currentHealth = maxHealth;
        ghostHealth = maxHealth;
        isDead = false;
        IsLocalPlayerDead = false;
        currentRespawnCountdown = 0f;
        spawnInvulnerabilityTimer = 2.5f; // فترة حصانة مؤقتة 2.5 ثانية عند العودة للحياة لمنع الموت المفاجئ

        CharacterCombat combatComp = GetComponent<CharacterCombat>();
        if (combatComp != null) combatComp.enabled = true;

        if (isHumanOwner && respawnText != null)
            respawnText.gameObject.SetActive(false);
    }

    // =========================
    // شريط صحة وشاشة الموت
    // =========================

    void OnGUI()
    {
        if (MatchEndStatsUI.IsGameOverActive) return;
        PlayerMovement player = GetComponent<PlayerMovement>();
        BotController bot = GetComponent<BotController>();
        bool isHumanOwner = player != null && player.IsOwner && bot == null;
        if (damageFlashTimer > 0f && isHumanOwner)
        {
            damageFlashTimer -= Time.deltaTime;
            float alpha = Mathf.Clamp01(damageFlashTimer);
            float borderThickness = 18f;
            GUI.color = new Color(0.9f, 0.05f, 0.05f, alpha * 0.45f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, borderThickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, Screen.height - borderThickness, Screen.width, borderThickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, 0, borderThickness, Screen.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(Screen.width - borderThickness, 0, borderThickness, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        if (isDead)
        {
            if (isHumanOwner && currentRespawnCountdown > 0f)
            {
                DrawDeathRespawnScreen();
            }
            return;
        }

        if (Camera.main == null) return;

        // إذا كان زعيماً، LevelBoss يرسم شريط الزعيم الضخم
        if (GetComponent<LevelBoss>() != null) return;

        // استخراج الفريق واللون المميز
        PlayerMovement pm = GetComponent<PlayerMovement>();
        CharacterCombat combat = GetComponent<CharacterCombat>();
        PlayerMovement.TeamType currentTeam = PlayerMovement.TeamType.Protector;

        if (pm != null) currentTeam = pm.team;
        else if (combat != null) currentTeam = combat.team;

        Color teamColor = currentTeam == PlayerMovement.TeamType.Protector 
            ? new Color(0.2f, 0.75f, 1f) 
            : new Color(1f, 0.28f, 0.15f);

        string teamLabel = currentTeam == PlayerMovement.TeamType.Protector 
            ? LanguageSelectionUI.Localize(
                "🛡️ PROTECTOR",
                ar: "🛡️ حامي الشجرة",
                fa: "🛡️ محافظ درخت",
                de: "🛡️ BESCHÜTZER",
                it: "🛡️ PROTETTORE",
                tr: "🛡️ KORUYUCU",
                ru: "🛡️ ЗАЩИТНИК",
                fr: "🛡️ PROTECTEUR",
                es: "🛡️ PROTECTOR",
                hi: "🛡️ रक्षक",
                zh: "🛡️ 守护者",
                ja: "🛡️ 守護者",
                pt: "🛡️ PROTETOR"
            )
            : LanguageSelectionUI.Localize(
                "⚔️ DESTROYER",
                ar: "⚔️ مدمر الشجرة",
                fa: "⚔️ نابودکننده",
                de: "⚔️ ZERSTÖRER",
                it: "⚔️ DISTRUTTORE",
                tr: "⚔️ YOK EDİCİ",
                ru: "⚔️ РАЗРУШИТЕЛЬ",
                fr: "⚔️ DESTRUCTEUR",
                es: "⚔️ DESTRUCTOR",
                hi: "⚔️ विनाशक",
                zh: "⚔️ 破坏者",
                ja: "⚔️ 破壊者",
                pt: "⚔️ DESTRUIDOR"
            );

        if (isHumanOwner)
        {
            // 1. Free Fire AAA Style Health HUD at the bottom center of the screen
            DrawFreeFirePlayerHealthHUD(currentTeam, teamColor, teamLabel);

            // 2. Curved Health Arc covering 45% of the circle from the bottom around player base
            DrawCurvedBaseArc(currentTeam, teamColor, teamLabel, isHumanOwner: true);
        }
        else
        {
            // For normal in-world characters and bots: 45% curved bottom health arc at character base
            DrawCurvedBaseArc(currentTeam, teamColor, teamLabel, isHumanOwner: false);
        }
    }

    // =========================================================================
    // Free Fire AAA Player Health HUD at Bottom of the Screen
    // =========================================================================
    void DrawFreeFirePlayerHealthHUD(PlayerMovement.TeamType currentTeam, Color teamColor, string teamLabel)
    {
        float sw = Screen.width;
        float sh = Screen.height;

        float hudW = Mathf.Clamp(sw * 0.36f, 380f, 480f);
        float hudH = 34f;
        float hudX = (sw - hudW) * 0.5f;
        float hudY = sh - 52f; // Bottom center of screen, exactly like Free Fire

        Rect hudRect = new Rect(hudX, hudY, hudW, hudH);

        // 1. Dark Translucent Carbon Plate with Chamfered Edges
        GUI.color = new Color(0.04f, 0.06f, 0.09f, 0.92f);
        GUI.DrawTexture(hudRect, Texture2D.whiteTexture);

        // Neon Rim in Team Color
        GUI.color = teamColor;
        GUI.DrawTexture(new Rect(hudX, hudY, hudW, 1.5f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(hudX, hudY + hudH - 1.5f, hudW, 1.5f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(hudX, hudY, 2.5f, hudH), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(hudX + hudW - 2.5f, hudY, 2.5f, hudH), Texture2D.whiteTexture);

        // Gold Corner Accent Trim
        GUI.color = new Color(1.0f, 0.85f, 0.35f, 0.9f);
        GUI.DrawTexture(new Rect(hudX, hudY, 5f, 3f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(hudX + hudW - 5f, hudY, 5f, 3f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(hudX, hudY + hudH - 3f, 5f, 3f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(hudX + hudW - 5f, hudY + hudH - 3f, 5f, 3f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 2. Hero Information & Class Badge
        string heroName = "";
        CharacterCombat combat = GetComponent<CharacterCombat>();
        if (combat != null && combat.characterData != null)
        {
            heroName = combat.characterData.characterName;
        }
        else
        {
            heroName = gameObject.name.Replace("(Clone)", "").Trim();
        }

        PlayerXP playerXp = GetComponent<PlayerXP>();
        string lvlText = (playerXp != null) ? "Lv." + playerXp.currentLevel.Value + " " : "";

        Font gameFont = EnchantedNatureGUI.GetActiveGameFont();

        GUIStyle heroStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            font = gameFont
        };
        heroStyle.normal.textColor = teamColor;

        string leftText = lvlText + teamLabel + "  \u2022  " + heroName;
        GUI.Label(new Rect(hudX + 10f, hudY + 1f, hudW * 0.55f, 15f), leftText, heroStyle);

        // 3. Free Fire Style Numerical Display ("HP 200 / 200")
        float healthRatio = Mathf.Clamp01(currentHealth / maxHealth);
        float ghostRatio = Mathf.Clamp01(ghostHealth / maxHealth);

        int curHp = Mathf.Max(0, Mathf.CeilToInt(currentHealth));
        int maxHp = Mathf.CeilToInt(maxHealth);

        GUIStyle hpStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight,
            font = gameFont
        };
        hpStyle.normal.textColor = Color.white;

        string hpText = "HP  " + curHp + " / " + maxHp;
        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.Label(new Rect(hudX + hudW * 0.45f + 1f, hudY + 2f, hudW * 0.55f - 11f, 15f), hpText, hpStyle);
        GUI.color = Color.white;
        GUI.Label(new Rect(hudX + hudW * 0.45f, hudY + 1f, hudW * 0.55f - 10f, 15f), hpText, hpStyle);

        // 4. Free Fire Health Bar Slot
        float barX = hudX + 8f;
        float barY = hudY + 17f;
        float barW = hudW - 16f;
        float barH = 12f;

        // Dark Track Background
        GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);
        GUI.DrawTexture(new Rect(barX, barY, barW, barH), Texture2D.whiteTexture);

        // Ghost Damage Lag Bar (Bright Amber / White)
        if (ghostRatio > 0.001f)
        {
            GUI.color = new Color(1.0f, 0.86f, 0.32f, 0.88f);
            GUI.DrawTexture(new Rect(barX, barY, barW * ghostRatio, barH), Texture2D.whiteTexture);
        }

        // Active Health Fill (Dynamic Free Fire Gradient)
        Color activeHealthCol;
        if (healthRatio > 0.55f)
        {
            activeHealthCol = Color.Lerp(new Color(0.18f, 0.82f, 1.0f), new Color(0.12f, 0.96f, 0.52f), (healthRatio - 0.55f) / 0.45f);
        }
        else if (healthRatio > 0.25f)
        {
            activeHealthCol = new Color(1.0f, 0.74f, 0.15f);
        }
        else
        {
            float pulse = 0.80f + 0.20f * Mathf.Sin(Time.time * 10f);
            activeHealthCol = new Color(1.0f, 0.12f, 0.22f, pulse);
        }

        GUI.color = activeHealthCol;
        GUI.DrawTexture(new Rect(barX, barY, barW * healthRatio, barH), Texture2D.whiteTexture);

        // Tactical Dividers (25%, 50%, 75%)
        GUI.color = new Color(0.03f, 0.05f, 0.08f, 0.90f);
        for (int s = 1; s <= 3; s++)
        {
            float tickX = barX + barW * (s * 0.25f);
            GUI.DrawTexture(new Rect(tickX - 0.75f, barY, 1.5f, barH), Texture2D.whiteTexture);
        }
        GUI.color = Color.white;

        // 5. Curved 45% Bottom Accent Trim below the HUD
        float arcCenterY = hudY + hudH + 2f;
        float arcRadius = hudW * 0.22f;
        for (int i = 0; i < 18; i++)
        {
            float t = (float)i / 18f;
            float angleDeg = 171f - t * 162f;
            float rad = angleDeg * Mathf.Deg2Rad;
            float ptX = (hudX + hudW * 0.5f) + arcRadius * Mathf.Cos(rad);
            float ptY = arcCenterY + (arcRadius * 0.32f) * Mathf.Sin(rad);

            float tx = arcRadius * Mathf.Sin(rad);
            float ty = -(arcRadius * 0.32f) * Mathf.Cos(rad);
            float rot = Mathf.Atan2(ty, tx) * Mathf.Rad2Deg;

            DrawRotatedRect(ptX, ptY, 4f, 1.5f, rot, new Color(teamColor.r, teamColor.g, teamColor.b, 0.60f));
        }
    }

    // =========================================================================
    // Curved Health Arc covering 45% of the circle from the bottom at character base
    // =========================================================================
        void DrawCurvedBaseArc(PlayerMovement.TeamType currentTeam, Color teamColor, string teamLabel, bool isHumanOwner)
    {
        if (Camera.main == null) return;

        // Ground-level projection at character feet (zero vertical offset into legs)
        Vector3 groundWorldPos = transform.position;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(groundWorldPos);

        if (screenPos.z < 0) return;

        float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
        if (distance > 38f) return;

        float alpha = Mathf.Clamp01(1f - (distance - 22f) / 16f);
        if (alpha <= 0.02f) return;

        // Offset center downward in screen space so the arc rests completely below the character's body
        float groundOffsetScreen = Mathf.Clamp(22f - distance * 0.25f, 14f, 26f);
        Vector2 center = new Vector2(screenPos.x, Screen.height - screenPos.y + groundOffsetScreen);

        // Generous radius framing the ground disc cleanly outside the character feet
        float rx = Mathf.Clamp(54f - distance * 0.65f, 32f, 58f);
        float ry = rx * 0.62f; // Perspective foreshortening for natural ground alignment

        // 45% of 360 deg = 162 deg, centered at bottom (90 deg): [9 deg to 171 deg]
        const int SEGMENTS = 28;
        float startAngle = 171f;
        float totalSpan = 162f;
        float stepDeg = totalSpan / SEGMENTS;

        float healthRatio = Mathf.Clamp01(currentHealth / maxHealth);
        float ghostRatio = Mathf.Clamp01(ghostHealth / maxHealth);

        // Dynamic vibrant colors
        Color activeHealthColor;
        if (healthRatio > 0.55f)
        {
            activeHealthColor = Color.Lerp(new Color(0.18f, 0.82f, 1f), new Color(0.12f, 0.98f, 0.52f), (healthRatio - 0.55f) / 0.45f);
        }
        else if (healthRatio > 0.25f)
        {
            activeHealthColor = new Color(1.0f, 0.76f, 0.18f);
        }
        else
        {
            float pulse = 0.80f + 0.20f * Mathf.Sin(Time.time * 10f);
            activeHealthColor = new Color(1.0f, 0.14f, 0.22f, pulse);
        }

        // Fluid energy shimmer wave traveling along the arc to feel dynamic and alive
        float flowPhase = Mathf.Repeat(Time.time * 0.65f, 1.4f) - 0.2f;
        float breath = 0.94f + 0.06f * Mathf.Sin(Time.time * 2.8f);

        Color trackBgBase = new Color(0.04f, 0.06f, 0.08f, 0.85f * alpha);
        Color trackRimBase = new Color(teamColor.r, teamColor.g, teamColor.b, 0.55f * alpha * breath);
        Color ghostColorBase = new Color(1.0f, 0.88f, 0.32f, 0.88f * alpha);

        float segLength = ((rx + ry) * 0.5f * Mathf.Deg2Rad * stepDeg) + 1.4f;

        // Render tapered crescent blade segments (wide & thick at root, tapering smoothly to sleek tip)
        for (int i = 0; i < SEGMENTS; i++)
        {
            float tMid = (i + 0.5f) / SEGMENTS;
            float angleMid = startAngle - tMid * totalSpan;
            float rad = angleMid * Mathf.Deg2Rad;

            float ptX = center.x + rx * Mathf.Cos(rad);
            float ptY = center.y + ry * Mathf.Sin(rad);

            float tx = rx * Mathf.Sin(rad);
            float ty = -ry * Mathf.Cos(rad);
            float rot = Mathf.Atan2(ty, tx) * Mathf.Rad2Deg;

            // Geometric tapering: starts bold/wide (11px track, 8px fill) and smoothly tapers down (3.8px track, 2.4px fill)
            float taperT = Mathf.SmoothStep(0f, 1f, tMid);
            float trackThick = Mathf.Lerp(11.0f, 3.8f, taperT);
            float fillThick = Mathf.Lerp(8.0f, 2.4f, taperT);

            // Shimmer gleam traveling along the blade
            float shimmerDist = Mathf.Abs(tMid - flowPhase);
            float shimmerBoost = Mathf.Clamp01(1f - shimmerDist / 0.16f) * 0.35f;

            Color segFillColor = new Color(
                Mathf.Clamp01(activeHealthColor.r + shimmerBoost),
                Mathf.Clamp01(activeHealthColor.g + shimmerBoost),
                Mathf.Clamp01(activeHealthColor.b + shimmerBoost),
                activeHealthColor.a * alpha
            );

            // 1. Dark Track Background with Tapered Bevel
            DrawRotatedRect(ptX, ptY, segLength, trackThick, rot, trackBgBase);

            // Fine Neon Contour Rim
            DrawRotatedRect(ptX, ptY, segLength, 1.2f, rot, trackRimBase);

            // 2. Ghost Damage Lag Arc (Tapered)
            if (ghostRatio > healthRatio && tMid <= ghostRatio)
            {
                DrawRotatedRect(ptX, ptY, segLength, fillThick, rot, ghostColorBase);
            }

            // 3. Active Health Arc (Tapered Crescent Blade)
            if (tMid <= healthRatio)
            {
                DrawRotatedRect(ptX, ptY, segLength, fillThick, rot, segFillColor);
            }
        }

        // Weighted Socket Cap at the Origin / Root (left side)
        float rootAngleRad = startAngle * Mathf.Deg2Rad;
        float rootX = center.x + rx * Mathf.Cos(rootAngleRad);
        float rootY = center.y + ry * Mathf.Sin(rootAngleRad);
        float rootRot = Mathf.Atan2(-ry * Mathf.Cos(rootAngleRad), rx * Mathf.Sin(rootAngleRad)) * Mathf.Rad2Deg;
        DrawRotatedRect(rootX, rootY, 3.5f, 12f, rootRot, new Color(teamColor.r, teamColor.g, teamColor.b, 0.90f * alpha));

        // Leading-Edge Spark Node at current health tip
        if (healthRatio > 0.02f && healthRatio <= 1.0f)
        {
            float headAngleRad = (startAngle - healthRatio * totalSpan) * Mathf.Deg2Rad;
            float headX = center.x + rx * Mathf.Cos(headAngleRad);
            float headY = center.y + ry * Mathf.Sin(headAngleRad);
            float headRot = Mathf.Atan2(-ry * Mathf.Cos(headAngleRad), rx * Mathf.Sin(headAngleRad)) * Mathf.Rad2Deg;
            float headTaper = Mathf.SmoothStep(0f, 1f, healthRatio);
            float headSize = Mathf.Lerp(9.0f, 3.5f, headTaper);
            DrawRotatedRect(headX, headY, 2.5f, headSize + 2.5f, headRot, new Color(1f, 1f, 1f, 0.95f * alpha));
        }

        // Pill badge with HP number for in-world bots & opponents (placed neatly below the lowest arc curve)
        if (!isHumanOwner)
        {
            float badgeY = center.y + ry + 6f;
            float badgeW = 46f;
            float badgeH = 14f;
            Rect badgeRect = new Rect(center.x - badgeW * 0.5f, badgeY, badgeW, badgeH);

            GUI.color = new Color(0.04f, 0.06f, 0.09f, 0.90f * alpha);
            GUI.DrawTexture(badgeRect, Texture2D.whiteTexture);

            GUI.color = new Color(teamColor.r, teamColor.g, teamColor.b, 0.85f * alpha);
            GUI.DrawTexture(new Rect(badgeRect.x, badgeRect.y, badgeRect.width, 1.2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(badgeRect.x, badgeRect.yMax - 1.2f, badgeRect.width, 1.2f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle badgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            badgeStyle.normal.textColor = new Color(1f, 1f, 1f, 0.95f * alpha);

            string emoji = currentTeam == PlayerMovement.TeamType.Protector ? "\uD83D\uDEE1\uFE0F" : "\u2694\uFE0F";
            GUI.Label(badgeRect, emoji + " " + Mathf.CeilToInt(currentHealth), badgeStyle);
        }
    }

    static void DrawRotatedRect(float cx, float cy, float width, float height, float rotDeg, Color col)
    {
        Color oldColor = GUI.color;
        Matrix4x4 oldMat = GUI.matrix;
        GUI.color = col;
        GUIUtility.RotateAroundPivot(rotDeg, new Vector2(cx, cy));
        GUI.DrawTexture(new Rect(cx - width * 0.5f, cy - height * 0.5f, width, height), Texture2D.whiteTexture);
        GUI.matrix = oldMat;
        GUI.color = oldColor;
    }

    void DrawDeathRespawnScreen()
    {
        float sw = Screen.width;
        float sh = Screen.height;

        // 1. لا توجد أي شاشة سوداء معتمة إطلاقاً حتى يظل المشهد ثلاثي الأبعاد وسقوط الشخصية وموتها واضحاً 100%
        // فقط شريط علوي عائم شبه شفاف وفخم للغاية بتصميم قروسطي جذاب
        float bannerW = Mathf.Min(sw * 0.72f, 520f);
        float bannerH = 135f;
        float bannerX = (sw - bannerW) * 0.5f;
        float bannerY = 28f; // في أعلى الشاشة لترك كامل منتصف وأسفل الشاشة مكشوفاً لرؤية الجثة والسقوط والمعركة

        // خلفية زجاجية فائقة النقاء وشبه شفافة تسمح برؤية المعركة وسقوط الشخصية خلفها بوضوح تام
        GUI.color = new Color(0.04f, 0.02f, 0.05f, 0.42f);
        GUI.DrawTexture(new Rect(bannerX, bannerY, bannerW, bannerH), Texture2D.whiteTexture);

        // إطار نيون سحري قرمزي وذهبي رفيع
        Color borderRed = new Color(0.95f, 0.22f, 0.25f, 0.85f);
        Color borderGold = new Color(1.0f, 0.82f, 0.25f, 0.9f);

        GUI.color = borderGold;
        GUI.DrawTexture(new Rect(bannerX, bannerY, bannerW, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(bannerX, bannerY + bannerH - 2f, bannerW, 2f), Texture2D.whiteTexture);
        GUI.color = borderRed;
        GUI.DrawTexture(new Rect(bannerX, bannerY, 2f, bannerH), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(bannerX + bannerW - 2f, bannerY, 2f, bannerH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 2. العنوان وأيقونة السقوط مع ظل داكن لقراءة فائقة الوضوح فوق أي مشهد ثلاثي الأبعاد
        int secondsLeft = Mathf.Clamp(Mathf.CeilToInt(currentRespawnCountdown), 1, 10);
        float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.04f;

        string titleStr = LanguageSelectionUI.Localize(
            "☠️ YOU HAVE FALLEN IN BATTLE ☠️",
            ar: "☠️ سقطت في المعركة ☠️",
            fa: "☠️ در میدان نبرد سقوط کردید ☠️",
            de: "☠️ DU BIST IM KAMPF GEFALLEN ☠️",
            it: "☠️ SEI CADUTO IN BATTAGLIA ☠️",
            tr: "☠️ SAVAŞTA YENİLDİNİZ ☠️",
            ru: "☠️ ВЫ ПАЛИ В БОЮ ☠️",
            fr: "☠️ VOUS ÊTES TOMBÉ AU COMBAT ☠️",
            es: "☠️ HAS CAÍDO EN COMBATE ☠️",
            hi: "☠️ आप युद्ध में वीरगति को प्राप्त हुए ☠️",
            zh: "☠️ 你已在战斗中阵亡 ☠️",
            ja: "☠️ 戦闘不能になりました ☠️",
            pt: "☠️ VOCÊ FOI DERROTADO EM COMBATE ☠️"
        );

        GUIStyle titleShadow = new GUIStyle(GUI.skin.label);
        titleShadow.alignment = TextAnchor.MiddleCenter;
        titleShadow.fontSize = 20;
        titleShadow.fontStyle = FontStyle.Bold;
        titleShadow.normal.textColor = new Color(0f, 0f, 0f, 0.95f);

        GUIStyle titleStyle = new GUIStyle(titleShadow);
        titleStyle.normal.textColor = borderRed;

        Rect titleRect = new Rect(bannerX, bannerY + 10f, bannerW, 26f);
        GUI.Label(new Rect(titleRect.x + 1f, titleRect.y + 1f, titleRect.width, titleRect.height), titleStr, titleShadow);
        GUI.Label(titleRect, titleStr, titleStyle);

        // 3. عداد العودة للحياة بنمط ذهبي متوهج وواضح جداً
        string countdownMsg = LanguageSelectionUI.Localize(
            $"⚡ Respawning in {secondsLeft} seconds...",
            ar: $"⚡ ستعود للحياة بعد {secondsLeft} ثوانٍ...",
            fa: $"⚡ بازگشت به زندگی در {secondsLeft} ثانیه...",
            de: $"⚡ Wiederbelebung in {secondsLeft} Sekunden...",
            it: $"⚡ Rientro in battaglia in {secondsLeft} secondi...",
            tr: $"⚡ {secondsLeft} saniye içinde yeniden doğuş...",
            ru: $"⚡ Возрождение через {secondsLeft} сек...",
            fr: $"⚡ Réapparition dans {secondsLeft} secondes...",
            es: $"⚡ Volverás a la vida en {secondsLeft} segundos...",
            hi: $"⚡ {secondsLeft} सेकंड में पुनर्जीवन...",
            zh: $"⚡ 将在 {secondsLeft} 秒后复活...",
            ja: $"⚡ {secondsLeft} 秒後に復活...",
            pt: $"⚡ Renascendo em {secondsLeft} segundos..."
        );

        GUIStyle countShadow = new GUIStyle(GUI.skin.label);
        countShadow.alignment = TextAnchor.MiddleCenter;
        countShadow.fontSize = Mathf.RoundToInt(22 * pulse);
        countShadow.fontStyle = FontStyle.Bold;
        countShadow.normal.textColor = new Color(0f, 0f, 0f, 0.95f);

        GUIStyle countStyle = new GUIStyle(countShadow);
        countStyle.normal.textColor = borderGold;

        Rect countRect = new Rect(bannerX, bannerY + 40f, bannerW, 30f);
        GUI.Label(new Rect(countRect.x + 1f, countRect.y + 1f, countRect.width, countRect.height), countdownMsg, countShadow);
        GUI.Label(countRect, countdownMsg, countStyle);

        // 4. شريط تقدم العودة للحياة (10 ثوانٍ)
        float barW = bannerW * 0.82f;
        float barH = 7f;
        float barX = bannerX + (bannerW - barW) * 0.5f;
        float barY = bannerY + 76f;

        float ratio = Mathf.Clamp01(currentRespawnCountdown / 10f);

        GUI.color = new Color(0.1f, 0.1f, 0.15f, 0.75f);
        GUI.DrawTexture(new Rect(barX, barY, barW, barH), Texture2D.whiteTexture);

        GUI.color = Color.Lerp(borderRed, borderGold, 1f - ratio);
        GUI.DrawTexture(new Rect(barX, barY, barW * (1f - ratio), barH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 5. نص إرشادي لطيف وموجز في أسفل الشريط
        GUIStyle subShadow = new GUIStyle(GUI.skin.label);
        subShadow.alignment = TextAnchor.MiddleCenter;
        subShadow.fontSize = 12;
        subShadow.normal.textColor = new Color(0f, 0f, 0f, 0.9f);

        GUIStyle subStyle = new GUIStyle(subShadow);
        subStyle.normal.textColor = new Color(0.8f, 0.95f, 0.88f, 0.95f);

        string hintStr = LanguageSelectionUI.Localize(
            "🌿 Preparing safe respawn at your team's sanctuary",
            ar: "🌿 جاري تجهيز إحيائك عند ملاذ فريقك المخصص",
            fa: "🌿 آماده‌سازی برای احیا در پناهگاه امن تیم شما",
            de: "🌿 Wiederbelebung am Heiligtum deines Teams",
            it: "🌿 Preparazione al rientro nel santuario della tua squadra",
            tr: "🌿 Takımınızın sığınağında yeniden doğuş hazırlanıyor",
            ru: "🌿 Подготовка к возрождению на базе команды",
            fr: "🌿 Réapparition au sanctuaire de votre équipe",
            es: "🌿 Preparando reaparición en el santuario de tu equipo",
            hi: "🌿 आपकी टीम के अभयारण्य में पुनर्जीवन की तैयारी",
            zh: "🌿 正在团队圣地准备重生",
            ja: "🌿 拠点で復活準備中",
            pt: "🌿 Preparando renascimento no santuário da equipe"
        );

        Rect subRect = new Rect(bannerX, bannerY + 92f, bannerW, 20f);
        GUI.Label(new Rect(subRect.x + 1f, subRect.y + 1f, subRect.width, subRect.height), hintStr, subShadow);
        GUI.Label(subRect, hintStr, subStyle);
    }
}
