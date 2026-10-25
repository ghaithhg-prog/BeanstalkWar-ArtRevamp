using UnityEngine;
using System.Collections;
using TMPro;

public class Health : MonoBehaviour
{
    public TextMeshProUGUI respawnText;

    public float maxHealth = 200f;
    public float currentHealth;
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
            isDead = false;
        }

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

    public void TakeDamage(float amount)
    {
        if (isDead) return;
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
        }
        TakeDamage(amount);
    }

    void Die()
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
        if (player != null) player.enabled = false;

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
            transform.position = targetSpawnPos + Vector3.up * 1.05f;
            if (spawnPoint != null) transform.rotation = spawnPoint.rotation;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
            }

            if (player != null) player.enabled = true;
        }

        currentHealth = maxHealth;
        isDead = false;
        IsLocalPlayerDead = false;
        currentRespawnCountdown = 0f;

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

        // موقع فوق رأس اللاعب
        Vector3 headPos = transform.position + Vector3.up * 2.3f;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(headPos);

        // لا ترسم إذا كان اللاعب خلف الكاميرا
        if (screenPos.z < 0) return;

        // حساب المسافة للتحكم بالحجم والإخفاء
        float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
        if (distance > 35f) return;

        float barWidth = Mathf.Clamp(75f - distance * 0.7f, 50f, 75f);
        float barHeight = 8f;

        float x = screenPos.x - barWidth / 2f;
        float y = Screen.height - screenPos.y - barHeight - 6f;

        // 1. خلفية داكنة نصف شفافة مع إطار بلون الفريق المشع
        GUI.color = new Color(0.06f, 0.06f, 0.1f, 0.88f);
        GUI.DrawTexture(new Rect(x - 2, y - 2, barWidth + 4, barHeight + 4), Texture2D.whiteTexture);

        // إطار علوي وسفلي بلون الفريق
        GUI.color = teamColor;
        GUI.DrawTexture(new Rect(x - 2, y - 2, barWidth + 4, 1.5f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x - 2, y + barHeight + 0.5f, barWidth + 4, 1.5f), Texture2D.whiteTexture);

        // 2. شريط الصحة بتدرج لوني يعكس صحة الفريق
        float healthPercent = Mathf.Clamp01(currentHealth / maxHealth);
        Color fillGrad = Color.Lerp(new Color(0.9f, 0.15f, 0.15f), teamColor, healthPercent);
        GUI.color = fillGrad;
        GUI.DrawTexture(new Rect(x, y, barWidth * healthPercent, barHeight), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 3. اسم ووسم الفريق فوق الشريط
        GUIStyle teamStyle = new GUIStyle(GUI.skin.label);
        teamStyle.alignment = TextAnchor.MiddleCenter;
        teamStyle.fontSize = 9;
        teamStyle.fontStyle = FontStyle.Bold;
        teamStyle.normal.textColor = teamColor;

        GUI.Label(new Rect(x - 30, y - 16f, barWidth + 60, 15f), teamLabel, teamStyle);
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
